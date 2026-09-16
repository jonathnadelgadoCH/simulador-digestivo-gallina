# Estrategia Blender → GLB

- Un archivo exterior y un archivo de aparato digestivo por especie.
- Órganos como objetos separados, nombrados con el mismo `id` de `organ.json`.
- Escala en metros, transformaciones aplicadas, pivotes revisados y normales hacia afuera.
- Materiales simples, texturas comprimibles y geometría optimizada para WebGL.
- Exportación glTF 2.0 binaria (`.glb`), sin scripts ni extensiones no aprobadas.
- El importador relaciona objetos y órganos mediante IDs; no mediante nombres visibles traducidos.

Antes de publicar: validar escala, orientación, jerarquía, conteo de polígonos, materiales, animaciones y carga en el navegador objetivo.

## Generación anatómica procedural

`Blender/generate_chicken_mvp.py` crea de forma reproducible la fuente Blender y las dos exportaciones GLB. El buche, la molleja y los lóbulos hepáticos se deforman vértice por vértice con BMesh para obtener siluetas asimétricas y compactas. El duodeno, yeyuno e íleon se construyen con curvas Bézier y tangentes explícitas para conservar continuidad visual.

Los órganos redondeados de alta resolución usan hasta 64 segmentos × 48 anillos. No se agrega un modificador de subdivisión adicional: esa malla base ya entrega una superficie suave y evita cuadruplicar innecesariamente el peso del build WebGL.

Conteo validado el 20 de agosto de 2026:

- Exterior: 7.100 triángulos.
- Aparato digestivo: 73.248 triángulos.
- Total: 80.348 triángulos, por debajo del límite automático de 120.000.

## Comandos de regeneración y control

Desde la raíz del proyecto, con Blender 5.2 instalado en la ruta predeterminada:

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background --python Blender\generate_chicken_mvp.py
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background --python Blender\validate_chicken_mvp.py
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background Blender\chicken_digestive_mvp.blend --python Blender\render_preview.py
```

El primer comando actualiza el `.blend`, `model.glb` y `digestive_system.glb`; el segundo comprueba nombres, transformaciones, materiales, envolvente corporal y presupuesto geométrico; el tercero genera `Blender/chicken_digestive_preview.png` para revisión visual.
