Add-Type -AssemblyName System.Drawing

function Draw-UltraTaskIcon([int]$size) {
    $bmp = New-Object System.Drawing.Bitmap($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode   = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceOver
    $g.Clear([System.Drawing.Color]::Transparent)

    $s = $size / 100.0

    # helper: GraphicsPath de retângulo arredondado
    function RoundRect($x, $y, $w, $h, $r) {
        $p = New-Object System.Drawing.Drawing2D.GraphicsPath
        $r2 = $r * 2
        $p.AddArc($x,           $y,           $r2, $r2, 180, 90)
        $p.AddArc($x+$w-$r2,    $y,           $r2, $r2, 270, 90)
        $p.AddArc($x+$w-$r2,    $y+$h-$r2,    $r2, $r2,   0, 90)
        $p.AddArc($x,           $y+$h-$r2,    $r2, $r2,  90, 90)
        $p.CloseFigure()
        return $p
    }

    $borderColor = [System.Drawing.ColorTranslator]::FromHtml("#CBD5E1")
    $pen15 = New-Object System.Drawing.Pen($borderColor, [float](1.5 * $s))

    # --- barra 1 (opacidade 1.0) ---
    $b1 = RoundRect ([float](10*$s)) ([float](13*$s)) ([float](76*$s)) ([float](20*$s)) ([float](7*$s))
    $g.FillPath((New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255,255,255,255))), $b1)
    $g.DrawPath($pen15, $b1)
    if ($size -ge 24) {
        $g.FillRectangle((New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(64,136,136,136))),
            ([float](20*$s)), ([float](20*$s)), ([float](30*$s)), ([float](3*$s)))
        $g.FillRectangle((New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(46,136,136,136))),
            ([float](54*$s)), ([float](20*$s)), ([float](18*$s)), ([float](3*$s)))
    }

    # --- barra 2 (opacidade 0.65) ---
    $b2 = RoundRect ([float](10*$s)) ([float](40*$s)) ([float](76*$s)) ([float](20*$s)) ([float](7*$s))
    $g.FillPath((New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(166,255,255,255))), $b2)
    $g.DrawPath($pen15, $b2)
    if ($size -ge 24) {
        $g.FillRectangle((New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(51,136,136,136))),
            ([float](20*$s)), ([float](47*$s)), ([float](36*$s)), ([float](3*$s)))
        $g.FillRectangle((New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(38,136,136,136))),
            ([float](60*$s)), ([float](47*$s)), ([float](14*$s)), ([float](3*$s)))
    }

    # --- barra 3 (opacidade 0.32) ---
    $b3 = RoundRect ([float](10*$s)) ([float](67*$s)) ([float](76*$s)) ([float](20*$s)) ([float](7*$s))
    $g.FillPath((New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(82,255,255,255))), $b3)
    $g.DrawPath($pen15, $b3)
    if ($size -ge 24) {
        $g.FillRectangle((New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(38,136,136,136))),
            ([float](20*$s)), ([float](74*$s)), ([float](24*$s)), ([float](3*$s)))
    }

    # --- sparkle Ultrasoft — raio e posição ajustados por tamanho para não clipar ---
    $sparkleRadiusPct = switch ($size) {
        16  { 0.34 }
        32  { 0.30 }
        48  { 0.27 }
        default { 0.24 }
    }
    $r = [float]($size * $sparkleRadiusPct)
    # cx/cy em % do size — limitados a (size - r) para não clipar na borda
    $cxPct = switch ($size) {
        16  { 0.60 }
        32  { 0.65 }
        48  { 0.69 }
        default { 0.72 }
    }
    $cyPct = switch ($size) {
        16  { 0.60 }
        32  { 0.65 }
        48  { 0.70 }
        default { 0.73 }
    }
    $cx = [float]([Math]::Min($size * $cxPct, $size - $r - 1))
    $cy = [float]([Math]::Min($size * $cyPct, $size - $r - 1))
    $cp = [float](0.36 * $r)   # fator bezier das pontas

    $sparkle = New-Object System.Drawing.Drawing2D.GraphicsPath
    $sparkle.AddBezier($cx,       $cy-$r,  $cx+$cp, $cy-$cp, $cx+$cp, $cy-$cp, $cx+$r,  $cy)
    $sparkle.AddBezier($cx+$r,    $cy,     $cx+$cp, $cy+$cp, $cx+$cp, $cy+$cp, $cx,     $cy+$r)
    $sparkle.AddBezier($cx,       $cy+$r,  $cx-$cp, $cy+$cp, $cx-$cp, $cy+$cp, $cx-$r,  $cy)
    $sparkle.AddBezier($cx-$r,    $cy,     $cx-$cp, $cy-$cp, $cx-$cp, $cy-$cp, $cx,     $cy-$r)
    $sparkle.CloseFigure()

    $g.FillPath((New-Object System.Drawing.SolidBrush([System.Drawing.ColorTranslator]::FromHtml("#C89B3C"))), $sparkle)

    $g.Dispose()
    return $bmp
}

# Gera PNG de cada tamanho em memória
$sizes    = @(16, 32, 48, 256)
$pngBytes = @()

foreach ($sz in $sizes) {
    $bmp = Draw-UltraTaskIcon $sz
    $ms  = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $pngBytes += ,($ms.ToArray())
    $bmp.Dispose(); $ms.Dispose()
}

# Monta ICO com PNGs embarcados
$count      = $sizes.Count
$dataOffset = 6 + 16 * $count
$offsets    = @(); $off = $dataOffset
for ($i = 0; $i -lt $count; $i++) { $offsets += $off; $off += $pngBytes[$i].Length }

$ico = New-Object System.IO.MemoryStream
$ico.Write([byte[]](0,0,1,0,[byte]($count -band 0xFF),[byte](($count -shr 8) -band 0xFF)), 0, 6)

for ($i = 0; $i -lt $count; $i++) {
    $sz  = $sizes[$i]
    $w   = if ($sz -eq 256) { 0 } else { [byte]$sz }
    $h   = if ($sz -eq 256) { 0 } else { [byte]$sz }
    $len = $pngBytes[$i].Length
    $o   = $offsets[$i]
    $entry = [byte[]]($w,$h, 0,0, 1,0, 32,0,
        [byte]($len -band 0xFF),[byte](($len -shr 8) -band 0xFF),[byte](($len -shr 16) -band 0xFF),[byte](($len -shr 24) -band 0xFF),
        [byte]($o   -band 0xFF),[byte](($o   -shr 8) -band 0xFF),[byte](($o   -shr 16) -band 0xFF),[byte](($o   -shr 24) -band 0xFF))
    $ico.Write($entry, 0, 16)
}
foreach ($png in $pngBytes) { $ico.Write($png, 0, $png.Length) }

$out = "C:\Users\mvsiq\Downloads\apps\UltraTask2\UltraTask\Resources\app-icon.ico"
[System.IO.File]::WriteAllBytes($out, $ico.ToArray())
Write-Host "ICO gerado: $out ($($ico.Length) bytes, tamanhos: $($sizes -join ', ')px)"
