# Universal Render Pipeline

El proyecto usa URP `17.5.0`, la versión incluida con Unity `6000.5`.

`UrpPipelineInstaller` crea y asigna automáticamente:

- `Assets/Settings/Rendering/DigestiveSimulatorRenderer.asset`;
- `Assets/Settings/Rendering/DigestiveSimulatorURP.asset`.

Configuración inicial para WebGL:

- HDR desactivado;
- textura de profundidad desactivada;
- textura opaca desactivada;
- MSAA 2x;
- escala de renderizado 1.0;
- distancia de sombras de 20 unidades.

La configuración puede regenerarse desde `Digestive Simulator > Configurar Universal Render Pipeline`.
