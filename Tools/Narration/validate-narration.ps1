[CmdletBinding()]
param([string]$ManifestPath)

$ErrorActionPreference = 'Stop'
$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
if ([string]::IsNullOrWhiteSpace($ManifestPath)) {
    $ManifestPath = Join-Path $PSScriptRoot 'narration.es-chicken.json'
}

$manifest = Get-Content -LiteralPath $ManifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
$runtimeRoot = Join-Path $projectRoot 'UnityProject\Assets\StreamingAssets\DigestiveSimulator'
$failures = [System.Collections.Generic.List[string]]::new()
$ffprobe = Get-Command ffprobe -ErrorAction SilentlyContinue
if ($null -eq $ffprobe) {
    throw 'FFprobe no está disponible en PATH; no es posible comprobar los MP3.'
}

foreach ($clip in $manifest.clips) {
    $relativePath = "audio/narration/$($manifest.language)/$($manifest.speciesId)/$($clip.fileName).mp3"
    $absolutePath = Join-Path $runtimeRoot ($relativePath -replace '/', '\')
    if (-not (Test-Path -LiteralPath $absolutePath -PathType Leaf)) {
        $failures.Add("Falta $relativePath")
    } elseif ((Get-Item -LiteralPath $absolutePath).Length -le 0) {
        $failures.Add("Está vacío $relativePath")
    } else {
        $audioInfo = [string](& $ffprobe.Source -v error -select_streams a:0 `
            -show_entries stream=codec_name,sample_rate,channels -of csv=p=0 $absolutePath)
        if ($LASTEXITCODE -ne 0 -or $audioInfo.Trim() -ne 'mp3,44100,1') {
            $failures.Add("Formato inesperado en ${relativePath}: '$($audioInfo.Trim())'; se esperaba mp3,44100,1")
        }
    }

    if (-not [string]::IsNullOrWhiteSpace([string]$clip.organId)) {
        $organPath = Join-Path $runtimeRoot "species\$($manifest.speciesId)\organs\$($clip.organId)\organ.json"
        if (-not (Test-Path -LiteralPath $organPath -PathType Leaf)) {
            $failures.Add("No existe la ficha del órgano $($clip.organId)")
            continue
        }
        $organ = Get-Content -LiteralPath $organPath -Raw -Encoding UTF8 | ConvertFrom-Json
        if ([string]$organ.narrationFile -ne $relativePath) {
            $failures.Add("$($clip.organId): narrationFile no coincide con $relativePath")
        }
    }
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Host "Narración validada: $($manifest.clips.Count) clips y todas las rutas de órganos coinciden."
