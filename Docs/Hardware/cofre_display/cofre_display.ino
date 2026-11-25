/*
 * Money Rank — display do cofre público (V4, HANDOFF §13)
 * Protocolo: o Unity envia   COFRE|<seq>|<saldo>\n   (ASCII, 115200 baud, 8N1)
 *            o Arduino responde ACK|<seq>\n  ou  ERR|<seq>\n
 * O saldo é sempre ABSOLUTO; sequência repetida/antiga é idempotente (a última prevalece).
 * O display só EXIBE o saldo; não calcula regras. Sem display/porta, o jogo segue normalmente.
 *
 * ATENÇÃO — confirmar o hardware real antes de gravar: este sketch assume LCD 16x2 I2C (biblioteca LiquidCrystal_I2C).
 * Endereço I2C comum: 0x27 ou 0x3F (rode um I2C scanner). Para OLED SSD1306 trocar apenas a função show().
 */
#include <Wire.h>
#include <LiquidCrystal_I2C.h>

#define LCD_ADDRESS 0x27
#define LCD_COLUMNS 16
#define LCD_ROWS 2
#define BUFFER_SIZE 40

LiquidCrystal_I2C lcd(LCD_ADDRESS, LCD_COLUMNS, LCD_ROWS);
char line[BUFFER_SIZE];
byte length = 0;
long lastSeq = -1;
long balance = -1;

void show(long value, long seq) {
  char text[LCD_COLUMNS + 1];
  snprintf(text, sizeof(text), "COFRE  %ld UMR", value);   // ex.: "COFRE  1234 UMR"
  lcd.clear();
  lcd.setCursor(0, 0);
  lcd.print(text);
  lcd.setCursor(0, 1);
  lcd.print("seq ");
  lcd.print(seq);
}

void reply(const char *kind, long seq) {
  Serial.print(kind);
  Serial.print('|');
  Serial.println(seq);
}

// Strictly validates "COFRE|<seq>|<saldo>" with non-negative integers.
bool parse(const char *s, long &seq, long &value) {
  if (strncmp(s, "COFRE|", 6) != 0) return false;
  char *end;
  const char *p = s + 6;
  if (*p < '0' || *p > '9') return false;
  seq = strtol(p, &end, 10);
  if (*end != '|') return false;
  p = end + 1;
  if (*p < '0' || *p > '9') return false;
  value = strtol(p, &end, 10);
  return *end == '\0' && seq >= 0 && value >= 0;
}

void handleLine() {
  long seq = 0, value = 0;
  if (!parse(line, seq, value)) {
    // best effort: echo the sequence when it can still be read
    const char *first = strchr(line, '|');
    long guess = first ? atol(first + 1) : 0;
    reply("ERR", guess);
    return;
  }
  if (seq != lastSeq || value != balance) {   // duplicates are harmless: redraw only on change
    balance = value;
    lastSeq = seq;
    show(balance, seq);
  }
  reply("ACK", seq);
}

void setup() {
  Serial.begin(115200);
  lcd.init();
  lcd.backlight();
  lcd.setCursor(0, 0);
  lcd.print("COFRE  aguardando");
}

void loop() {
  while (Serial.available() > 0) {
    char c = (char)Serial.read();
    if (c == '\r') continue;
    if (c == '\n') {
      line[length] = '\0';
      if (length > 0) handleLine();
      length = 0;
    } else if (length < BUFFER_SIZE - 1) {
      line[length++] = c;
    } else {
      length = 0;                // overflow: drop the line, never crash
      reply("ERR", 0);
    }
  }
}
