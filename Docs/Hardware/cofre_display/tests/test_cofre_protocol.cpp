// Teste no PC do MESMO cofre_protocol.h que roda no Arduino, contra os vetores de protocol_vectors.csv
// (o teste C# lê esse CSV; protocol_vectors.inc é a mesma lista gerada por make_vectors.py).
//
// Não usa biblioteca C/C++ nenhuma além de <stdint.h>, então compila até sem o Windows SDK:
//   MSVC (sem SDK):  cl /GS- /Zl /O1 test_cofre_protocol.cpp /link /NODEFAULTLIB /ENTRY:main /SUBSYSTEM:CONSOLE
//   MSVC/g++ normal: cl /EHsc test_cofre_protocol.cpp        |   g++ -std=c++11 test_cofre_protocol.cpp -o test_cofre
// Resultado: código de saída 0 = tudo certo; senão, o número de falhas (máx. 100). 101 = nenhum vetor carregado.
// Quando houver <stdio.h>, imprime o resumo e cada falha.
#include <stdint.h>
#include "../cofre_protocol.h"

#if defined(__has_include)
#if __has_include(<stdio.h>)
#include <stdio.h>
#define HAVE_STDIO 1
#endif
#else
#include <stdio.h>
#define HAVE_STDIO 1
#endif

struct Vector { const char *kind, *f1, *f2, *f3; };
static const Vector VECTORS[] = {
#include "protocol_vectors.inc"
};

static int failures = 0;
static int checks = 0;
static int firstFailure = 0;   // com -DCOFRE_FIRST_FAILURE o programa devolve o numero da 1a verificacao que falhou (sem stdio)

static void check(bool ok, const char *what, const char *detail) {
  checks++;
  if (ok) return;
  failures++;
  if (firstFailure == 0) firstFailure = checks;
#ifdef HAVE_STDIO
  printf("FALHOU: %s [%s]\n", what, detail);
#else
  (void)what; (void)detail;
#endif
}

static bool same(const char *a, const char *b) {
  while (*a && *a == *b) { a++; b++; }
  return *a == *b;
}

static uint32_t toU32(const char *s) {
  uint32_t v = 0;
  while (*s >= '0' && *s <= '9') { v = v * 10u + (uint32_t)(*s - '0'); s++; }
  return v;
}

static void copy(char *dst, const char *src) {
  while ((*dst++ = *src++) != '\0') {}
}

int main() {
  const int count = (int)(sizeof(VECTORS) / sizeof(VECTORS[0]));
  for (int i = 0; i < count; i++) {
    const Vector &v = VECTORS[i];
    char buf[64];
    if (same(v.kind, "crc")) {
      static const char HEX[] = "0123456789ABCDEF";
      uint8_t crc = cofre::crc8(v.f1, cofre::length(v.f1));
      char hex[3] = { HEX[crc >> 4], HEX[crc & 15], 0 };
      check(same(v.f3, hex), "crc", v.f1);
    } else if (same(v.kind, "money")) {
      cofre::formatMoney(toU32(v.f1), buf);
      check(same(v.f3, buf), "money", v.f1);
    } else if (same(v.kind, "cmd")) {
      uint32_t seq = 0, cents = 0;
      check(cofre::parseCommand(v.f3, seq, cents) == cofre::PARSE_OK, "cmd aceito", v.f3);
      check(seq == toU32(v.f1) && cents == toU32(v.f2), "cmd valores", v.f3);
      cofre::buildCommand(buf, toU32(v.f1), toU32(v.f2));
      check(same(v.f3, buf), "cmd montado", v.f3);
    } else if (same(v.kind, "badcmd")) {
      uint32_t seq = 0, cents = 777;
      check(cofre::parseCommand(v.f1, seq, cents) != cofre::PARSE_OK, "badcmd rejeitado", v.f1);
      check(cents == 777, "badcmd nao altera o valor", v.f1);
    } else if (same(v.kind, "ack")) {
      cofre::buildAck(buf, toU32(v.f1), toU32(v.f2));
      check(same(v.f3, buf), "ack", v.f3);
    } else if (same(v.kind, "err")) {
      cofre::buildErr(buf, toU32(v.f1));
      check(same(v.f3, buf), "err", v.f3);
    } else if (same(v.kind, "ready")) {
      cofre::buildReady(buf, toU32(v.f1));
      check(same(v.f3, buf), "ready", v.f3);
    }
    // "badreply": só o Unity interpreta respostas
  }

  // Trocar UM caractere de um quadro válido, em qualquer posição, tem que ser recusado (CRC-8 pega todo erro de até 8 bits).
  char good[64];
  cofre::size_type n = cofre::buildCommand(good, 1234, 2826000);
  for (cofre::size_type i = 0; i < n; i++) {
    for (int c = 32; c < 127; c++) {
      if (good[i] == (char)c) continue;
      if (cofre::hexValue(good[i]) >= 0 && cofre::hexValue(good[i]) == cofre::hexValue((char)c) && i >= n - 2) continue;   // A x a no CRC e o mesmo valor
      char bad[64];
      copy(bad, good);
      bad[i] = (char)c;
      uint32_t seq = 0, cents = 0;
      check(cofre::parseCommand(bad, seq, cents) != cofre::PARSE_OK, "corrompido aceito", bad);
    }
  }

  // Ida e volta de uma faixa grande de valores; o texto sempre cabe nas 16 colunas do LCD.
  uint32_t state = 12345;
  for (int i = 0; i < 200000; i++) {
    state = state * 1664525UL + 1013904223UL;
    uint32_t cents = state % (cofre::MAX_CENTS + 1);
    uint32_t seq = (state >> 3) % 100000;
    char frame[64], text[32];
    cofre::buildCommand(frame, seq, cents);
    uint32_t s2 = 0, c2 = 0;
    check(cofre::parseCommand(frame, s2, c2) == cofre::PARSE_OK && s2 == seq && c2 == cents, "ida e volta", frame);
    check(cofre::formatMoney(cents, text) <= 16, "cabe em 16 colunas", text);
  }

  // Acima do limite: faixa (com CRC valido, o numero de sequencia continua confiavel).
  char over[64];
  cofre::size_type m = cofre::writeTag(over, "COFRE|1|");
  m += cofre::writeUInt(over + m, cofre::MAX_CENTS + 1);
  cofre::seal(over, m);
  uint32_t seq = 0, cents = 0;
  check(cofre::parseCommand(over, seq, cents) == cofre::PARSE_RANGE && seq == 1, "acima do limite", over);

#ifdef HAVE_STDIO
  printf("%d vetores, %d verificacoes, %d falhas\n", count, checks, failures);
#endif
#ifdef COFRE_FIRST_FAILURE
  return firstFailure;
#endif
  if (count < 40) return 101;
  return failures > 100 ? 100 : failures;
}
