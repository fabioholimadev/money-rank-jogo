// Money Rank - bench test for the 0.96" I2C OLED (SSD1306, 128x64) on an Arduino Uno.
//
// Wiring (Uno):  OLED GND -> GND | VCC -> 5V | SCL -> A5 | SDA -> A4
// No external libraries: talks to the SSD1306 directly over Wire (I2C).
//
// Serial test protocol (9600 baud, one command per line, '\n' terminated):
//   PING        -> PONG
//   SHOW <n>    -> OK <n>        (shows the integer n in large digits)
//   CLEAR       -> OK CLEAR
//   SCAN        -> I2C 0x3C ... (lists I2C devices found)
//   anything else -> ERR <text>
// On boot the board prints "READY MONEYRANK-OLED".
//
// Bench/diagnostic protocol only (wiring and I2C check). The game talks to the display
// through the coffer protocol in Docs/Hardware/cofre_display (115200 baud, CRC-8).

#include <Wire.h>
#include <avr/pgmspace.h>

uint8_t OLED_ADDRESS = 0x3C;         // auto-detected: 0x3C or 0x3D
const uint8_t OLED_WIDTH = 128;
const uint8_t OLED_PAGES = 8;        // 64 px / 8

// 5x7 font, column-major, bit 0 = top row. Characters: ' ', '-', '0'-'9', 'A'-'Z'.
const uint8_t FONT[][5] PROGMEM = {
  {0x00,0x00,0x00,0x00,0x00}, // ' '
  {0x08,0x08,0x08,0x08,0x08}, // '-'
  {0x3E,0x51,0x49,0x45,0x3E}, // 0
  {0x00,0x42,0x7F,0x40,0x00}, // 1
  {0x42,0x61,0x51,0x49,0x46}, // 2
  {0x21,0x41,0x45,0x4B,0x31}, // 3
  {0x18,0x14,0x12,0x7F,0x10}, // 4
  {0x27,0x45,0x45,0x45,0x39}, // 5
  {0x3C,0x4A,0x49,0x49,0x30}, // 6
  {0x01,0x71,0x09,0x05,0x03}, // 7
  {0x36,0x49,0x49,0x49,0x36}, // 8
  {0x06,0x49,0x49,0x29,0x1E}, // 9
  {0x7E,0x11,0x11,0x11,0x7E}, // A
  {0x7F,0x49,0x49,0x49,0x36}, // B
  {0x3E,0x41,0x41,0x41,0x22}, // C
  {0x7F,0x41,0x41,0x22,0x1C}, // D
  {0x7F,0x49,0x49,0x49,0x41}, // E
  {0x7F,0x09,0x09,0x09,0x01}, // F
  {0x3E,0x41,0x49,0x49,0x7A}, // G
  {0x7F,0x08,0x08,0x08,0x7F}, // H
  {0x00,0x41,0x7F,0x41,0x00}, // I
  {0x20,0x40,0x41,0x3F,0x01}, // J
  {0x7F,0x08,0x14,0x22,0x41}, // K
  {0x7F,0x40,0x40,0x40,0x40}, // L
  {0x7F,0x02,0x0C,0x02,0x7F}, // M
  {0x7F,0x04,0x08,0x10,0x7F}, // N
  {0x3E,0x41,0x41,0x41,0x3E}, // O
  {0x7F,0x09,0x09,0x09,0x06}, // P
  {0x3E,0x41,0x51,0x21,0x5E}, // Q
  {0x7F,0x09,0x19,0x29,0x46}, // R
  {0x46,0x49,0x49,0x49,0x31}, // S
  {0x01,0x01,0x7F,0x01,0x01}, // T
  {0x3F,0x40,0x40,0x40,0x3F}, // U
  {0x1F,0x20,0x40,0x20,0x1F}, // V
  {0x3F,0x40,0x38,0x40,0x3F}, // W
  {0x63,0x14,0x08,0x14,0x63}, // X
  {0x07,0x08,0x70,0x08,0x07}, // Y
  {0x61,0x51,0x49,0x45,0x43}, // Z
};

uint8_t glyphIndex(char c) {
  if (c >= 'a' && c <= 'z') c -= 32;
  if (c == '-') return 1;
  if (c >= '0' && c <= '9') return 2 + (c - '0');
  if (c >= 'A' && c <= 'Z') return 12 + (c - 'A');
  return 0;
}

uint8_t glyphColumn(char c, uint8_t column) {
  return pgm_read_byte(&FONT[glyphIndex(c)][column]);
}

void oledCommand(uint8_t command) {
  Wire.beginTransmission(OLED_ADDRESS);
  Wire.write(0x00);
  Wire.write(command);
  Wire.endTransmission();
}

void oledSetCursor(uint8_t page, uint8_t column) {
  oledCommand(0xB0 | page);
  oledCommand(0x00 | (column & 0x0F));
  oledCommand(0x10 | (column >> 4));
}

// Writes `count` data bytes produced by `byteAt(i)` in I2C chunks (Wire buffer is 32 bytes).
template <typename F>
void oledWriteData(uint8_t count, F byteAt) {
  uint8_t i = 0;
  while (i < count) {
    Wire.beginTransmission(OLED_ADDRESS);
    Wire.write(0x40);
    for (uint8_t n = 0; n < 16 && i < count; n++, i++) {
      Wire.write(byteAt(i));
    }
    Wire.endTransmission();
  }
}

void oledClearPage(uint8_t page) {
  oledSetCursor(page, 0);
  oledWriteData(OLED_WIDTH, [](uint8_t) -> uint8_t { return 0x00; });
}

void oledClear() {
  for (uint8_t page = 0; page < OLED_PAGES; page++) oledClearPage(page);
}

