/*
 * Money Rank — driver mínimo para o OLED 0,96" I2C (SSD1306, 128x64), sem biblioteca externa.
 * Só o necessário para o display do cofre: limpar, texto pequeno numa linha e texto grande centralizado.
 * Fonte 5x7 com os caracteres usados pelo cofre: espaço, '$', ',', '-', '.', '0'-'9', 'A'-'Z'.
 *
 * Testado num Arduino Uno (Wire: SDA=A4, SCL=A5). Endereço detectado automaticamente: 0x3C ou 0x3D.
 */
#ifndef OLED_SSD1306_H
#define OLED_SSD1306_H

#include <Arduino.h>
#include <Wire.h>
#include <avr/pgmspace.h>
#include <string.h>

namespace oled {

const uint8_t WIDTH = 128;
const uint8_t PAGES = 8;  // 64 px / 8

uint8_t address = 0x3C;

const uint8_t FONT[][5] PROGMEM = {
  {0x00,0x00,0x00,0x00,0x00}, //  0 ' '
  {0x24,0x2A,0x7F,0x2A,0x12}, //  1 '$'
  {0x00,0x50,0x30,0x00,0x00}, //  2 ','
  {0x08,0x08,0x08,0x08,0x08}, //  3 '-'
  {0x00,0x60,0x60,0x00,0x00}, //  4 '.'
  {0x3E,0x51,0x49,0x45,0x3E}, //  5 '0'
  {0x00,0x42,0x7F,0x40,0x00}, //    '1'
  {0x42,0x61,0x51,0x49,0x46}, //    '2'
  {0x21,0x41,0x45,0x4B,0x31}, //    '3'
  {0x18,0x14,0x12,0x7F,0x10}, //    '4'
  {0x27,0x45,0x45,0x45,0x39}, //    '5'
  {0x3C,0x4A,0x49,0x49,0x30}, //    '6'
  {0x01,0x71,0x09,0x05,0x03}, //    '7'
  {0x36,0x49,0x49,0x49,0x36}, //    '8'
  {0x06,0x49,0x49,0x29,0x1E}, //    '9'
  {0x7E,0x11,0x11,0x11,0x7E}, // 15 'A'
  {0x7F,0x49,0x49,0x49,0x36}, //    'B'
  {0x3E,0x41,0x41,0x41,0x22}, //    'C'
  {0x7F,0x41,0x41,0x22,0x1C}, //    'D'
  {0x7F,0x49,0x49,0x49,0x41}, //    'E'
  {0x7F,0x09,0x09,0x09,0x01}, //    'F'
  {0x3E,0x41,0x49,0x49,0x7A}, //    'G'
  {0x7F,0x08,0x08,0x08,0x7F}, //    'H'
  {0x00,0x41,0x7F,0x41,0x00}, //    'I'
  {0x20,0x40,0x41,0x3F,0x01}, //    'J'
  {0x7F,0x08,0x14,0x22,0x41}, //    'K'
  {0x7F,0x40,0x40,0x40,0x40}, //    'L'
  {0x7F,0x02,0x0C,0x02,0x7F}, //    'M'
  {0x7F,0x04,0x08,0x10,0x7F}, //    'N'
  {0x3E,0x41,0x41,0x41,0x3E}, //    'O'
  {0x7F,0x09,0x09,0x09,0x06}, //    'P'
  {0x3E,0x41,0x51,0x21,0x5E}, //    'Q'
  {0x7F,0x09,0x19,0x29,0x46}, //    'R'
  {0x46,0x49,0x49,0x49,0x31}, //    'S'
  {0x01,0x01,0x7F,0x01,0x01}, //    'T'
  {0x3F,0x40,0x40,0x40,0x3F}, //    'U'
  {0x1F,0x20,0x40,0x20,0x1F}, //    'V'
  {0x3F,0x40,0x38,0x40,0x3F}, //    'W'
  {0x63,0x14,0x08,0x14,0x63}, //    'X'
  {0x07,0x08,0x70,0x08,0x07}, //    'Y'
  {0x61,0x51,0x49,0x45,0x43}, //    'Z'
};

inline uint8_t glyphIndex(char c) {
  if (c >= 'a' && c <= 'z') c -= 32;
  switch (c) {
    case '$': return 1;
    case ',': return 2;
    case '-': return 3;
    case '.': return 4;
  }
  if (c >= '0' && c <= '9') return 5 + (c - '0');
  if (c >= 'A' && c <= 'Z') return 15 + (c - 'A');
  return 0;
}

inline uint8_t glyphColumn(char c, uint8_t column) {
  return pgm_read_byte(&FONT[glyphIndex(c)][column]);
}

inline void command(uint8_t value) {
  Wire.beginTransmission(address);
  Wire.write(0x00);
  Wire.write(value);
  Wire.endTransmission();
}

inline void setCursor(uint8_t page, uint8_t column) {
  command(0xB0 | page);
  command(0x00 | (column & 0x0F));
  command(0x10 | (column >> 4));
}

// Envia `count` bytes de dados gerados por byteAt(i), em blocos (o buffer do Wire tem 32 bytes).
template <typename F>
void writeData(uint8_t count, F byteAt) {
  uint8_t i = 0;
  while (i < count) {
    Wire.beginTransmission(address);
    Wire.write(0x40);
    for (uint8_t n = 0; n < 16 && i < count; n++, i++) Wire.write(byteAt(i));
    Wire.endTransmission();
  }
}

inline void clearPage(uint8_t page) {
  setCursor(page, 0);
  writeData(WIDTH, [](uint8_t) -> uint8_t { return 0x00; });
}

inline void clear() {
  for (uint8_t page = 0; page < PAGES; page++) clearPage(page);
}

// Procura o display em 0x3C/0x3D e inicializa. Devolve false se não houver display no barramento.
inline bool begin() {
  Wire.begin();
  const uint8_t candidates[] = {0x3C, 0x3D};
  bool found = false;
  for (uint8_t i = 0; i < sizeof(candidates) && !found; i++) {
    Wire.beginTransmission(candidates[i]);
    if (Wire.endTransmission() == 0) { address = candidates[i]; found = true; }
  }
  if (!found) return false;
  Wire.setClock(400000);  // I2C rápido: a tela inteira em ~30 ms, para animar a ~20 quadros/s

  const uint8_t init[] = {
    0xAE,       // display off
    0xD5, 0x80, // clock
    0xA8, 0x3F, // multiplex 64
    0xD3, 0x00, // offset 0
    0x40,       // start line 0
    0x8D, 0x14, // charge pump on
    0x20, 0x02, // page addressing
    0xA1,       // segment remap
    0xC8,       // COM scan descending
    0xDA, 0x12, // COM pins
    0x81, 0xCF, // contrast
    0xD9, 0xF1, // pre-charge
    0xDB, 0x40, // VCOMH
    0xA4,       // follow RAM
    0xA6,       // normal
    0xAF        // display on
  };
  for (uint8_t i = 0; i < sizeof(init); i++) command(init[i]);
  clear();
  return true;
}

// Texto pequeno (5x7 + 1 px) centralizado numa página. Até 21 caracteres.
inline void smallText(uint8_t page, const char *text) {
  clearPage(page);
  uint8_t length = (uint8_t)strlen(text);
  if (length > 21) length = 21;
  setCursor(page, (WIDTH - length * 6) / 2);
  writeData(length * 6, [text](uint8_t i) -> uint8_t {
    uint8_t col = i % 6;
    return col < 5 ? glyphColumn(text[i / 6], col) : 0x00;
  });
}

// Texto grande centralizado ocupando as páginas firstPage..firstPage+pageCount-1 (limpa a área toda).
// Escolhe a maior escala horizontal que cabe na largura; a altura usa scaleY px por linha da fonte.
inline void bigText(const char *text, uint8_t firstPage, uint8_t pageCount, uint8_t scaleY) {
  // Cada caractere ocupa 5*scaleX px + 1 px de espaço: escala 3 = até 8 caracteres, 2 = até 11, 1 = até 21.
  uint8_t length = (uint8_t)strlen(text);
  uint8_t scaleX = 3;
  while (scaleX > 1 && length * (5 * scaleX + 1) > WIDTH) scaleX--;
  if (length * (5 * scaleX + 1) > WIDTH) length = WIDTH / (5 * scaleX + 1);
  const uint8_t cell = 5 * scaleX + 1;
  if (length == 0) {
    for (uint8_t p = 0; p < pageCount; p++) clearPage(firstPage + p);
    return;
  }
  const uint8_t width = length * cell - 1;  // sem espaço depois do último caractere
  const uint8_t start = (WIDTH - width) / 2;
  if (scaleX == 1 && scaleY > 2) scaleY = 2;  // texto longo: letra menor também na altura

  for (uint8_t p = 0; p < pageCount; p++) {
    clearPage(firstPage + p);
    setCursor(firstPage + p, start);
    writeData(width, [text, p, cell, scaleX, scaleY](uint8_t x) -> uint8_t {
      uint8_t fontCol = (x % cell) / scaleX;
      if (fontCol >= 5) return 0x00;
      uint8_t source = glyphColumn(text[x / cell], fontCol);
      uint8_t out = 0;
      for (uint8_t bit = 0; bit < 8; bit++) {
        uint8_t fontRow = (uint8_t)((p * 8 + bit) / scaleY);
        if (fontRow < 7 && (source & (1 << fontRow))) out |= (uint8_t)(1 << bit);
      }
      return out;
    });
  }
}

// ------------------------------------------------------------------ desenho por camadas (tela inteira)
// Uma cena é uma função byteAt(page, x) que devolve os 8 pixels verticais daquela coluna; drawFrame envia
// a tela toda (1 KB). A 400 kHz isso leva ~30 ms, então não há framebuffer na RAM (o Uno só tem 2 KB).

template <typename F>
void drawFrame(F byteAt) {
  for (uint8_t page = 0; page < PAGES; page++) {
    setCursor(page, 0);
    writeData(WIDTH, [&byteAt, page](uint8_t x) -> uint8_t { return byteAt(page, x); });
  }
}

// Sprite em PROGMEM no formato de páginas (w colunas x h linhas) com o canto superior esquerdo em (sx, sy).
// sx/sy podem ser negativos ou passar da tela: o que fica fora é cortado. mirror = espelhado na horizontal.
inline uint8_t spriteByte(const uint8_t *bmp, uint8_t w, uint8_t h, int16_t sx, int16_t sy, uint8_t page, uint8_t x,
                          bool mirror = false) {
  int16_t cx = (int16_t)x - sx;
  if (cx < 0 || cx >= w) return 0;
  if (mirror) cx = w - 1 - cx;
  uint8_t out = 0;
  for (uint8_t bit = 0; bit < 8; bit++) {
    int16_t r = (int16_t)(page * 8 + bit) - sy;
    if (r < 0 || r >= h) continue;
    uint8_t b = pgm_read_byte(&bmp[(r >> 3) * w + cx]);
    if (b & (1 << (r & 7))) out |= (uint8_t)(1 << bit);
  }
  return out;
}

// Texto pequeno (5x7) numa única página, começando em startX. Devolve 0 fora do texto.
inline uint8_t smallTextByte(const char *text, uint8_t length, int16_t startX, uint8_t x) {
  int16_t rel = (int16_t)x - startX;
  if (rel < 0 || rel >= (int16_t)length * 6) return 0;
  uint8_t col = (uint8_t)(rel % 6);
  return col < 5 ? glyphColumn(text[rel / 6], col) : 0x00;
}

inline int16_t centeredSmallX(uint8_t length) { return ((int16_t)WIDTH - (int16_t)length * 6) / 2; }

// Texto ampliado e centralizado: layout calculado uma vez por quadro, byte calculado por coluna.
struct BigText {
  const char *text;
  uint8_t length, scaleX, scaleY, cell, start, firstPage;

