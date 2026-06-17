# create-msix.ps1 — empacota UltraTask como MSIX assinado com certificado autoassinado
# Uso: .\tools\create-msix.ps1
# Para instalar no trabalho: instale UltraTask.cer como "Trusted People" (admin) e depois clique no .msix

param([string]$Version = "2.0.0.0")

$sdk      = "C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x64"
$makeappx = "$sdk\makeappx.exe"
$signtool = "$sdk\signtool.exe"
$publisher = "CN=Ultrasoft"
$outDir    = "dist\UltraTask-msix"
$staging   = "dist\_msix_staging"
$msixPath  = "$outDir\UltraTask.msix"
$pfxPath   = "$outDir\UltraTask.pfx"
$cerPath   = "$outDir\Ultrasoft.cer"

Write-Host "=== UltraTask MSIX v$Version ===" -ForegroundColor Cyan

# --- Limpa e cria diretórios ---
foreach ($d in @($outDir, $staging, "$staging\Assets")) {
    if (Test-Path $d) { Remove-Item $d -Recurse -Force }
    New-Item -ItemType Directory $d | Out-Null
}

# --- 1. Publica app (self-contained, multi-file para compatibilidade MSIX) ---
Write-Host "`n[1/5] Publicando app..." -ForegroundColor Yellow
dotnet publish UltraTask\UltraTask.csproj `
    -c Release -r win-x64 --self-contained true `
    /p:PublishSingleFile=false `
    /p:DebugType=none /p:DebugSymbols=false `
    /p:Version=$Version `
    -o "$staging\app" | Out-Null

if ($LASTEXITCODE -ne 0) { Write-Host "ERRO: publish falhou." -ForegroundColor Red; exit 1 }
Write-Host "   OK — $(((Get-ChildItem "$staging\app" -Recurse | Measure-Object Length -Sum).Sum / 1MB).ToString('0.0')) MB" -ForegroundColor Green

# --- 2. Gera ícones nos tamanhos exigidos pelo MSIX ---
Write-Host "`n[2/5] Gerando assets de ícone..." -ForegroundColor Yellow
Add-Type -AssemblyName System.Drawing

