# Teste de bancada do OLED 0,96" (diagnóstico)

Serve só para conferir fiação e se o Arduino enxerga o display. **O jogo não usa este sketch**:
para o jogo, grave `Docs/Hardware/cofre_display` (veja o LEIA-ME de lá).

| Display | Arduino Uno |
|---|---|
| GND | GND |
| VCC | 5V |
| SCL | A5 |
| SDA | A4 |

Gravar (usa o `arduino-cli` que vem com a Arduino IDE 2):

```powershell
& "$env:LOCALAPPDATA\Programs\Arduino IDE\resources\app\lib\backend\resources\arduino-cli.exe" compile --fqbn arduino:avr:uno -u -p COM11 Docs\Hardware\OledNumberTest
```

Testar (feche o Serial Monitor antes):

```powershell
powershell -ExecutionPolicy Bypass -File Docs\Hardware\OledNumberTest\Test-OledDisplay.ps1 -Port COM11
```

Protocolo de diagnóstico (9600 baud, uma linha por comando): `PING` → `PONG`, `SHOW 1500` → `OK 1500`,
`CLEAR` → `OK CLEAR`, `SCAN` → `I2C 0x3C` (ou `I2C NONE`). No boot imprime `READY MONEYRANK-OLED`.

Se o `SCAN` responder `I2C NONE`, a serial está boa e o problema é contato físico. No primeiro teste real
(27/11/2026) a causa foi mau contato nos jumpers macho-fêmea; com protoboard e jumpers macho-macho funcionou.
