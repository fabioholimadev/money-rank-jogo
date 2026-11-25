"""Gera protocol_vectors.csv (lido pelo teste C#) e protocol_vectors.inc (embutido no teste C++) com uma
implementação independente em Python do CRC-8 e do formato.
Uso: python make_vectors.py ../protocol_vectors.csv protocol_vectors.inc"""
import sys


def crc8(b):
    c = 0
    for x in b:
        c ^= x
        for _ in range(8):
            c = ((c << 1) ^ 7) & 255 if c & 128 else (c << 1) & 255
    return c


def seal(s):
    return "%s*%02X" % (s, crc8(s.encode()))


def money(c):
    u = str(c // 100)
    out = ""
    for i, ch in enumerate(u):
        if i and (len(u) - i) % 3 == 0:
            out += "."
        out += ch
    return "R$%s,%02d" % (out, c % 100)


rows = ["kind;f1;f2;f3"]
rows.append("crc;123456789;;%02X" % crc8(b"123456789"))
for t in ["COFRE|1|0", "COFRE|7|2826000", "ACK|3|150", "PRONTO|0"]:
    rows.append("crc;%s;;%02X" % (t, crc8(t.encode())))
for c in [0, 1, 9, 10, 99, 100, 101, 1000, 12345, 123456, 2826000, 99999999, 100000000, 123456789, 999999999]:
    rows.append("money;%d;;%s" % (c, money(c)))
for seq, c in [(0, 0), (1, 0), (1, 100), (7, 123400), (7, 2826000), (42, 1), (99, 99), (100, 100000), (2147483647, 999999999), (65536, 99999999)]:
    rows.append("cmd;%d;%d;%s" % (seq, c, seal("COFRE|%d|%d" % (seq, c))))
    rows.append("ack;%d;%d;%s" % (seq, c, seal("ACK|%d|%d" % (seq, c))))
for seq in [0, 3, 2147483647]:
    rows.append("err;%d;;%s" % (seq, seal("ERR|%d" % seq)))
for c in [0, 2826000, 999999999]:
    rows.append("ready;%d;;%s" % (c, seal("PRONTO|%d" % c)))
good = seal("COFRE|7|123400")
bad = [good[:-2] + "00", good[:-3], "COFRE|7|123400", good.replace("123400", "123401"),
       seal("COFRE|7|12.50"), seal("COFRE|7|-5"), seal("COFRE|7|"), seal("COFRE||5"), seal("COFRE|7|0100"), seal("COFRE|07|100"),
       seal("COFRE|7|1000000000"), seal("COFRE|7|99999999999"), seal("COFRE|7|1|2"), seal("COFRE|7|+5"), seal("COFRE|7| 5"),
       seal("COFRE|2147483648|5"), seal("COFRE|-1|5"), seal("cofre|7|5"), seal("SALDO|7|5"), seal("COFRE|7|1,50"), seal("COFRE|7|5 "),
       "", "*00", "COFRE|7|5*", "COFRE|7|5*Z1", "COFRE|7|5*1"]
for b in bad:
    rows.append("badcmd;%s;;" % b)
for b in [seal("ACK|1"), seal("ACK|1|-1"), seal("ACK|1|1000000000"), seal("ERR|1|2"), seal("PRONTO"), seal("PRONTO|1|2"), seal("ACK|x|1"),
          seal("ACK|01|1"), seal("OK|1"), "ACK|1|100", "ACK|1|100*00", "ACK|1|100*ZZ", "", "R$0,00"]:
    rows.append("badreply;%s;;" % b)
open(sys.argv[1], "w", newline="\n").write("\n".join(rows) + "\n")
inc = []
for r in rows[1:]:
    f = r.split(";")
    inc.append("  { %s }," % ", ".join('"%s"' % x for x in f))
open(sys.argv[2], "w", newline="\n").write("// gerado por make_vectors.py; nao editar\n" + "\n".join(inc) + "\n")
print(len(rows), "rows")