  void layout(const char *t, uint8_t first, uint8_t maxScaleX, uint8_t sy) {
    text = t;
    firstPage = first;
    length = (uint8_t)strlen(t);
    scaleX = maxScaleX;
    while (scaleX > 1 && length * (5 * scaleX + 1) > WIDTH) scaleX--;
    if (length * (5 * scaleX + 1) > WIDTH) length = WIDTH / (5 * scaleX + 1);
    scaleY = (scaleX == 1 && sy > 2) ? 2 : sy;
    cell = 5 * scaleX + 1;
    start = length == 0 ? 0 : (uint8_t)((WIDTH - (length * cell - 1)) / 2);
  }

  uint8_t byteAt(uint8_t page, uint8_t x) const {
    if (length == 0 || page < firstPage || x < start) return 0;
    uint8_t rel = x - start;
    if (rel >= length * cell - 1) return 0;
    uint8_t fontCol = (rel % cell) / scaleX;
    if (fontCol >= 5) return 0;
    uint8_t source = glyphColumn(text[rel / cell], fontCol);
    uint8_t p = page - firstPage, out = 0;
    for (uint8_t bit = 0; bit < 8; bit++) {
      uint8_t fontRow = (uint8_t)((p * 8 + bit) / scaleY);
      if (fontRow < 7 && (source & (1 << fontRow))) out |= (uint8_t)(1 << bit);
    }
    return out;
  }
};

}  // namespace oled

#endif
