# Simulador digestivo modular

Base técnica para una aplicación educativa Unity/WebGL sobre fisiología digestiva de animales domésticos. El primer módulo es la gallina, pero especies, órganos, alimentos y perfiles especie–alimento se cargan desde datos y no desde condicionales por nombre.

## Estado

Este incremento contiene:

- contratos JSON versionados;
- runtime C# desacoplado para especies, órganos, alimentos y perfiles;
- grafo digestivo con órganos accesorios y ramificaciones;
- narración WebGL sincronizada por órgano o etapa, con 15 audios iniciales en español;
- catálogos iniciales para gallina y diez alimentos;
- escena MVP navegable con anatomía, simulación y comparador;
- modelo esquemático reproducible en Blender y dos GLB cargados mediante glTFast;
- marcadores explícitos para contenido científico todavía no validado;
- documentación de Blender, paquetes, WebGL y GitHub Pages.

El modelo actual sirve para validar el pipeline técnico y todavía requiere revisión anatómica. Las narraciones actuales son una primera versión generada y los valores científicos aún requieren revisión humana y bibliografía trazable.

## Apertura en Unity

1. Abra `UnityProject/` con Unity `6000.5.7f1`.
2. Conserve `Assets/StreamingAssets/DigestiveSimulator/` sin cambiar su estructura.
3. Abra `Assets/Scenes/00_MVP.unity` y permita que Unity restaure los paquetes.

Consulte [Documentation/ARCHITECTURE.md](Documentation/ARCHITECTURE.md) para el diseño y el orden de implementación.

El avance, los pendientes y los insumos requeridos están centralizados en [PROJECT_STATUS.md](PROJECT_STATUS.md).

## Publicación web

El repositorio incluye un workflow de GitHub Pages. Al subir la rama `main`, prepara `WebGLBuild/` para hosting estático y publica el simulador. Consulte [Documentation/WEBGL_DEPLOYMENT.md](Documentation/WEBGL_DEPLOYMENT.md).

Simulador público: <https://jonathnadelgadoch.github.io/simulador-digestivo-gallina/>
