param(
    [string]$Logo = "Assets\Resources\MoneyRankLogo.png",
    [string]$OutHeader = "Docs\Hardware\cofre_display\oled_art.h",
    [string]$PreviewDir = "",
    [int]$LogoHeight = 48,
    [int]$AlphaThreshold = 110,
    # Pixels mais claros que isto (soma R+G+B) ficam apagados: separa o preto/amarelo do contorno branco.
    [int]$WhiteCutoff = 560
)

# Gera oled_art.h (bitmaps em PROGMEM no formato de páginas do SSD1306: 1 byte = 8 pixels verticais,
# bit 0 = linha de cima) a partir da logo PNG e das capivaras em pixel art definidas abaixo.
# Uso (na raiz do projeto): powershell -ExecutionPolicy Bypass -File Docs\Hardware\cofre_display\tools\make_oled_art.ps1
# -PreviewDir <pasta> grava PNGs ampliados para conferir o resultado.

Add-Type -AssemblyName System.Drawing
$ErrorActionPreference = 'Stop'

# ----------------------------------------------------------------- pixel art ('#' = pixel aceso)
# Capivara andando para a direita, 28x15, dois quadros (pernas alternadas).
$capyBody = @(
    "..########..................",
    ".############..........##...",
    "################.....######.",
    "########################.###",
    "############################",
    "############################",
    "############################",
    "###########################.",
    "##########################..",
    ".########################...",
    "..######################....",
    "...##################......."
)
$capyLegsA = @(
    "...##..##......##..##.......",
    "...##..##......##..##.......",
    "..##....##....##....##......"
)
$capyLegsB = @(
    "....####........####........",
    "....####........####........",
    "....##.##.......##.##......."
)# Moeda 8x8.
$coin = @(
    "..####..",
    ".#....#.",
    "#..##..#",
    "#.#..#.#",
    "#.#..#.#",
    "#..##..#",
    ".#....#.",
    "..####.."
)

function ConvertTo-Grid([string[]]$rows) {
    $h = $rows.Count; $w = $rows[0].Length
    foreach ($r in $rows) { if ($r.Length -ne $w) { throw "Linha com largura diferente: '$r'" } }
    $grid = New-Object 'bool[,]' $w, $h
    for ($y = 0; $y -lt $h; $y++) { for ($x = 0; $x -lt $w; $x++) { $grid[$x, $y] = $rows[$y][$x] -eq '#' } }
    return ,$grid
}

function ConvertFrom-Logo([string]$path, [int]$height, [int]$threshold) {
    $src = [System.Drawing.Bitmap]::FromFile((Resolve-Path $path))
    try {
        $width = [int][math]::Round($src.Width * $height / $src.Height)
        if ($width -gt 128) { $width = 128; $height = [int][math]::Round($src.Height * 128 / $src.Width) }
        $dst = New-Object System.Drawing.Bitmap $width, $height
        $g = [System.Drawing.Graphics]::FromImage($dst)
        $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $g.Clear([System.Drawing.Color]::Transparent)
        $g.DrawImage($src, 0, 0, $width, $height)
        $g.Dispose()
        $grid = New-Object 'bool[,]' $width, $height
        for ($y = 0; $y -lt $height; $y++) {
            for ($x = 0; $x -lt $width; $x++) { $c = $dst.GetPixel($x, $y); $grid[$x, $y] = ($c.A -ge $threshold) -and (($c.R + $c.G + $c.B) -lt $WhiteCutoff) }
        }
        $dst.Dispose()
        return ,$grid
    } finally { $src.Dispose() }
}

# Converte uma grade em bytes de página (colunas de 8 px), altura arredondada para múltiplo de 8.
function ConvertTo-PageBytes([bool[,]]$grid) {
    $w = $grid.GetLength(0); $h = $grid.GetLength(1); $pages = [math]::Ceiling($h / 8)
    $bytes = New-Object System.Collections.Generic.List[byte]
    for ($p = 0; $p -lt $pages; $p++) {
        for ($x = 0; $x -lt $w; $x++) {
            $b = 0
            for ($bit = 0; $bit -lt 8; $bit++) { $y = $p * 8 + $bit; if ($y -lt $h -and $grid[$x, $y]) { $b = $b -bor (1 -shl $bit) } }
            $bytes.Add([byte]$b)
        }
    }
    return @{ Width = $w; Height = $h; Pages = $pages; Bytes = $bytes.ToArray() }
}

