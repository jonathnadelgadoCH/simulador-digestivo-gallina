# Flujo de narración

El guion base en español se encuentra en `Documentation/NARRATION_SCRIPT_ES.md`. Está dividido en introducción, trece etapas digestivas y cierre opcional, con nombres de archivo y referencias de respaldo.

## Generación automática

El manifiesto editable está en `Tools/Narration/narration.es-chicken.json`. Para sintetizar los WAV maestros con Microsoft Sabina y producir los MP3 de Unity/WebGL:

```powershell
pwsh -ExecutionPolicy Bypass -File Tools\Narration\generate-narration.ps1
pwsh -ExecutionPolicy Bypass -File Tools\Narration\validate-narration.ps1
```

El generador admite Windows SAPI, Edge TTS y Piper. Para generación completamente local se recomienda Piper con el modelo `es_MX-ald-medium`; FFmpeg y FFprobe deben estar disponibles en `PATH`. Los WAV se guardan bajo `AudioMasters/narration/` y los MP3 normalizados a 44,1 kHz mono bajo `StreamingAssets/DigestiveSimulator/audio/narration/`. Se utiliza MP3 porque la carga dinámica de OGG mediante `UnityWebRequest` no es compatible con el reproductor WebGL actual.

Para instalar Piper y descargar la voz local:

```powershell
py -m pip install --user piper-tts
py -m piper.download_voices --download-dir Tools/Narration/voices es_MX-ald-medium
```

Para generar la narración específica de alimentos sin enviar los textos a un servicio externo:

```powershell
Tools/Narration/generate-narration.ps1 -ManifestPath Tools/Narration/narration.es-chicken-foods.json -Engine Piper
```

Las trece fichas de órganos principales contienen `narrationFile`. Una sobrescritura `audioFile` específica de un perfil gallina–alimento conserva prioridad sobre la narración general del órgano. Los clips del manifiesto `narration.es-chicken-foods.json` explican por separado el comportamiento de cada grano, harina y aceite en molleja, duodeno e íleon.

1. Los estudiantes terminan y validan el guion con bibliografía científica.
2. Se genera una versión sintetizada o un integrante graba cada fragmento por órgano o etapa.
3. Se conserva WAV como archivo maestro de edición.
4. Se exporta MP3 optimizado para WebGL.
5. El archivo se coloca bajo `StreamingAssets/DigestiveSimulator/audio/narration/`.
6. La ruta se agrega a `narrationFile` del órgano o a `audioFile` del perfil especie–alimento.

`AudioNarrationManager` carga mediante `UnityWebRequest`, por lo que funciona tanto en Editor como en WebGL. La simulación espera a que terminen la duración visual y la narración antes de avanzar automáticamente.
