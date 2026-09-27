[CmdletBinding()]
param(
    [string]$ManifestPath,
    [string]$Voice,
    [int]$Rate = 0,
    [ValidateSet('Sapi', 'Edge', 'Piper')]
    [string]$Engine = 'Sapi',
    [string]$EdgeVoice,
    [string]$PiperModelPath,
    [switch]$OnlyMissing
)

$ErrorActionPreference = 'Stop'
$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))

if ([string]::IsNullOrWhiteSpace($ManifestPath)) {
    $ManifestPath = Join-Path $PSScriptRoot 'narration.es-chicken.json'
}
$ManifestPath = [System.IO.Path]::GetFullPath($ManifestPath)
if (-not (Test-Path -LiteralPath $ManifestPath -PathType Leaf)) {
    throw "No se encontró el manifiesto: $ManifestPath"
}

$manifest = Get-Content -LiteralPath $ManifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
if (-not $manifest.clips -or $manifest.clips.Count -eq 0) {
    throw 'El manifiesto no contiene clips.'
}

if ([string]::IsNullOrWhiteSpace($Voice)) {
    $Voice = [string]$manifest.voice
}
if (-not $PSBoundParameters.ContainsKey('Rate')) {
    $Rate = [int]$manifest.rate
}
if ([string]::IsNullOrWhiteSpace($EdgeVoice)) {
    $EdgeVoice = if ([string]::IsNullOrWhiteSpace([string]$manifest.edgeVoice)) {
        'es-GT-MartaNeural'
    } else {
        [string]$manifest.edgeVoice
    }
}
if ([string]::IsNullOrWhiteSpace($PiperModelPath)) {
    $PiperModelPath = Join-Path $PSScriptRoot 'voices\es_MX-ald-medium.onnx'
}
$PiperModelPath = [System.IO.Path]::GetFullPath($PiperModelPath)

$ffmpeg = Get-Command ffmpeg -ErrorAction SilentlyContinue
if ($null -eq $ffmpeg) {
    throw 'FFmpeg no está disponible en PATH. Instálelo o agregue su carpeta bin a PATH.'
}
$ffprobe = Get-Command ffprobe -ErrorAction SilentlyContinue

$synthesizer = $null
if ($Engine -eq 'Sapi') {
    $synthesizer = New-Object -ComObject SAPI.SpVoice
    $voiceTokens = $synthesizer.GetVoices()
    $selectedVoice = $null
    $voiceDescriptions = [System.Collections.Generic.List[string]]::new()
    for ($index = 0; $index -lt $voiceTokens.Count; $index++) {
        $candidate = $voiceTokens.Item($index)
        $description = [string]$candidate.GetDescription()
        $voiceDescriptions.Add($description)
        if ($null -eq $selectedVoice -and $description.StartsWith($Voice, [System.StringComparison]::OrdinalIgnoreCase)) {
            $selectedVoice = $candidate
        }
    }
    if ($null -eq $selectedVoice) {
        [System.Runtime.InteropServices.Marshal]::FinalReleaseComObject($synthesizer) | Out-Null
        throw "La voz '$Voice' no está instalada. Voces disponibles: $($voiceDescriptions -join ', ')"
    }

    $synthesizer.Voice = $selectedVoice
    $synthesizer.Rate = [Math]::Max(-10, [Math]::Min(10, $Rate))
    $synthesizer.Volume = 100
}
elseif ($Engine -eq 'Edge') {
    if ($null -eq (Get-Command py -ErrorAction SilentlyContinue)) {
        throw 'Python no está disponible para ejecutar Edge TTS.'
    }
    & py -m edge_tts --version | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw 'Edge TTS no está instalado. Ejecute: py -m pip install --user edge-tts'
    }
}
else {
    if ($null -eq (Get-Command py -ErrorAction SilentlyContinue)) {
        throw 'Python no está disponible para ejecutar Piper.'
    }
    if (-not (Test-Path -LiteralPath $PiperModelPath -PathType Leaf)) {
        throw "No se encontró el modelo Piper: $PiperModelPath. Descárguelo con: py -m piper.download_voices --download-dir Tools/Narration/voices es_MX-ald-medium"
    }
    & py -m piper --help | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw 'Piper no está instalado. Ejecute: py -m pip install --user piper-tts'
    }
}

$masterRoot = Join-Path $projectRoot "AudioMasters\narration\$($manifest.language)\$($manifest.speciesId)"
$runtimeRoot = Join-Path $projectRoot "UnityProject\Assets\StreamingAssets\DigestiveSimulator\audio\narration\$($manifest.language)\$($manifest.speciesId)"
New-Item -ItemType Directory -Force -Path $masterRoot, $runtimeRoot | Out-Null

