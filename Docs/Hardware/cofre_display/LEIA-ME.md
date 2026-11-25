# Display do cofre público (LCD 16x2 + Arduino)

O Unity só **envia o saldo do cofre**. O Arduino só **mostra** o número recebido; ele não calcula nenhuma regra.
Enquanto nenhum número válido chegar, o LCD fica em `R$0,00`.

```
COFRE PUBLICO
      R$28.260,00
```

## Por que o número chega certo (C# → C++)

- O saldo vai como **centavos inteiros** (`28260` CRIA Coin → `2826000`). Não existe `float`/`double` nem arredondamento dos dois lados.
- Todo quadro termina em `*HH`, um **CRC-8** (polinômio 0x07). Número alterado no cabo é recusado, nunca exibido.
- O Arduino responde `ACK|seq|centavos*HH` com o valor que **está no display**. O Unity só dá o envio por concluído se esse número for igual ao enviado; se for diferente, envia de novo.
- Ao ligar, o Arduino avisa `PRONTO|0*HH`; o Unity reenvia o saldo atual.
- Limite: `R$9.999.999,99` (cabe em 32 bits sem sinal e nas 16 colunas). Valor fora da faixa é recusado (`ERR`) e o jogo continua normalmente.

| Sentido | Quadro | Significado |
| --- | --- | --- |
| Unity → Arduino | `COFRE\|<seq>\|<centavos>*HH` | saldo absoluto |
| Arduino → Unity | `ACK\|<seq>\|<centavos>*HH` | valor agora exibido |
| Arduino → Unity | `ERR\|<seq>*HH` | quadro recusado (formato, CRC ou faixa) |
| Arduino → Unity | `PRONTO\|<centavos>*HH` | ligou/reiniciou e mostra esse valor |

Serial: 115200 baud, 8N1, ASCII, uma linha por quadro.

## Arquivos

- `cofre_display.ino` — sketch (abrir esta pasta no Arduino IDE; precisa da biblioteca "LiquidCrystal I2C").
- `cofre_protocol.h` — protocolo em C++ puro (sem Arduino); é o mesmo código que roda no PC nos testes.
- `protocol_vectors.csv` — vetores compartilhados (CRC, textos de dinheiro, quadros válidos e inválidos).
- `tests/test_cofre_protocol.cpp` — teste no PC do `cofre_protocol.h` contra os vetores (instruções de compilação no topo do arquivo).
- `tests/make_vectors.py` — gera `protocol_vectors.csv` e `tests/protocol_vectors.inc` com uma implementação independente em Python.
- Lado C#: `Assets/_MoneyRank/Scripts/Domain/V4/V4Coffer.cs`; testes em `Assets/_MoneyRank/Tests/EditMode/V4CofferProtocolTests.cs` (leem o mesmo CSV).

## Antes de gravar no Arduino

1. Confirmar o módulo: LCD 1602 + adaptador I2C, endereço (`0x27` ou `0x3F` — se a tela acender sem texto, rode um I2C scanner e ajuste `LCD_ADDRESS`).
2. No Unity: escolher a porta COM no painel do cofre. Sem display o jogo segue e o saldo aparece na tela.
3. Teste de bancada: ao ligar deve mostrar `R$0,00`; ao iniciar a partida, o saldo do cofre.

Esta rotina ainda **não foi testada em hardware real** (ver `Docs/OPEN_DECISIONS.md`).