function Format-Array([string]$name, $art) {
    $sb = New-Object System.Text.StringBuilder
    [void]$sb.AppendLine("// $($art.Width)x$($art.Height) px, $($art.Pages) pagina(s), $($art.Bytes.Length) bytes")
    [void]$sb.AppendLine("const uint8_t $($name.ToUpper())_W = $($art.Width);")
    [void]$sb.AppendLine("const uint8_t $($name.ToUpper())_H = $($art.Height);")
    [void]$sb.AppendLine("const uint8_t $($name.ToUpper())_PAGES = $($art.Pages);")
    [void]$sb.Append("const uint8_t $($name.ToUpper())[] PROGMEM = {")
    for ($i = 0; $i -lt $art.Bytes.Length; $i++) {
        if ($i % 16 -eq 0) { [void]$sb.Append("`n  ") }
        [void]$sb.Append(('0x{0:X2},' -f $art.Bytes[$i]))
    }
    [void]$sb.AppendLine("`n};")
    return $sb.ToString()
}

function Save-Preview([bool[,]]$grid, [string]$path, [int]$scale = 4) {
    $w = $grid.GetLength(0); $h = $grid.GetLength(1)
    $bmp = New-Object System.Drawing.Bitmap ($w * $scale), ($h * $scale)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.Clear([System.Drawing.Color]::FromArgb(8, 10, 20))
    $on = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(120, 200, 255))
    for ($y = 0; $y -lt $h; $y++) { for ($x = 0; $x -lt $w; $x++) { if ($grid[$x, $y]) { $g.FillRectangle($on, $x * $scale, $y * $scale, $scale - 1, $scale - 1) } } }
    $g.Dispose(); $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png); $bmp.Dispose()
}

$logoGrid = ConvertFrom-Logo $Logo $LogoHeight $AlphaThreshold
$capyA = ConvertTo-Grid ($capyBody + $capyLegsA)
$capyB = ConvertTo-Grid ($capyBody + $capyLegsB)
$coinGrid = ConvertTo-Grid $coin

$header = @"
/*
 * Money Rank — arte do display OLED (gerado por tools/make_oled_art.ps1; não edite à mão).
 * Formato de páginas do SSD1306: para cada página (8 linhas), um byte por coluna, bit 0 = linha de cima.
 * Logo: Assets/Resources/MoneyRankLogo.png reduzida para 1 bit. Capivaras e moeda: pixel art do script.
 */
#ifndef OLED_ART_H
#define OLED_ART_H

#include <stdint.h>
#include <avr/pgmspace.h>

namespace art {

$(Format-Array 'logo' (ConvertTo-PageBytes $logoGrid))
$(Format-Array 'capy_a' (ConvertTo-PageBytes $capyA))
$(Format-Array 'capy_b' (ConvertTo-PageBytes $capyB))
$(Format-Array 'coin' (ConvertTo-PageBytes $coinGrid))
}  // namespace art

#endif
"@
[IO.File]::WriteAllText((Join-Path (Get-Location) $OutHeader), $header, (New-Object Text.UTF8Encoding $false))
Write-Host "Gerado: $OutHeader (logo $($logoGrid.GetLength(0))x$($logoGrid.GetLength(1)))"

if ($PreviewDir) {
    New-Item -ItemType Directory -Force $PreviewDir | Out-Null
    Save-Preview $logoGrid (Join-Path $PreviewDir 'logo.png') 5
    Save-Preview $capyA (Join-Path $PreviewDir 'capy_a.png') 12
    Save-Preview $capyB (Join-Path $PreviewDir 'capy_b.png') 12
    Save-Preview $coinGrid (Join-Path $PreviewDir 'coin.png') 12
    Write-Host "Previas em $PreviewDir"
}
