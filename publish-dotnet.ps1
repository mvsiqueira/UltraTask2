# publish-dotnet.ps1 — publica como DLL framework-dependent (requer dotnet instalado)
# Uso: .\publish-dotnet.ps1 [-Version "2.0.0"]
# Iniciar com: dotnet "caminho\UltraTask.dll"

param(
    [string]$Version = "2.0.0"
)

$project = "UltraTask\UltraTask.csproj"
$outDir  = "dist\UltraTask-$Version-dotnet"

Write-Host "=== UltraTask publish (framework-dependent) v$Version ===" -ForegroundColor Cyan

if (Test-Path $outDir) {
    Remove-Item $outDir -Recurse -Force
}

dotnet publish $project `
    -c Release `
    --self-contained false `
    /p:PublishSingleFile=false `
    /p:Version=$Version `
    /p:FileVersion="$Version.0" `
    /p:AssemblyVersion="$Version.0" `
    -o $outDir

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERRO: publish falhou." -ForegroundColor Red
    exit 1
}

$dll = Get-ChildItem $outDir -Filter "UltraTask.dll" | Select-Object -First 1
if ($dll) {
    $sizeMb = [math]::Round((Get-ChildItem $outDir | Measure-Object -Property Length -Sum).Sum / 1MB, 1)

    # Recria o launcher VBS
    $vbsPath = Join-Path $outDir "UltraTask.vbs"
    @'
Dim dll
dll = CreateObject("Scripting.FileSystemObject").GetParentFolderName(WScript.ScriptFullName) & "\UltraTask.dll"
CreateObject("WScript.Shell").Run "dotnet """ & dll & """", 0, False
'@ | Set-Content $vbsPath -Encoding UTF8

    Write-Host ""
    Write-Host "Publicado com sucesso:" -ForegroundColor Green
    Write-Host "  Pasta: $outDir  ($sizeMb MB total)" -ForegroundColor Green
    Write-Host "  VBS:   $vbsPath" -ForegroundColor Green
    Write-Host ""
    Write-Host "Para executar: clique duplo em UltraTask.vbs" -ForegroundColor Yellow
} else {
    Write-Host "Aviso: UltraTask.dll nao encontrada em $outDir" -ForegroundColor Yellow
}
