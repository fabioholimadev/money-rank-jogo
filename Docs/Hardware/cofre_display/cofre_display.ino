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
#include "oled_ssd1306.h"

void displayBegin() {
  oled::begin();                          // sem display no barramento o protocolo continua respondendo
  oled::smallText(0, "COFRE PUBLICO");
}

void displayAmountText(const char *text) {
  oled::bigText(text, 3, 4, 4);           // páginas 3..6 (32 px de altura)
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

void displayAmountText(const char *text) {
  size_t n = 0;
  while (text[n] != '\0') n++;
  lcd.setCursor(0, 1);
  for (size_t i = n; i < LCD_COLUMNS; i++) lcd.print(' ');   // alinhado à direita; sobrescreve o valor anterior sem lcd.clear()
  lcd.print(text);
}
#endif

char line[BUFFER_SIZE];
byte lineLength = 0;
bool discarding = false;                  // linha longa demais: ignora até o fim da linha
uint32_t shownCents = 0;                  // o que está no display agora

void showAmount(uint32_t cents) {
  char text[24];
  cofre::formatMoney(cents, text);
  displayAmountText(text);
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
  if (cents != shownCents) showAmount(cents);       // repetições não piscam a tela
  cofre::buildAck(out, seq, shownCents);            // devolve o que de fato está no display
  send(out);
}

void setup() {
  Serial.begin(115200);
  displayBegin();
  showAmount(0);                                    // R$0,00 até chegar o primeiro número
  char out[40];
  cofre::buildReady(out, shownCents);
  send(out);                                        // avisa o Unity que ligou (o Unity reenvia o saldo atual)
}

void loop() {
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
