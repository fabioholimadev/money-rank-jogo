/*
 * Money Rank — display do cofre público (OLED 0,96" SSD1306 ou LCD 16x2 I2C + Arduino Uno)
 *
 * O Unity envia o saldo do cofre; este sketch só EXIBE o número recebido. Não calcula regra nenhuma.
 * Ao ligar (e enquanto nenhum número válido chegar) o display mostra  R$0,00.
 *
 * Protocolo, formato e limites: veja cofre_protocol.h (mesmo código testado no PC contra protocol_vectors.csv).
 * Resumo:  COFRE|<seq>|<centavos>*HH  ->  ACK|<seq>|<centavos exibidos>*HH  (ou ERR|<seq>*HH)
 *
 * Hardware: escolha o display em COFRE_DISPLAY_OLED abaixo.
 *   1 = OLED 0,96" I2C SSD1306 128x64 (padrão; testado no Uno). Sem biblioteca externa (oled_ssd1306.h).
 *       Ligação: GND->GND, VCC->5V, SCL->A5, SDA->A4. Endereço detectado sozinho (0x3C ou 0x3D).
 *   0 = LCD 1602 com adaptador I2C, biblioteca "LiquidCrystal I2C". Endereço comum: 0x27 ou 0x3F
 *       (rode um I2C scanner se a tela ficar apagada/sem texto). Ainda não testado em hardware real.
 * As fontes não têm acentos, por isso "COFRE PUBLICO".
 */
#define COFRE_DISPLAY_OLED 1

#include <Wire.h>
#include "cofre_protocol.h"

#define BUFFER_SIZE 48                    // o maior quadro válido tem 29 caracteres

#if COFRE_DISPLAY_OLED
// Modo exibição (logo e capivaras em loop) ao ligar, até chegar o primeiro saldo; botão opcional no pino 2
// alterna exibição/jogo. Configuração e detalhes em cofre_screens.h.
#define COFRE_EXHIBITION_ON_BOOT 1
#define COFRE_MODE_BUTTON_PIN 2           // botão entre o pino 2 e o GND; -1 desliga
#define COFRE_RETURN_TO_EXHIBITION_MS 0UL // 0 = só volta à exibição pelo botão ou reiniciando o Arduino
#include "cofre_screens.h"

void displayBegin() {
  screens::begin(oled::begin());          // sem display no barramento o protocolo continua respondendo
}

void displayAmount(uint32_t cents) {
  screens::showAmount(cents);             // anima a troca; repetições do mesmo valor não animam
}

void displayTick() {
  screens::tick();
}
#else
#include <LiquidCrystal_I2C.h>

#define LCD_ADDRESS 0x27
#define LCD_COLUMNS 16
#define LCD_ROWS 2

LiquidCrystal_I2C lcd(LCD_ADDRESS, LCD_COLUMNS, LCD_ROWS);

void displayBegin() {
  lcd.init();
  lcd.backlight();
  lcd.setCursor(0, 0);
  lcd.print("COFRE PUBLICO");
}

uint32_t lcdCents = 0xFFFFFFFFUL;          // valor no LCD (nenhum ainda)

void displayAmount(uint32_t cents) {
  if (cents == lcdCents) return;            // repetições não piscam a tela
  lcdCents = cents;
  char text[24];
  size_t n = (size_t)cofre::formatMoney(cents, text);
  lcd.setCursor(0, 1);
  for (size_t i = n; i < LCD_COLUMNS; i++) lcd.print(' ');   // alinhado à direita; sobrescreve o valor anterior sem lcd.clear()
  lcd.print(text);
}

void displayTick() {}
#endif

char line[BUFFER_SIZE];
byte lineLength = 0;
bool discarding = false;                  // linha longa demais: ignora até o fim da linha
uint32_t shownCents = 0;                  // valor final no display (0 = R$0,00 até chegar o primeiro número)

void showAmount(uint32_t cents) {
  displayAmount(cents);
  shownCents = cents;
}

void send(const char *frame) {
  Serial.println(frame);
}

void handleLine() {
  uint32_t seq = 0, cents = 0;
  char out[40];
  cofre::ParseResult r = cofre::parseCommand(line, seq, cents);
  if (r != cofre::PARSE_OK) {
    // O número de sequência só é confiável se o CRC passou (PARSE_RANGE); caso contrário responde 0.
    cofre::buildErr(out, r == cofre::PARSE_RANGE ? seq : 0);
    send(out);
    return;
  }
  showAmount(cents);                                // o display ignora repetições (não pisca nem reanima)
  cofre::buildAck(out, seq, shownCents);            // devolve o valor final exibido, já na hora (antes da animação)
  send(out);
}

void setup() {
  Serial.begin(115200);
  displayBegin();                                   // OLED: modo exibição até chegar o primeiro saldo
#if !COFRE_DISPLAY_OLED
  showAmount(0);                                    // LCD: R$0,00 até chegar o primeiro número
#endif
  char out[40];
  cofre::buildReady(out, shownCents);
  send(out);                                        // avisa o Unity que ligou (o Unity reenvia o saldo atual)
}

void loop() {
  displayTick();                                    // animações (cada quadro leva ~30 ms; a serial tem buffer)
  while (Serial.available() > 0) {
    char c = (char)Serial.read();
    if (c == '\r') continue;
    if (c == '\n') {
      if (discarding) {
        char out[40];
        cofre::buildErr(out, 0);
        send(out);
        discarding = false;
      } else if (lineLength > 0) {
        line[lineLength] = '\0';
        handleLine();
      }
      lineLength = 0;
    } else if (!discarding) {
      if (lineLength < BUFFER_SIZE - 1) line[lineLength++] = c;
      else { discarding = true; lineLength = 0; }
    }
  }
}
