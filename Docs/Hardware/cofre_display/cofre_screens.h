/*
 * Money Rank — telas animadas do display do cofre (OLED SSD1306 128x64).
 *
 * Dois modos:
 *   EXIBIÇÃO — vitrine para apresentações: logo, capivaras, profissões, moedas e uma demonstração do cofre, em loop.
 *              É o modo ao ligar (COFRE_EXHIBITION_ON_BOOT) enquanto o jogo não mandar nenhum saldo.
 *   JOGO     — mostra o saldo do cofre. Quando o saldo muda, o número conta até o valor novo e uma capivara
 *              atravessa a tela com uma moeda (entrando pela esquerda se o cofre ganhou, pela direita se pagou).
 *              Parado, a capivara fica no canto e dá um pulinho de vez em quando.
 *
 * Troca de modo: o primeiro saldo válido recebido passa para JOGO; o botão opcional (COFRE_MODE_BUTTON_PIN ligado
 * ao GND) alterna entre os modos; COFRE_RETURN_TO_EXHIBITION_MS > 0 volta à exibição após esse tempo sem quadros.
 * As animações não mudam o protocolo: o ACK é enviado na hora e sempre informa o valor final exibido.
 */
#ifndef COFRE_SCREENS_H
#define COFRE_SCREENS_H

#include <Arduino.h>
#include "cofre_protocol.h"
#include "oled_ssd1306.h"
#include "oled_art.h"

#ifndef COFRE_EXHIBITION_ON_BOOT
#define COFRE_EXHIBITION_ON_BOOT 1
#endif
#ifndef COFRE_MODE_BUTTON_PIN
#define COFRE_MODE_BUTTON_PIN 2            // botão entre este pino e o GND; -1 desliga
#endif
#ifndef COFRE_RETURN_TO_EXHIBITION_MS
#define COFRE_RETURN_TO_EXHIBITION_MS 0UL  // 0 = nunca volta sozinho para a exibição
#endif