function Draw-Icon([int]$size) {
    $bmp = New-Object System.Drawing.Bitmap($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g   = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode   = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceOver
    $g.Clear([System.Drawing.Color]::Transparent)
    $s = $size / 100.0

    function RoundRect($x,$y,$w,$h,$r) {
        $p = New-Object System.Drawing.Drawing2D.GraphicsPath; $r2=$r*2
        $p.AddArc($x,$y,$r2,$r2,180,90); $p.AddArc($x+$w-$r2,$y,$r2,$r2,270,90)
        $p.AddArc($x+$w-$r2,$y+$h-$r2,$r2,$r2,0,90); $p.AddArc($x,$y+$h-$r2,$r2,$r2,90,90)
        $p.CloseFigure(); return $p
    }
    $pen = New-Object System.Drawing.Pen([System.Drawing.ColorTranslator]::FromHtml("#CBD5E1"), [float](1.5*$s))

    $b1 = RoundRect ([float](10*$s)) ([float](13*$s)) ([float](76*$s)) ([float](20*$s)) ([float](7*$s))
    $g.FillPath((New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255,255,255,255))), $b1)
    $g.DrawPath($pen, $b1)
    if ($size -ge 24) {
        $g.FillRectangle((New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(64,136,136,136))),([float](20*$s)),([float](20*$s)),([float](30*$s)),([float](3*$s)))
        $g.FillRectangle((New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(46,136,136,136))),([float](54*$s)),([float](20*$s)),([float](18*$s)),([float](3*$s)))
    }
    $b2 = RoundRect ([float](10*$s)) ([float](40*$s)) ([float](76*$s)) ([float](20*$s)) ([float](7*$s))
    $g.FillPath((New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(166,255,255,255))), $b2)
    $g.DrawPath($pen, $b2)
    if ($size -ge 24) {
        $g.FillRectangle((New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(51,136,136,136))),([float](20*$s)),([float](47*$s)),([float](36*$s)),([float](3*$s)))
        $g.FillRectangle((New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(38,136,136,136))),([float](60*$s)),([float](47*$s)),([float](14*$s)),([float](3*$s)))
    }
    $b3 = RoundRect ([float](10*$s)) ([float](67*$s)) ([float](76*$s)) ([float](20*$s)) ([float](7*$s))
    $g.FillPath((New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(82,255,255,255))), $b3)
    $g.DrawPath($pen, $b3)
    if ($size -ge 24) {
        $g.FillRectangle((New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(38,136,136,136))),([float](20*$s)),([float](74*$s)),([float](24*$s)),([float](3*$s)))
    }

    $rPct = switch ($size) { 16{0.34} 32{0.30} 48{0.27} 50{0.27} 44{0.28} default{0.24} }
    $r    = [float]($size * $rPct)
    $cxPct = switch ($size) { 16{0.60} 32{0.65} 48{0.69} 50{0.69} 44{0.68} default{0.72} }
    $cyPct = switch ($size) { 16{0.60} 32{0.65} 48{0.70} 50{0.70} 44{0.69} default{0.73} }
    $cx = [float]([Math]::Min($size*$cxPct, $size-$r-1))
    $cy = [float]([Math]::Min($size*$cyPct, $size-$r-1))
    $cp = [float](0.36*$r)

    $sp = New-Object System.Drawing.Drawing2D.GraphicsPath
    $sp.AddBezier($cx,$cy-$r,$cx+$cp,$cy-$cp,$cx+$cp,$cy-$cp,$cx+$r,$cy)
    $sp.AddBezier($cx+$r,$cy,$cx+$cp,$cy+$cp,$cx+$cp,$cy+$cp,$cx,$cy+$r)
    $sp.AddBezier($cx,$cy+$r,$cx-$cp,$cy+$cp,$cx-$cp,$cy+$cp,$cx-$r,$cy)
    $sp.AddBezier($cx-$r,$cy,$cx-$cp,$cy-$cp,$cx-$cp,$cy-$cp,$cx,$cy-$r)
    $sp.CloseFigure()
    $g.FillPath((New-Object System.Drawing.SolidBrush([System.Drawing.ColorTranslator]::FromHtml("#C89B3C"))), $sp)
    $g.Dispose()
    return $bmp
}

function Save-Icon([int]$size, [string]$path) {
    $bmp = Draw-Icon $size
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
}

# Tamanhos exigidos pelo MSIX
Save-Icon 44  "$staging\Assets\Square44x44Logo.png"
Save-Icon 16  "$staging\Assets\Square44x44Logo.targetsize-16.png"
Save-Icon 32  "$staging\Assets\Square44x44Logo.targetsize-32.png"
Save-Icon 48  "$staging\Assets\Square44x44Logo.targetsize-48.png"
Save-Icon 150 "$staging\Assets\Square150x150Logo.png"
Save-Icon 50  "$staging\Assets\StoreLogo.png"
Write-Host "   OK" -ForegroundColor Green

# --- 3. Copia arquivos do app para staging ---
Copy-Item "$staging\app\*" $staging -Recurse -Force
Remove-Item "$staging\app" -Recurse -Force

# --- 4. Gera AppxManifest.xml ---
Write-Host "`n[3/5] Criando manifesto..." -ForegroundColor Yellow
$manifest = @"
<?xml version="1.0" encoding="utf-8"?>
<Package
  xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10"
  xmlns:uap="http://schemas.microsoft.com/appx/manifest/uap/windows10"
  xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities"
  IgnorableNamespaces="uap rescap">

  <Identity Name="Ultrasoft.UltraTask" Publisher="$publisher" Version="$Version" ProcessorArchitecture="x64" />

  <Properties>
    <DisplayName>UltraTask</DisplayName>
    <PublisherDisplayName>Ultrasoft</PublisherDisplayName>
    <Logo>Assets\StoreLogo.png</Logo>
  </Properties>

  <Dependencies>
    <TargetDeviceFamily Name="Windows.Desktop" MinVersion="10.0.17763.0" MaxVersionTested="10.0.26100.0" />
  </Dependencies>

  <Resources>
    <Resource Language="pt-BR" />
  </Resources>

  <Applications>
    <Application Id="UltraTask" Executable="UltraTask.exe" EntryPoint="Windows.FullTrustApplication">
      <uap:VisualElements
        DisplayName="UltraTask"
        Description="Gerenciador de tarefas de alta densidade"
        BackgroundColor="transparent"
        Square150x150Logo="Assets\Square150x150Logo.png"
        Square44x44Logo="Assets\Square44x44Logo.png">
        <uap:DefaultTile />
        <uap:SplashScreen Image="Assets\Square150x150Logo.png" />
      </uap:VisualElements>
    </Application>
  </Applications>

  <Capabilities>
    <rescap:Capability Name="runFullTrust" />
  </Capabilities>

