param(
    [string]$Port = 'COM11',
    # Saldos em CRIA Coin (inteiros). O quadro leva centavos: 28260 -> 2826000.
    [int[]]$Balances = @(0, 150, 28260, 1234567)
)

# Teste de bancada do display do cofre sem abrir o jogo: fala o mesmo protocolo do Unity
# (cofre_protocol.h / V4Coffer.cs). Feche o Serial Monitor da Arduino IDE antes.

function Get-Crc8([string]$text) {
    $crc = 0
    foreach ($b in [Text.Encoding]::ASCII.GetBytes($text)) {
        $crc = $crc -bxor $b
        for ($i = 0; $i -lt 8; $i++) {
            if ($crc -band 0x80) { $crc = (($crc -shl 1) -bxor 0x07) -band 0xFF } else { $crc = ($crc -shl 1) -band 0xFF }
        }
    }
    return $crc
}

function Seal([string]$body) { return '{0}*{1:X2}' -f $body, (Get-Crc8 $body) }

$serial = New-Object System.IO.Ports.SerialPort $Port, 115200, 'None', 8, 'One'
$serial.NewLine = "`n"
$serial.ReadTimeout = 2000
$serial.DtrEnable = $true

try { $serial.Open() } catch {
    Write-Host "Nao foi possivel abrir $Port. Porta errada ou em uso (Serial Monitor ou o jogo conectado?)." -ForegroundColor Red
    exit 1
}

try {
    Start-Sleep -Milliseconds 2500   # o Uno reinicia ao abrir a porta
    $boot = $serial.ReadExisting().Trim()
    Write-Host "Boot: $boot"
    if ($boot -notmatch '^PRONTO\|') {
        Write-Host 'O Arduino nao mandou PRONTO. Grave o sketch Docs\Hardware\cofre_display.' -ForegroundColor Red
        exit 2
    }

    $failures = 0
    $seq = 0
    foreach ($balance in $Balances) {
        $seq++
        $cents = [long]$balance * 100
        $frame = Seal "COFRE|$seq|$cents"
        $serial.WriteLine($frame)
        try { $reply = $serial.ReadLine().Trim() } catch { $reply = '(sem resposta)' }
        $expected = Seal "ACK|$seq|$cents"
        $ok = $reply -eq $expected
        if (-not $ok) { $failures++ }
        Write-Host ("{0,-24} -> {1,-24} {2}" -f $frame, $reply, $(if ($ok) { 'OK' } else { 'FALHOU' }))
        Start-Sleep -Milliseconds 1500
    }

    # Quadro com CRC errado precisa ser recusado e nao mudar o display.
    $serial.WriteLine("COFRE|99|100*00")
    try { $reply = $serial.ReadLine().Trim() } catch { $reply = '(sem resposta)' }
    $okErr = $reply -match '^ERR\|'
    if (-not $okErr) { $failures++ }
    Write-Host ("{0,-24} -> {1,-24} {2}" -f 'COFRE|99|100*00 (CRC ruim)', $reply, $(if ($okErr) { 'OK (recusado)' } else { 'FALHOU' }))

    if ($failures -eq 0) {
        Write-Host 'Teste concluido: o display deve estar mostrando o ultimo saldo enviado.' -ForegroundColor Green
    } else {
        Write-Host "$failures verificacao(oes) falharam." -ForegroundColor Red
        exit 3
    }
} finally {
    $serial.Close()
}
