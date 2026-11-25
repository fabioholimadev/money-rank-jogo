/*
 * Money Rank — protocolo do display do cofre (lado C++).
 * C++ puro, sem dependência de Arduino: roda no Arduino e no PC (os mesmos vetores de teste que o C# usa).
 *
 * Quadros (ASCII, uma linha cada, terminam em nova linha; 115200 baud 8N1). Todo quadro termina em *HH,
 * onde HH é o CRC-8 (polinômio 0x07, início 0x00) de tudo que vem antes do '*'.
 *
 *   Unity   -> Arduino  COFRE|<seq>|<centavos>*HH   saldo ABSOLUTO em centavos inteiros (nunca decimal)
 *   Arduino -> Unity    ACK|<seq>|<centavos>*HH     devolve o valor que está AGORA no display
 *                       ERR|<seq>*HH                quadro recusado (formato, CRC ou faixa)
 *                       PRONTO|<centavos>*HH        o Arduino ligou/reiniciou e mostra esse valor (0 = R$0,00)
 *
 * Valores: inteiros sem sinal de 32 bits; máximo 999999999 centavos (R$9.999.999,99), que cabe nas 16 colunas.
 * Nenhum float/double é usado, então não existe erro de arredondamento entre o C# e o C++.
 */
#ifndef COFRE_PROTOCOL_H
#define COFRE_PROTOCOL_H

#include <stdint.h>

