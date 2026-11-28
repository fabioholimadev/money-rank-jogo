# Display do cofre público (OLED 0,96" ou LCD 16x2 + Arduino)

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

- `cofre_display.ino` — sketch (abrir esta pasta no Arduino IDE). `COFRE_DISPLAY_OLED 1` (padrão) usa o OLED sem
  biblioteca externa; `0` usa o LCD e precisa da biblioteca "LiquidCrystal I2C".
- `oled_ssd1306.h` — driver mínimo do OLED SSD1306 128x64 (fonte 5x7 ampliada, sprites, endereço 0x3C/0x3D automático).
- `cofre_screens.h` — telas animadas do OLED, com dois modos:
  - **Exibição** (ao ligar, até chegar o primeiro saldo): logo da Money Rank, capivara andando com as seis profissões,
    moedas caindo, demonstração do cofre contando até R$28.260,00 e convite "VENHA JOGAR", em loop (~18 s).
  - **Jogo**: saldo do cofre; quando muda, o número conta até o valor novo e uma capivara atravessa com uma moeda
    (da esquerda se o cofre ganhou, da direita se pagou). Parada, a capivara fica no canto e pula de vez em quando.
  - Botão opcional entre o pino 2 e o GND alterna os modos (`COFRE_MODE_BUTTON_PIN`, `-1` desliga);
    `COFRE_RETURN_TO_EXHIBITION_MS` volta sozinho à exibição após um tempo sem saldo (padrão desligado).
  - O protocolo não muda: o `ACK` sai na hora com o valor final; a animação roda depois.
- `oled_art.h` — logo e capivaras em bitmap, gerados por `tools/make_oled_art.ps1` a partir de
  `Assets/Resources/MoneyRankLogo.png` e da pixel art do script (rode o script para regerar).
- `Test-CofreDisplay.ps1` — teste de bancada no PC sem abrir o jogo: envia saldos com o mesmo protocolo do Unity,
  confere cada `ACK` e verifica que um quadro com CRC errado é recusado.
- `cofre_protocol.h` — protocolo em C++ puro (sem Arduino); é o mesmo código que roda no PC nos testes.
- `protocol_vectors.csv` — vetores compartilhados (CRC, textos de dinheiro, quadros válidos e inválidos).
- `tests/test_cofre_protocol.cpp` — teste no PC do `cofre_protocol.h` contra os vetores (instruções de compilação no topo do arquivo).
- `tests/make_vectors.py` — gera `protocol_vectors.csv` e `tests/protocol_vectors.inc` com uma implementação independente em Python.
- Lado C#: `Assets/_MoneyRank/Scripts/Domain/V4/V4Coffer.cs`; testes em `Assets/_MoneyRank/Tests/EditMode/V4CofferProtocolTests.cs` (leem o mesmo CSV).

## Antes de gravar no Arduino

1. Escolher o display em `COFRE_DISPLAY_OLED`:
   - OLED 0,96" SSD1306 (padrão): GND→GND, VCC→5V, SCL→A5, SDA→A4.
   - LCD 1602 + adaptador I2C: endereço `0x27` ou `0x3F` (se a tela acender sem texto, rode um I2C scanner e ajuste `LCD_ADDRESS`).
2. Gravar: `arduino-cli compile --fqbn arduino:avr:uno -u -p COM11 Docs\Hardware\cofre_display`
   (o `arduino-cli` vem com a Arduino IDE 2, em `%LOCALAPPDATA%\Programs\Arduino IDE\resources\app\lib\backend\resources`).
3. Teste de bancada sem o jogo: `powershell -ExecutionPolicy Bypass -File Docs\Hardware\cofre_display\Test-CofreDisplay.ps1 -Port COM11`.
   Ao ligar deve mostrar `R$0,00`; o script termina com o último saldo enviado na tela.
4. No jogo (painel "Banco e cofre"): clique em **Detectar display**. O jogo testa cada porta USB/serial até o Arduino
   responder; não é preciso saber o número da COM (ele muda quando o cabo troca de entrada USB, ex.: COM11 → COM12).
   No executável, com uma única porta USB, a detecção roda sozinha ao iniciar a partida. Também dá para escolher a porta
   à mão; as portas Bluetooth virtuais não aparecem. Sem display o jogo segue e o saldo aparece na tela.
5. O status do painel diz a verdade sobre a ligação:
   - `Display (COMx): ✓ mostrando R$…` — o display confirmou o saldo (ACK). É a única indicação de que está tudo certo.
   - `porta aberta, Arduino não responde` — a porta abriu, mas nenhum quadro válido voltou: grave este sketch ou troque o cabo.
   - `porta indisponível` — outro programa está com a porta (feche o Serial Monitor / Arduino IDE) ou o cabo saiu.
   No início da partida o cofre está em R$0,00; com o sketch animado o display sai do modo exibição para o modo jogo
   quando o jogo conecta, o que mostra a conexão mesmo com saldo zero.

Teste do caminho do próprio jogo com o hardware (não roda na suíte normal):
`$env:MONEYRANK_COFFER_PORT='COM12'; unity test . --mode EditMode --filter V4CofferHardwareTests` e, na cena,
`unity test . --mode PlayMode --filter V4DisplayHardwareSceneTests`.

Estado em 27/11/2026: **OLED testado em hardware real** (Arduino Uno clone CH340, OLED SSD1306 em 0x3C) com o
`Test-CofreDisplay.ps1` — `PRONTO` no boot, `ACK` correto para 0, 150, 28.260 e 1.234.567 CRIA Coin, e quadro com CRC
errado recusado. O LCD continua **sem teste em hardware real**. Se o OLED não responder, use o diagnóstico em
`Docs/Hardware/OledNumberTest` (o primeiro problema real foi mau contato nos jumpers macho-fêmea).
