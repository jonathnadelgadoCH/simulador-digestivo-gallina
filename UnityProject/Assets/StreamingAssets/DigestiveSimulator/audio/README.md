# Narraciones

Coloque aquí los archivos aprobados para producción. Para la carga dinámica en WebGL, use MP3; conserve WAV como maestro de edición.

Convención sugerida:

```text
audio/narration/es-GT/chicken/01_beak.mp3
audio/narration/es-GT/chicken/02_oral_cavity.mp3
audio/narration/es-GT/chicken/03_pharynx.mp3
...
```

La ruta se registra en `OrganDefinition.narrationFile` o en el `audioFile` de una sobrescritura de etapa dentro de `SpeciesFoodProfile`. Todas las rutas son relativas a `StreamingAssets/DigestiveSimulator/`.

No se incluyen grabaciones provisionales: el audio debe basarse en el guion científico elaborado y validado por los estudiantes.
