param(
    [string]$Port = 'COM11',
    [long[]]$Numbers = @(1500, -42, 2026)
)

# Bench test for Hardware/Arduino/OledNumberTest. Close the Arduino IDE Serial Monitor first.
$serial = New-Object System.IO.Ports.SerialPort $Port, 9600, 'None', 8, 'One'
$serial.NewLine = "`n"
$serial.ReadTimeout = 3000
$serial.DtrEnable = $true

function Send-Command([string]$command) {
    $serial.WriteLine($command)
    try { $reply = $serial.ReadLine().Trim() } catch { $reply = '(sem resposta)' }
    Write-Host ("{0,-12} -> {1}" -f $command, $reply)
    return $reply
}

try {
    $serial.Open()
} catch {
    Write-Host "Nao foi possivel abrir $Port. Porta errada ou em uso (Serial Monitor aberto?)." -ForegroundColor Red
    exit 1
}

try {
    Start-Sleep -Milliseconds 2500   # the Uno resets when the port opens
    $boot = $serial.ReadExisting().Trim()
    Write-Host "Boot: $boot"

    $pong = Send-Command 'PING'
    if ($pong -ne 'PONG') {
        Write-Host 'O Arduino nao respondeu. Grave o sketch OledNumberTest.' -ForegroundColor Red
        exit 2
    }

    $scan = Send-Command 'SCAN'
    if ($scan -match 'NONE') {
        Write-Host 'Serial OK, mas nenhum display I2C encontrado. Confira GND, VCC, SDA (A4) e SCL (A5).' -ForegroundColor Yellow
        exit 3
    }

    foreach ($n in $Numbers) {
        Send-Command "SHOW $n" | Out-Null
        Start-Sleep -Milliseconds 1200
    }
    Write-Host 'Teste concluido. Os numeros devem ter aparecido no display.' -ForegroundColor Green
} finally {
    $serial.Close()
}