</Package>
"@
$manifest | Set-Content "$staging\AppxManifest.xml" -Encoding UTF8
Write-Host "   OK" -ForegroundColor Green

# --- 5. Cria certificado autoassinado (se não existir) ---
Write-Host "`n[4/5] Certificado..." -ForegroundColor Yellow
$certThumb = $null
$existing = Get-ChildItem Cert:\CurrentUser\My | Where-Object { $_.Subject -eq $publisher } | Select-Object -First 1
if ($existing) {
    $certThumb = $existing.Thumbprint
    Write-Host "   Reutilizando certificado existente ($($certThumb.Substring(0,12))...)" -ForegroundColor Green
} else {
    $cert = New-SelfSignedCertificate `
        -Subject $publisher `
        -CertStoreLocation "Cert:\CurrentUser\My" `
        -KeyExportPolicy Exportable `
        -KeyUsage DigitalSignature `
        -FriendlyName "UltraTask Code Signing" `
        -NotAfter (Get-Date).AddYears(5) `
        -Type CodeSigningCert
    $certThumb = $cert.Thumbprint
    Write-Host "   Certificado criado ($($certThumb.Substring(0,12))...)" -ForegroundColor Green
}

# Exporta .cer (chave pública — para instalar no trabalho)
$certObj = Get-Item "Cert:\CurrentUser\My\$certThumb"
$cerBytes = $certObj.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Cert)
[System.IO.File]::WriteAllBytes((Resolve-Path "." | Join-Path -ChildPath $cerPath), $cerBytes)
Write-Host "   Exportado: $cerPath" -ForegroundColor Green

# --- 6. Empacota com makeappx ---
Write-Host "`n[5/5] Empacotando e assinando..." -ForegroundColor Yellow
& $makeappx pack /d $staging /p $msixPath /o 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) { Write-Host "ERRO: makeappx falhou." -ForegroundColor Red; exit 1 }

& $signtool sign /fd SHA256 /sha1 $certThumb /tr http://timestamp.digicert.com /td SHA256 $msixPath 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) {
    # Tenta sem timestamp (pode não ter acesso à internet)
    & $signtool sign /fd SHA256 /sha1 $certThumb $msixPath 2>&1 | Out-Null
}
if ($LASTEXITCODE -ne 0) { Write-Host "ERRO: signtool falhou." -ForegroundColor Red; exit 1 }

# Limpa staging
Remove-Item $staging -Recurse -Force

$sizeMb = [math]::Round((Get-Item $msixPath).Length / 1MB, 1)
Write-Host "`n=== Concluído ===" -ForegroundColor Cyan
Write-Host "  MSIX:  $msixPath  ($sizeMb MB)" -ForegroundColor Green
Write-Host "  CERT:  $cerPath" -ForegroundColor Green
Write-Host ""
Write-Host "Para instalar no trabalho:" -ForegroundColor Yellow
Write-Host "  1. Copie os dois arquivos para o PC do trabalho" -ForegroundColor Yellow
Write-Host "  2. Clique direito em UltraTask.cer → Instalar certificado" -ForegroundColor Yellow
Write-Host "     → Máquina Local → Colocar em 'Pessoas Confiáveis' (requer admin)" -ForegroundColor Yellow
Write-Host "  3. Clique duplo em UltraTask.msix para instalar" -ForegroundColor Yellow
Write-Host "  Obs: o certificado Ultrasoft.cer pode ser reutilizado para assinar outros apps" -ForegroundColor Yellow
