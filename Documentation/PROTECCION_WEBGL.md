# Protección del código en WebGL

## Alcance real

Una aplicación WebGL se ejecuta en el equipo del visitante. Por ese motivo, el navegador debe descargar el HTML, JavaScript, WebAssembly, datos, modelos y audios necesarios. DevTools siempre podrá mostrar y guardar esos archivos: no existe una forma técnica de ocultarlos por completo.

La protección integrada reduce la información útil para reconstruir el proyecto, evita publicar accidentalmente fuentes o símbolos y limita lo que una página inyectada podría ejecutar. No sustituye una licencia ni convierte los recursos enviados al navegador en secretos.

## Controles integrados

| Control | Lugar | Resultado |
|---|---|---|
| Build no-development | `WebGlBuild.cs` | El build usa `BuildOptions.None`, sin depurador ni profiler. |
| Símbolos desactivados | `WebGlBuild.cs` | No se generan símbolos WebAssembly para relacionar funciones con el código original. |
| Trazas desactivadas | `WebGlBuild.cs` | La consola no revela pilas con nombres internos del proyecto. |
| Diagnósticos desactivados | `WebGlBuild.cs` | Se reduce la información técnica visible en producción. |
| Validación del build | `WebGlBuild.cs` | La compilación falla si encuentra fuentes, mapas, PDB o datos de depuración. |
| Validación del despliegue | `prepare-github-pages.mjs` | El sitio no se prepara si contiene `.cs`, `.py`, `.blend`, `.map`, símbolos u otros archivos de desarrollo. |
| Política CSP | HTML generado | Solo permite recursos propios y las capacidades imprescindibles de Unity WebGL. |
| Encabezados portables | `_headers` generado | Cloudflare Pages y Netlify pueden aplicar CSP, `nosniff`, privacidad de referencias y bloqueo de permisos. |

GitHub Pages no interpreta el archivo `_headers`. Allí la CSP se aplica mediante una etiqueta `<meta>` insertada en `index.html`; los demás encabezados quedan disponibles si el mismo artefacto se despliega en Cloudflare Pages o Netlify.

## Qué permanece visible

- El JavaScript del cargador y del motor de Unity.
- El binario WebAssembly compilado, aunque sea mucho menos legible que el código C#.
- Los modelos GLB, audios y catálogos JSON requeridos por la aplicación.
- Los textos científicos mostrados al usuario.
- Las solicitudes de red y la estructura general del sitio.

## Información que nunca debe ir en el cliente

Claves privadas, contraseñas, credenciales, algoritmos realmente confidenciales y datos restringidos deben mantenerse en un servidor. El WebGL solo debe recibir el resultado mínimo autorizado mediante una API. Ofuscar o renombrar archivos puede elevar el esfuerzo de inspección, pero no protege secretos.

## Comprobación antes de publicar

1. Generar el build desde `Digestive Simulator > Build WebGL` o mediante el comando automatizado del proyecto.
2. Ejecutar `node Tools/prepare-github-pages.mjs WebGLBuild _site`.
3. Confirmar que el proceso termina sin archivos prohibidos.
4. Probar el sitio preparado y verificar modelos, audio, giro y zoom.
5. Publicar únicamente `_site`, nunca la carpeta `UnityProject/Assets`.