namespace cofre {

typedef decltype(sizeof(0)) size_type;   // o mesmo que size_t, sem precisar de <stddef.h>

const uint32_t MAX_CENTS = 999999999UL;
const uint32_t MAX_SEQ = 2147483647UL;     // o C# usa int
const char CURRENCY_PREFIX[] = "R$";       // DECISION_REQUIRED: confirmar "R$" x "CRIA" (Docs/OPEN_DECISIONS.md)

enum ParseResult { PARSE_OK = 0, PARSE_FORMAT = 1, PARSE_CRC = 2, PARSE_RANGE = 3 };

inline size_type length(const char *s) { size_type n = 0; while (s[n] != '\0') n++; return n; }

inline uint8_t crc8(const char *s, size_type n) {
  uint8_t crc = 0;
  for (size_type i = 0; i < n; i++) {
    crc ^= (uint8_t)s[i];
    for (uint8_t b = 0; b < 8; b++) crc = (crc & 0x80) ? (uint8_t)((crc << 1) ^ 0x07) : (uint8_t)(crc << 1);
  }
  return crc;
}

// Decimal estrito sem sinal: sem espaços, sem '+', sem zeros à esquerda, no máximo 10 dígitos, sem estouro de 32 bits.
// Devolve o ponteiro logo após os dígitos ou NULL.
inline const char *readUInt(const char *p, uint32_t &out) {
  if (*p < '0' || *p > '9') return 0;
  if (*p == '0' && p[1] >= '0' && p[1] <= '9') return 0;
  uint32_t v = 0;
  uint8_t digits = 0;
  while (*p >= '0' && *p <= '9') {
    uint32_t d = (uint32_t)(*p - '0');
    if (++digits > 10) return 0;
    if (v > (0xFFFFFFFFUL - d) / 10UL) return 0;
    v = v * 10UL + d;
    p++;
  }
  out = v;
  return p;
}

inline int hexValue(char c) {
  if (c >= '0' && c <= '9') return c - '0';
  if (c >= 'A' && c <= 'F') return c - 'A' + 10;
  if (c >= 'a' && c <= 'f') return c - 'a' + 10;
  return -1;
}

// Valida "COFRE|<seq>|<centavos>*HH". Em PARSE_OK e PARSE_RANGE o CRC já foi conferido (seq é confiável em PARSE_RANGE).
inline ParseResult parseCommand(const char *line, uint32_t &seq, uint32_t &cents) {
  size_type n = length(line);
  if (n < 4 || line[n - 3] != '*') return PARSE_FORMAT;
  int hi = hexValue(line[n - 2]), lo = hexValue(line[n - 1]);
  if (hi < 0 || lo < 0) return PARSE_FORMAT;
  if (crc8(line, n - 3) != (uint8_t)(hi * 16 + lo)) return PARSE_CRC;

  const char *p = line;
  const char *tag = "COFRE|";
  for (; *tag; tag++, p++) if (*p != *tag) return PARSE_FORMAT;
  uint32_t s = 0, c = 0;
  p = readUInt(p, s);
  if (p == 0 || *p != '|') return PARSE_FORMAT;
  p = readUInt(p + 1, c);
  if (p == 0 || p != line + (n - 3)) return PARSE_FORMAT;
  seq = s;
  if (s > MAX_SEQ || c > MAX_CENTS) return PARSE_RANGE;
  cents = c;
  return PARSE_OK;
}

// "R$28.260,00" (separadores pt-BR só com aritmética inteira). `out` precisa de pelo menos 20 bytes. Devolve o tamanho.
inline size_type formatMoney(uint32_t cents, char *out) {
  size_type n = 0;
  for (const char *p = CURRENCY_PREFIX; *p; p++) out[n++] = *p;
  uint32_t units = cents / 100UL;
  uint8_t frac = (uint8_t)(cents % 100UL);
  char rev[12];
  uint8_t count = 0;
  do { rev[count++] = (char)('0' + (units % 10UL)); units /= 10UL; } while (units > 0);
  for (uint8_t i = 0; i < count; i++) {
    if (i > 0 && (count - i) % 3 == 0) out[n++] = '.';
    out[n++] = rev[count - 1 - i];
  }
  out[n++] = ',';
  out[n++] = (char)('0' + frac / 10);
  out[n++] = (char)('0' + frac % 10);
  out[n] = '\0';
  return n;
}

inline size_type writeUInt(char *out, uint32_t v) {
  char rev[11];
  uint8_t count = 0;
  do { rev[count++] = (char)('0' + (v % 10UL)); v /= 10UL; } while (v > 0);
  for (uint8_t i = 0; i < count; i++) out[i] = rev[count - 1 - i];
  return count;
}

// Fecha um quadro: acrescenta "*HH" ao corpo já escrito em `buf` (precisa de 4 bytes livres). Devolve o novo tamanho.
inline size_type seal(char *buf, size_type bodyLength) {
  static const char HEX_DIGITS[] = "0123456789ABCDEF";
  uint8_t crc = crc8(buf, bodyLength);
  buf[bodyLength] = '*';
  buf[bodyLength + 1] = HEX_DIGITS[crc >> 4];
  buf[bodyLength + 2] = HEX_DIGITS[crc & 0x0F];
  buf[bodyLength + 3] = '\0';
  return bodyLength + 3;
}

inline size_type writeTag(char *out, const char *tag) { size_type n = 0; while (tag[n]) { out[n] = tag[n]; n++; } return n; }

// Buffers de resposta: 40 bytes bastam (maior quadro: "ACK|2147483647|999999999*FF").
inline size_type buildAck(char *out, uint32_t seq, uint32_t cents) {
  size_type n = writeTag(out, "ACK|");
  n += writeUInt(out + n, seq); out[n++] = '|'; n += writeUInt(out + n, cents);
  return seal(out, n);
}
inline size_type buildErr(char *out, uint32_t seq) {
  size_type n = writeTag(out, "ERR|");
  n += writeUInt(out + n, seq);
  return seal(out, n);
}
inline size_type buildReady(char *out, uint32_t cents) {
  size_type n = writeTag(out, "PRONTO|");
  n += writeUInt(out + n, cents);
  return seal(out, n);
}
// Só para teste no PC (no Arduino quem monta o comando é o Unity).
inline size_type buildCommand(char *out, uint32_t seq, uint32_t cents) {
  size_type n = writeTag(out, "COFRE|");
  n += writeUInt(out + n, seq); out[n++] = '|'; n += writeUInt(out + n, cents);
  return seal(out, n);
}

}  // namespace cofre

#endif
