# WebGL y GitHub Pages

1. Abrir el proyecto con Unity 6000.5.7f1 y comprobar que Unity Hub muestre una licencia activa.
2. Ejecutar **Digestive Simulator > Build WebGL**. El script `Assets/Editor/WebGlBuild.cs` usa las escenas habilitadas y genera `WebGLBuild/` en la raíz del repositorio.
3. Para automatización, cerrar el Editor y ejecutar:

   ```powershell
   & 'C:\Program Files\Unity\Hub\Editor\6000.5.7f1\Editor\Unity.exe' -batchmode -quit -projectPath "$PWD\UnityProject" -executeMethod DigestiveSimulator.Editor.WebGlBuild.Build -logFile "$PWD\WebGLBuild.log"
   ```

4. El build versionado puede conservar la compresión gzip de Unity. Para GitHub Pages, `Tools/prepare-github-pages.mjs` genera una copia temporal sin compresión, porque Pages no permite configurar `Content-Encoding` por archivo.
5. Probar `WebGLBuild/` mediante un servidor HTTP local; no abrir `index.html` directamente desde el explorador:

   ```powershell
   node Tools\serve-webgl.mjs WebGLBuild 8000
   ```

   Abrir `http://127.0.0.1:8000/`. El servidor asigna `application/wasm`, `audio/ogg`, `model/gltf-binary` y `Content-Encoding: gzip` cuando corresponde.
6. Validar manualmente el artefacto que usará Pages, si se desea:

   ```powershell
   node Tools\prepare-github-pages.mjs WebGLBuild _site
   node Tools\serve-webgl.mjs _site 8000
   ```

7. Subir la rama `main` a un repositorio de GitHub. El workflow `.github/workflows/pages.yml` conserva `StreamingAssets`, descomprime el runtime en `_site` y publica el artefacto con GitHub Actions.
8. En el repositorio remoto, abrir **Settings > Pages** y seleccionar **GitHub Actions** como origen. La URL aparecerá en la ejecución del workflow `Publicar Unity WebGL`.
9. Verificar Chrome, Edge y Firefox en el equipo de presentación, incluida la activación de audio tras interacción del usuario.

Los dos intentos batch del 15 de agosto de 2026 no llegaron a compilar porque Unity perdió la conexión con `Unity Licensing Client`. El generador quedó integrado y pasó la comprobación de compilación de C#; el siguiente intento debe hacerse con Unity Hub autenticado y la licencia activa.

La publicación no necesita una licencia de Unity: reutiliza el contenido existente de `WebGLBuild/`. La licencia solo es necesaria para regenerar ese build desde el proyecto de Unity.