$results = [System.Collections.Generic.List[object]]::new()
try {
    foreach ($clip in ($manifest.clips | Sort-Object order)) {
        if ([string]::IsNullOrWhiteSpace([string]$clip.fileName) -or [string]::IsNullOrWhiteSpace([string]$clip.text)) {
            throw "El clip de orden $($clip.order) no tiene fileName o text."
        }

        $wavPath = Join-Path $masterRoot "$($clip.fileName).wav"
        $mp3Path = Join-Path $runtimeRoot "$($clip.fileName).mp3"
        if ($OnlyMissing -and (Test-Path -LiteralPath $wavPath) -and (Test-Path -LiteralPath $mp3Path)) {
            Write-Host "Omitido: $($clip.fileName)"
            continue
        }

        Write-Host "Sintetizando: $($clip.fileName)"
        if ($Engine -eq 'Edge') {
            $temporaryMp3 = Join-Path ([System.IO.Path]::GetTempPath()) "digestive-$([guid]::NewGuid().ToString('N')).mp3"
            try {
                & py -m edge_tts --voice $EdgeVoice --rate '+0%' --text ([string]$clip.text) --write-media $temporaryMp3
                if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $temporaryMp3)) {
                    throw "Edge TTS no pudo sintetizar: $($clip.fileName)"
                }
                & $ffmpeg.Source -y -hide_banner -loglevel error -i $temporaryMp3 -ar 44100 -ac 1 -c:a pcm_s16le $wavPath
                if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $wavPath)) {
                    throw "FFmpeg no pudo crear el WAV maestro: $wavPath"
                }
            }
            finally {
                Remove-Item -LiteralPath $temporaryMp3 -Force -ErrorAction SilentlyContinue
            }
        }
        elseif ($Engine -eq 'Piper') {
            $temporaryText = Join-Path ([System.IO.Path]::GetTempPath()) "digestive-$([guid]::NewGuid().ToString('N')).txt"
            try {
                [System.IO.File]::WriteAllText($temporaryText, [string]$clip.text, [System.Text.UTF8Encoding]::new($false))
                & py -m piper -m $PiperModelPath -f $wavPath --sentence-silence 0.2 --input-file $temporaryText
                if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $wavPath)) {
                    throw "Piper no pudo crear el WAV maestro: $wavPath"
                }
            }
            finally {
                Remove-Item -LiteralPath $temporaryText -Force -ErrorAction SilentlyContinue
            }
        }
        else {
            $waveStream = New-Object -ComObject SAPI.SpFileStream
            $waveFormat = New-Object -ComObject SAPI.SpAudioFormat
            try {
                # SpeechAudioFormatType 34 = PCM, 44.1 kHz, 16 bits, mono.
                $waveFormat.Type = 34
                $waveStream.Format = $waveFormat
                # SpeechStreamFileMode 3 = crear o reemplazar para escritura.
                $waveStream.Open($wavPath, 3, $false)
                $synthesizer.AudioOutputStream = $waveStream
                [void]$synthesizer.Speak([string]$clip.text, 0)
                $waveStream.Close()
            }
            finally {
                if ($null -ne $waveStream) {
                    try { $waveStream.Close() } catch { }
                    [System.Runtime.InteropServices.Marshal]::FinalReleaseComObject($waveStream) | Out-Null
                }
                if ($null -ne $waveFormat) {
                    [System.Runtime.InteropServices.Marshal]::FinalReleaseComObject($waveFormat) | Out-Null
                }
            }
        }

        & $ffmpeg.Source -y -hide_banner -loglevel error `
            -i $wavPath `
            -af 'adelay=250,apad=pad_dur=0.35,loudnorm=I=-18:TP=-2:LRA=7' `
            -ar 44100 -ac 1 -c:a libmp3lame -b:a 96k $mp3Path
        if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $mp3Path)) {
            throw "FFmpeg no pudo crear: $mp3Path"
        }

        $duration = $null
        if ($null -ne $ffprobe) {
            $rawDuration = & $ffprobe.Source -v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 $mp3Path
            if ($LASTEXITCODE -eq 0) {
                $parsedDuration = 0.0
                if ([double]::TryParse(
                    [string]$rawDuration,
                    [System.Globalization.NumberStyles]::Float,
                    [System.Globalization.CultureInfo]::InvariantCulture,
                    [ref]$parsedDuration
                )) {
                    $duration = [Math]::Round($parsedDuration, 2)
                }
            }
        }

        $results.Add([pscustomobject]@{
            Order = [int]$clip.order
            OrganId = [string]$clip.organId
            File = "audio/narration/$($manifest.language)/$($manifest.speciesId)/$($clip.fileName).mp3"
            DurationSeconds = $duration
            Mp3Bytes = (Get-Item -LiteralPath $mp3Path).Length
        })
    }
}
finally {
    if ($null -ne $synthesizer) {
        [System.Runtime.InteropServices.Marshal]::FinalReleaseComObject($synthesizer) | Out-Null
    }
}

Write-Host ''
$voiceLabel = if ($Engine -eq 'Edge') { $EdgeVoice } elseif ($Engine -eq 'Piper') { [System.IO.Path]::GetFileNameWithoutExtension($PiperModelPath) } else { $Voice }
Write-Host "Motor: $Engine | Voz: $voiceLabel | Velocidad: $Rate"
Write-Host "WAV maestros: $masterRoot"
Write-Host "MP3 para Unity/WebGL: $runtimeRoot"
$results | Format-Table -AutoSize
