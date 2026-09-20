# Modelo Blender del MVP

`generate_chicken_mvp.py` crea un modelo de integración low-poly, guarda `chicken_digestive_mvp.blend` y exporta los GLB utilizados por el módulo de gallina.

El modelo es esquemático y está marcado como `schematic_pending_validation`. Sirve para comprobar el pipeline Blender → GLB → Unity, los IDs de órganos, transparencia, selección y rendimiento. No debe presentarse como anatomía validada sin revisión y refinamiento por el equipo.

La silueta actual toma como referencia una gallina ponedora de cuerpo redondeado. El aparato digestivo fue compactado dentro de la envolvente corporal y `validate_chicken_mvp.py` comprueba automáticamente esa restricción para los órganos del torso.

Antes de exportar, el generador aplica escala y rotación, centra los pivotes, recalcula normales, activa suavizado y crea coordenadas UV cuando una malla no las posee. El validador vuelve a importar ambos GLB y revisa esas propiedades, los materiales, los IDs y el presupuesto de triángulos.

Los órganos utilizan una densidad superior a la envolvente exterior: las superficies redondeadas generales parten de 64 segmentos por 48 anillos y las curvas usan resolución 10 y bisel 6. El exterior mantiene su densidad low-poly para reservar el presupuesto de WebGL a las estructuras educativas seleccionables.

El primer bloque de remodelación sustituye las primitivas de buche y proventrículo por perfiles longitudinales cerrados, aumenta la definición muscular y los cuellos de la molleja, y genera lóbulos hepáticos aviares asimétricos con borde ventral, relieve visceral y escotadura craneal. El validador establece pisos individuales de detalle para impedir que estos cuatro órganos vuelvan a geometrías genéricas.

El segundo bloque define el duodeno como un asa continua descendente–ascendente que retorna hacia el yeyuno. Dentro del asa, el páncreas combina un núcleo curvo con cuatro lóbulos glandulares superpuestos, evitando la apariencia de tubo uniforme. Además de sus pisos de detalle, el validador comprueba que el centro del páncreas permanezca dentro de la envolvente del duodeno.

El tercer bloque convierte el yeyuno en un conjunto compacto de asas irregulares, afina progresivamente el íleon hacia la unión ileocecal y modela el divertículo de Meckel como una rama ciega en la transición yeyuno–íleon. Las curvas cierran ahora sus extremos y el validador comprueba contacto espacial entre duodeno, yeyuno, íleon, divertículo y ciegos.

Regeneración:

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background --python Blender\generate_chicken_mvp.py
```

La vista previa reproducible se genera con `Blender/render_preview.py` y se guarda como `Blender/chicken_digestive_preview.png`.