namespace screens {

enum Mode : uint8_t { EXHIBITION = 0, GAME = 1 };
enum Scene : uint8_t { S_LOGO, S_PROFESSIONS, S_COINS, S_COFFER, S_INVITE, S_COUNT };

const uint16_t FRAME_MS = 50;                        // ~20 quadros/s
const uint16_t SCENE_MS[S_COUNT] = {3600, 4200, 3600, 3800, 3000};
const uint16_t CHANGE_ANIM_MS = 1500;                // capivara atravessando a tela
const uint16_t COUNT_ANIM_MS = 1000;                 // número contando até o valor novo
const uint16_t HOP_EVERY_MS = 8000, HOP_MS = 300;    // pulinho da capivara parada
const int16_t CAPY_Y = 64 - art::CAPY_A_H;           // capivara encostada na base da tela
const uint32_t DEMO_COFFER_CENTS = 2826000UL;        // R$28.260,00: total das moedas do kit (seção 6 da V4)

const char *const PROFESSIONS[6] = {"PROFESSORA", "MEDICA", "ECONOMISTA", "ATLETA", "EMPREENDEDORA", "MOTORISTA"};

Mode mode = EXHIBITION;
bool displayOk = false;
uint8_t scene = S_LOGO;
unsigned long sceneStart = 0, lastFrame = 0, lastAmountAt = 0, animStart = 0, idleSince = 0;
bool animating = false, needsDraw = true, lastHop = false;
uint32_t targetCents = 0, fromCents = 0;
char money[20];
oled::BigText big;

// ------------------------------------------------------------------ desenho

inline uint8_t capy(int16_t x, int16_t y, bool frameB, bool mirror, uint8_t page, uint8_t col) {
  return oled::spriteByte(frameB ? art::CAPY_B : art::CAPY_A, art::CAPY_A_W, art::CAPY_A_H, x, y, page, col, mirror);
}

inline uint8_t coin(int16_t x, int16_t y, uint8_t page, uint8_t col) {
  return oled::spriteByte(art::COIN, art::COIN_W, art::COIN_H, x, y, page, col);
}

inline uint8_t smallLine(const char *text, uint8_t page, uint8_t wantedPage, uint8_t col) {
  if (page != wantedPage) return 0;
  uint8_t n = (uint8_t)strlen(text);
  return oled::smallTextByte(text, n, oled::centeredSmallX(n), col);
}

inline int16_t lerp(int16_t a, int16_t b, unsigned long t, unsigned long total) {
  if (t >= total) return b;
  return (int16_t)(a + ((int32_t)(b - a) * (int32_t)t) / (int32_t)total);
}

// Contagem com desaceleração no fim (ease-out quadrático), só com inteiros.
inline uint32_t countTo(uint32_t from, uint32_t to, unsigned long t, unsigned long total) {
  if (t >= total) return to;
  uint32_t p = (uint32_t)(t * 1000UL / total);
  uint32_t eased = 1000UL - ((1000UL - p) * (1000UL - p)) / 1000UL;
  int64_t diff = (int64_t)to - (int64_t)from;
  return (uint32_t)((int64_t)from + diff * (int64_t)eased / 1000);
}

void drawExhibition(unsigned long t) {
  switch (scene) {
    case S_LOGO: {
      const int16_t lx = (oled::WIDTH - art::LOGO_W) / 2;
      const int16_t reveal = lerp(0, oled::WIDTH, t, 800);
      oled::drawFrame([&](uint8_t page, uint8_t x) -> uint8_t {
        uint8_t b = (int16_t)x < reveal ? oled::spriteByte(art::LOGO, art::LOGO_W, art::LOGO_H, lx, 0, page, x) : 0;
        if (t >= 800) b |= smallLine("EDUCACAO FINANCEIRA", page, 7, x);
        return b;
      });
      break;
    }
    case S_PROFESSIONS: {
      big.layout(PROFESSIONS[(t / 700) % 6], 2, 2, 2);
      const int16_t cx = lerp(-art::CAPY_A_W, oled::WIDTH, t, SCENE_MS[S_PROFESSIONS]);
      const bool frameB = (t / 150) % 2;
      oled::drawFrame([&](uint8_t page, uint8_t x) -> uint8_t {
        return smallLine("ESCOLHA SUA PROFISSAO", page, 0, x) | (page <= 3 ? big.byteAt(page, x) : 0) |
               capy(cx, CAPY_Y, frameB, false, page, x);
      });
      break;
    }
    case S_COINS: {
      const int16_t cx = lerp(-art::CAPY_A_W, oled::WIDTH, t, SCENE_MS[S_COINS]);
      const bool frameB = (t / 150) % 2;
      int16_t coinY[5];
      for (uint8_t i = 0; i < 5; i++) {
        unsigned long ct = (t + i * 300UL) % 1500UL;               // cada moeda cai em loop, defasada
        coinY[i] = lerp(-8, 44, ct, 1500);
      }
      oled::drawFrame([&](uint8_t page, uint8_t x) -> uint8_t {
        uint8_t b = smallLine("CADA ESCOLHA CONTA", page, 0, x) | capy(cx, CAPY_Y, frameB, false, page, x);
        for (uint8_t i = 0; i < 5; i++)
          if (coinY[i] >= 8) b |= coin(10 + i * 24, coinY[i], page, x);   // abaixo da linha do título
        return b;
      });
      break;
    }
    case S_COFFER: {
      cofre::formatMoney(countTo(0, DEMO_COFFER_CENTS, t, 2000), money);
      big.layout(money, 2, 3, 4);
      oled::drawFrame([&](uint8_t page, uint8_t x) -> uint8_t {
        return smallLine("COFRE PUBLICO", page, 0, x) | (page >= 2 && page <= 5 ? big.byteAt(page, x) : 0) |
               smallLine("FINANCIA O HOSPITAL", page, 7, x);
      });
      break;
    }
    default: {  // S_INVITE
      big.layout("VENHA JOGAR", 1, 2, 3);
      const bool hop = (t / 250) % 2;
      const int16_t cy = CAPY_Y - (hop ? 3 : 0);
      oled::drawFrame([&](uint8_t page, uint8_t x) -> uint8_t {
        return (page >= 1 && page <= 3 ? big.byteAt(page, x) : 0) |
               capy((oled::WIDTH - art::CAPY_A_W) / 2, cy, hop, false, page, x);
      });
      break;
    }
  }
}

void drawGame(unsigned long now) {
  const unsigned long t = now - animStart;
  if (animating && t >= CHANGE_ANIM_MS) { animating = false; idleSince = now; }

  const uint32_t shown = animating ? countTo(fromCents, targetCents, t, COUNT_ANIM_MS) : targetCents;
  cofre::formatMoney(shown, money);
  big.layout(money, 1, 3, 4);

  int16_t cx, cy = CAPY_Y, coinX = -100;
  bool frameB = false, mirror = false;
  if (animating) {
    const bool gained = targetCents >= fromCents;               // cofre ganhou: entra pela esquerda
    mirror = !gained;
    cx = gained ? lerp(-art::CAPY_A_W, oled::WIDTH, t, CHANGE_ANIM_MS)
                : lerp(oled::WIDTH, -art::CAPY_A_W, t, CHANGE_ANIM_MS);
    frameB = (t / 150) % 2;
    coinX = mirror ? cx : cx + art::CAPY_A_W - art::COIN_W;     // moeda sobre a cabeça
  } else {
    cx = oled::WIDTH - art::CAPY_A_W;
    if (lastHop) cy -= 3;
  }
  const int16_t coinY = CAPY_Y - art::COIN_H - 1;
  oled::drawFrame([&](uint8_t page, uint8_t x) -> uint8_t {
    return smallLine("COFRE PUBLICO", page, 0, x) | (page >= 1 && page <= 4 ? big.byteAt(page, x) : 0) |
           capy(cx, cy, frameB, mirror, page, x) | coin(coinX, coinY, page, x);
  });
}

// ------------------------------------------------------------------ controle

void setMode(Mode m, unsigned long now) {
  mode = m;
  scene = S_LOGO;
  sceneStart = now;
  animating = false;
  idleSince = now;
  needsDraw = true;
}

void begin(bool ok) {
  displayOk = ok;
#if COFRE_MODE_BUTTON_PIN >= 0
  pinMode(COFRE_MODE_BUTTON_PIN, INPUT_PULLUP);
#endif
  setMode(COFRE_EXHIBITION_ON_BOOT ? EXHIBITION : GAME, millis());
}

// Chamado a cada saldo válido recebido (mesmo repetido). Só anima quando o valor muda ou ao sair da exibição.
void showAmount(uint32_t cents) {
  const unsigned long now = millis();
  lastAmountAt = now;
  const bool wasExhibition = mode == EXHIBITION;
  if (wasExhibition) setMode(GAME, now);
  if (cents == targetCents && !wasExhibition) return;
  fromCents = animating ? countTo(fromCents, targetCents, now - animStart, COUNT_ANIM_MS) : targetCents;
  targetCents = cents;
  animStart = now;
  animating = true;
}

void pollButton(unsigned long now) {
#if COFRE_MODE_BUTTON_PIN >= 0
  static bool lastPressed = false;
  static unsigned long changedAt = 0;
  const bool pressed = digitalRead(COFRE_MODE_BUTTON_PIN) == LOW;
  if (pressed != lastPressed && now - changedAt > 50) {        // debounce
    changedAt = now;
    lastPressed = pressed;
    if (pressed) setMode(mode == GAME ? EXHIBITION : GAME, now);
  }
#endif
}

void tick() {
  const unsigned long now = millis();
  pollButton(now);
#if COFRE_RETURN_TO_EXHIBITION_MS > 0
  if (mode == GAME && !animating && now - lastAmountAt > COFRE_RETURN_TO_EXHIBITION_MS) setMode(EXHIBITION, now);
#endif
  if (!displayOk) return;

  if (mode == EXHIBITION) {
    if (now - lastFrame < FRAME_MS) return;
    lastFrame = now;
    unsigned long t = now - sceneStart;
    while (t >= SCENE_MS[scene]) {                              // próxima cena (em loop)
      sceneStart += SCENE_MS[scene];
      scene = (scene + 1) % S_COUNT;
      t = now - sceneStart;
    }
    drawExhibition(t);
    return;
  }

  // Modo jogo: anima durante a troca de valor; parado, só redesenha quando o pulinho começa/termina.
  const bool hop = !animating && now - idleSince > HOP_EVERY_MS && (now - idleSince) % HOP_EVERY_MS < HOP_MS;
  if (animating) {
    if (now - lastFrame < FRAME_MS) return;
  } else if (!needsDraw && hop == lastHop) {
    return;
  }
  lastFrame = now;
  lastHop = hop;
  needsDraw = false;
  drawGame(now);   // no quadro em que a animação termina, desenha a tela parada final
}

}  // namespace screens

#endif