bool oledInit() {
  Wire.begin();
  const uint8_t candidates[] = {0x3C, 0x3D};
  bool found = false;
  for (uint8_t i = 0; i < sizeof(candidates) && !found; i++) {
    Wire.beginTransmission(candidates[i]);
    if (Wire.endTransmission() == 0) {
      OLED_ADDRESS = candidates[i];
      found = true;
    }
  }
  if (!found) return false;

  const uint8_t init[] = {
    0xAE,       // display off
    0xD5, 0x80, // clock divide
    0xA8, 0x3F, // multiplex 64
    0xD3, 0x00, // display offset 0
    0x40,       // start line 0
    0x8D, 0x14, // charge pump on
    0x20, 0x02, // page addressing mode
    0xA1,       // segment remap
    0xC8,       // COM scan descending
    0xDA, 0x12, // COM pins
    0x81, 0xCF, // contrast
    0xD9, 0xF1, // pre-charge
    0xDB, 0x40, // VCOMH
    0xA4,       // follow RAM
    0xA6,       // normal (not inverted)
    0xAF        // display on
  };
  for (uint8_t i = 0; i < sizeof(init); i++) oledCommand(init[i]);
  oledClear();
  return true;
}

// Small text (5x7 + 1px spacing) on one page.
void drawSmallText(uint8_t page, const char* text) {
  oledClearPage(page);
  uint8_t length = strlen(text);
  if (length > 21) length = 21;
  uint8_t start = (OLED_WIDTH - length * 6) / 2;
  oledSetCursor(page, start);
  oledWriteData(length * 6, [text](uint8_t i) -> uint8_t {
    uint8_t col = i % 6;
    return col < 5 ? glyphColumn(text[i / 6], col) : 0x00;
  });
}

// Large text: each font pixel becomes SCALE_X x SCALE_Y. Uses pages 2..5 (32 px tall).
const uint8_t SCALE_X = 3;
const uint8_t SCALE_Y = 4;
const uint8_t BIG_CELL = 6 * SCALE_X;   // 18 px per character
const uint8_t BIG_FIRST_PAGE = 2;
const uint8_t BIG_PAGE_COUNT = 4;

void drawBigText(const char* text) {
  uint8_t length = strlen(text);
  if (length > OLED_WIDTH / BIG_CELL) length = OLED_WIDTH / BIG_CELL;  // 7 characters
  uint8_t width = length * BIG_CELL;
  uint8_t start = (OLED_WIDTH - width) / 2;

  for (uint8_t p = 0; p < BIG_PAGE_COUNT; p++) {
    oledClearPage(BIG_FIRST_PAGE + p);
    oledSetCursor(BIG_FIRST_PAGE + p, start);
    oledWriteData(width, [text, p](uint8_t x) -> uint8_t {
      uint8_t inCell = x % BIG_CELL;
      uint8_t fontCol = inCell / SCALE_X;
      if (fontCol >= 5) return 0x00;
      uint8_t source = glyphColumn(text[x / BIG_CELL], fontCol);
      uint8_t out = 0;
      for (uint8_t bit = 0; bit < 8; bit++) {
        uint8_t fontRow = (p * 8 + bit) / SCALE_Y;
        if (fontRow < 7 && (source & (1 << fontRow))) out |= (1 << bit);
      }
      return out;
    });
  }
}

void showNumber(long value) {
  char buffer[12];
  ltoa(value, buffer, 10);
  drawSmallText(0, "MONEY RANK");
  drawBigText(buffer);
  drawSmallText(7, strlen(buffer) > 7 ? "MAX 7 DIGITOS" : "");
}

char line[24];
uint8_t lineLength = 0;

void handleLine(char* text) {
  while (*text == ' ') text++;
  if (strcasecmp(text, "PING") == 0) {
    Serial.println(F("PONG"));
  } else if (strncasecmp(text, "SHOW ", 5) == 0) {
    char* end;
    long value = strtol(text + 5, &end, 10);
    if (end == text + 5) {
      Serial.print(F("ERR "));
      Serial.println(text);
      return;
    }
    showNumber(value);
    Serial.print(F("OK "));
    Serial.println(value);
  } else if (strcasecmp(text, "SCAN") == 0) {
    Serial.print(F("I2C"));
    uint8_t count = 0;
    for (uint8_t address = 1; address < 127; address++) {
      Wire.beginTransmission(address);
      if (Wire.endTransmission() == 0) {
        Serial.print(F(" 0x"));
        Serial.print(address, HEX);
        count++;
      }
    }
    Serial.println(count == 0 ? F(" NONE") : F(""));
  } else if (strcasecmp(text, "CLEAR") == 0) {
    oledClear();
    Serial.println(F("OK CLEAR"));
  } else {
    Serial.print(F("ERR "));
    Serial.println(text);
  }
}

void setup() {
  Serial.begin(9600);
  if (!oledInit()) {
    Serial.println(F("ERR OLED NOT FOUND (check wiring / address 0x3C or 0x3D)"));
  } else {
    drawSmallText(0, "MONEY RANK");
    drawBigText("OK");
    drawSmallText(7, "AGUARDANDO JOGO");
  }
  Wire.begin();
  Serial.println(F("READY MONEYRANK-OLED"));
}

void loop() {
  while (Serial.available() > 0) {
    char c = (char)Serial.read();
    if (c == '\n' || c == '\r') {
      if (lineLength > 0) {
        line[lineLength] = '\0';
        handleLine(line);
        lineLength = 0;
      }
    } else if (lineLength < sizeof(line) - 1) {
      line[lineLength++] = c;
    }
  }
}
