# publish.ps1 — gera executável single-file self-contained para Windows x64
# Uso: .\publish.ps1 [-Version "1.2.3"]

param(
    [string]$Version = "1.0.0"
)

$project  = "UltraTask\UltraTask.csproj"
$outDir   = "dist\UltraTask-$Version"
$runtime  = "win-x64"

Write-Host "=== UltraTask publish v$Version ===" -ForegroundColor Cyan

# Limpa destino anterior
if (Test-Path $outDir) {
    Remove-Item $outDir -Recurse -Force
}

dotnet publish $project `
    -c Release `
    -r $runtime `
    --self-contained true `
    /p:PublishSingleFile=true `
    /p:PublishReadyToRun=true `
    /p:IncludeNativeLibrariesForSelfExtract=true `
    /p:EnableCompressionInSingleFile=true `
    /p:DebugType=embedded `
    /p:Version=$Version `
    /p:FileVersion="$Version.0" `
    /p:AssemblyVersion="$Version.0" `
    -o $outDir

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERRO: publish falhou." -ForegroundColor Red
    exit 1
}

# Lista o que foi gerado
$exe = Get-ChildItem $outDir -Filter "*.exe" | Select-Object -First 1
if ($exe) {
    $sizeMb = [math]::Round($exe.Length / 1MB, 1)
    Write-Host ""
    Write-Host "Publicado com sucesso:" -ForegroundColor Green
    Write-Host "  $($exe.FullName)  ($sizeMb MB)" -ForegroundColor Green
} else {
    Write-Host "Aviso: nenhum .exe encontrado em $outDir" -ForegroundColor Yellow
}
