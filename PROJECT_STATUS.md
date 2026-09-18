# Estado del proyecto

Actualizado: 17 de septiembre de 2026

## Resumen ejecutivo

El proyecto ya posee una base Unity funcional, modular y preparada para WebGL. Puede cargar una especie, diez alimentos y sus perfiles, construir una anatomía esquemática desde un grafo, cargar los modelos GLB y ejecutar una simulación educativa por etapas.

Todavía no está listo para la entrega académica final porque el modelo 3D actual es esquemático y requiere validación anatómica. Los diez perfiles gallina–alimento ya cuentan con una primera versión documentada, pero necesitan aprobación académica; también faltan las grabaciones y completar la publicación WebGL.

| Elemento | Estado actual |
|---|---:|
| Escenas Unity | 1 |
| Scripts propios | 27 |
| Especies cargadas | 1 — Gallina |
| Órganos con ficha estructural | 19 |
| Alimentos | 10 |
| Perfiles gallina–alimento | 10 |
| Modelos Blender/GLB | 3 — 1 fuente y 2 exportaciones |
| Archivos de narración | 15 MP3 integrados; pendientes de aprobación auditiva |
| Referencias normalizadas | 24 |
| Órganos con primer borrador citado | 19 de 19 |
| Ingredientes con composición citada | 10 de 10 |
| JSON con contenido pendiente | 40 |

## Terminado

### Base del proyecto

- [x] Proyecto compatible con Unity `6000.5.7f1`.
- [x] Universal Render Pipeline `17.5.0` configurado.
- [x] Escena `00_MVP.unity` creada e incluida en Build Settings.
- [x] `.gitignore` apropiado para Unity.
- [x] Estructura de documentación, esquemas, plantillas y herramientas.

### Arquitectura modular

- [x] `SpeciesDefinition`.
- [x] `OrganDefinition`.
- [x] `FoodDefinition`.
- [x] `SpeciesFoodProfile`.
- [x] `DigestionGraph` y nodos accesorios.
- [x] Bases de datos runtime para especies y alimentos.
- [x] Cargadores desde `StreamingAssets`, compatibles con WebGL.
- [x] Validación de IDs, versiones, órganos declarados y referencias mínimas.
- [x] Separación entre composición del alimento y digestibilidad por especie.
- [x] Motor de simulación sin condicionales rígidos por especie, órgano o alimento.

### Módulo de gallina

- [x] Definición de gallina.
- [x] Grafo digestivo principal.
- [x] Diecinueve fichas estructurales de órganos.
- [x] Representación correcta de órganos accesorios dentro del grafo.
- [x] Recorrido funcional de trece etapas para el bolo.

### Catálogo de alimentos

- [x] Maíz.
- [x] Trigo.
- [x] Sorgo.
- [x] Harina de soya.
- [x] Cebada.
- [x] Avena.
- [x] Harina de canola.
- [x] Harina de girasol.
- [x] Aceite de soya.
- [x] Harina de alfalfa.
- [x] Perfil gallina–alimento creado para cada ingrediente.

### Prototipo interactivo

- [x] Menú principal.
- [x] Pantalla de anatomía.
- [x] Pantalla de simulación.
- [x] Comparador de dos alimentos.
- [x] Pantalla de bibliografía, proyecto y uso de IA.
- [x] Selección de órganos.
- [x] Vistas exterior, transparente y aparato digestivo.
- [x] Cámara orbital y zoom.
- [x] Desplazamiento del encuadre con botón central o `Shift + botón derecho`, y control para recentrar.
- [x] Encuadre automático general y enfoque al seleccionar un órgano.
- [x] Partícula educativa de alimento.
- [x] Reproducir, pausar, reiniciar y avanzar etapas.
- [x] Visualización explícita de datos pendientes.

### Narración

- [x] Sistema de carga WAV, MP3 y OGG.
- [x] Carga mediante `UnityWebRequest` para WebGL.
- [x] Pausa, reanudación, silencio y detención.
- [x] Narración predeterminada por órgano o específica por perfil.
- [x] Sincronización para esperar el final del audio antes de avanzar.

### Verificación técnica

- [x] Compilación independiente del runtime y scripts de Editor.
- [x] Cero errores y advertencias en la última comprobación.
- [x] Validación de 44 archivos JSON.
- [x] Prueba del recorrido y la máquina de estados.
- [x] URP asignado en Graphics Settings.
- [x] Los GLB contienen 19 objetos de órgano con IDs coincidentes.
- [x] Presupuesto geométrico validado: 7.100 triángulos exteriores y 73.248 digestivos.
- [x] Resolución digestiva elevada hasta 64 segmentos × 48 anillos en órganos redondeados, sin subdivisión redundante; curvas con resolución/bisel 4.
- [x] Validación automática de que los órganos del torso permanezcan en la envolvente corporal compacta.

## Parcialmente terminado

### Contenido científico

- [~] Existen todos los campos y fichas necesarias.
- [~] Los diecinueve órganos tienen un primer borrador con fuentes; falta la revisión académica.
- [~] Los diez ingredientes tienen composición documentada; falta revisar los tipos exactos de producto y lotes.
- [~] Los diez perfiles tienen un primer borrador trazable: seis incluyen digestibilidad ileal media de aminoácidos, uno digestibilidad de grasa y nueve AMEn; permanecen nulos los campos sin evidencia comparable.
- [x] La biblioteca científica contiene veinticuatro referencias normalizadas en `references.json`.
- [x] El validador comprueba que las referencias usadas por alimentos, órganos, señales y perfiles existan en la biblioteca.
- [ ] Ninguna ficha debe pasar a `approved` hasta completar su revisión y referencias.

### Simulación

- [~] El recorrido y los controles funcionan con geometría provisional.
- [~] Los alimentos usan colores diferentes.
- [~] Las diferencias fisiológicas, nutricionales y temporales entre alimentos tienen un primer borrador citado y visible; falta aprobación académica.
- [x] Marcadores, leyenda y coeficientes disponibles visualizan los nutrientes destacados.
- [x] Se integraron capas visuales de absorción, destinos metabólicos y regulación neuroendocrina.

### Comparador

- [~] La interfaz y carga de perfiles funcionan.
- [ ] Las celdas científicas permanecen pendientes porque no hay valores aprobados.
- [x] La interfaz muestra citas completas y condiciones experimentales detalladas.

### Audio

- [~] El sistema de reproducción está terminado.
- [x] Guion base en español preparado para introducción, trece etapas digestivas y cierre, con referencias trazables.
- [x] Generador reproducible basado en Windows SAPI y FFmpeg, con manifiesto editable y validación automática.
- [~] Quince WAV maestros y quince MP3 WebGL generados con Microsoft Sabina; pendientes de revisión auditiva y aprobación.
- [ ] Faltan créditos de voz y revisión de duración/volumen.

### Extensibilidad

- [~] El runtime está desacoplado y admite futuras especies.
- [ ] Solo se ha cargado la gallina.
- [ ] `.speciespack` y `.foodpack` están documentados, pero el importador, validador de seguridad y persistencia todavía no están implementados.

### Modelado 3D

- [~] Existe un modelo low-poly esquemático generado de forma reproducible en Blender y ajustado a una silueta de gallina ponedora.
- [~] Exterior y órganos separados ya se cargan desde GLB; falta validarlos visualmente dentro de Unity.
- [x] Los órganos del torso fueron reducidos y reubicados dentro del volumen corporal.
- [x] Buche, molleja y lóbulos hepáticos tienen formas anatómicas asimétricas generadas mediante BMesh, en lugar de elipsoides genéricos.
- [x] Duodeno, yeyuno e íleon usan tangentes Bézier explícitas para evitar quiebres y solapamientos bruscos.
- [x] La densidad digestiva aumentó hasta 73.248 triángulos, manteniendo el conjunto completo en 80.348 triángulos.
- [x] Escala, rotaciones, pivotes, normales, UV, materiales y nombres se validan después de reimportar los GLB.
- [ ] La forma, posición y proporción anatómica todavía requieren revisión especializada.

| Hito de modelado | Estado |
|---|---|
| Fuente Blender reproducible | Terminado |
| Silueta de gallina ponedora | MVP terminado; pendiente aprobación visual |
| Diecinueve órganos separados | Terminado técnicamente; pendiente revisión anatómica |
| Compactación dentro del cuerpo | Terminada y validada automáticamente |
| Exportación e integración GLB | Terminada |
| Calidad de malla para WebGL | Validada con 96.188 triángulos totales tras el primer bloque de remodelación |
| Transparencia y selección en Unity | Pendiente de prueba interactiva |

## Pendiente — prioridad alta

### Insumos académicos y científicos

- [x] Incorporar un primer conjunto de al menos cinco fuentes científicas válidas (7 incorporadas; pendiente validación del equipo).
- [x] Completar la primera versión de `references.json` con referencias normalizadas.
- [~] Completar y revisar las diecinueve fichas de órganos (19 con borrador citado; pendientes de aprobación).
- [~] Documentar composición de los diez alimentos (10 con borrador citado; pendientes de aprobación).
- [x] Documentar la primera versión de digestibilidad por alimento, nutriente, método y condiciones del estudio.
- [x] Completar los diez perfiles gallina–alimento como borradores documentados.
- [~] Revisión científica interna documentada en `Documentation/SCIENTIFIC_REVIEW.md`; pendiente evaluación por especialista.
- [ ] Aprobar contenido mediante el flujo `pending → reviewed → approved`.

### Modelado 3D

- [~] Refinar el modelo exterior esquemático de la gallina en Blender.
- [~] Refinar los diecinueve órganos digestivos ya separados.
- [x] Primer bloque anatómico: hígado bilobulado, molleja muscular con conexiones, buche dependiente y proventrículo fusiforme.
- [x] Corregir y validar escala, pivotes, normales, UV y nombres.
- [x] Mantener polígonos y materiales dentro del presupuesto técnico para WebGL.
- [x] Exportar `model.glb` y `digestive_system.glb`.
- [x] Integrar los GLB con fallback a primitivas esquemáticas.
- [x] Asociar cada objeto del modelo con su `organId`.
- [ ] Verificar transparencia y selección en el modelo definitivo.

### Contenido audiovisual

- [~] Guion científico base elaborado; pendiente revisión y aprobación por los estudiantes y el especialista.
- [ ] Validar el guion contra las referencias.
- [~] Primera narración sintetizada por órgano o etapa; pendiente decidir si se conserva o se reemplaza con grabación humana.
- [x] Exportar versiones MP3 optimizadas a 44,1 kHz mono y comprobar su compatibilidad con el navegador.
- [x] Registrar las trece rutas generales en `narrationFile`.
- [ ] Probar sincronización, volumen y reproducción en navegador.

### Experiencia educativa

- [x] Implementar tres mensajes diferentes para cada alimento en molleja, duodeno e íleon, con función general para las demás etapas.
- [x] Mostrar un panel desplegable con respuesta fisiológica, condiciones experimentales y referencias de cada perfil.
- [x] Representar cualitativamente almacenamiento, trituración, digestión, absorción y reducción de tamaño de la partícula a partir del proceso declarado en el grafo.
- [x] Incorporar marcadores 3D y leyenda por color para almidón/energía, proteínas/aminoácidos, lípidos, fibra y componentes específicos.
- [x] Mostrar en la etapa activa los coeficientes cuantitativos disponibles sin convertir los valores pendientes en cero.
- [x] Incorporar cuatro rutas de absorción específicas de gallina, trayectorias 3D hacia el hígado y destinos metabólicos generales con fuentes visibles.
- [x] Incorporar señales neuroendocrinas y reflejas con trayectorias 3D, órganos objetivo, respuestas y referencias visibles.
- [x] Implementar enfoque de cámara sobre el órgano seleccionado.
- [~] Tipografía ampliada y adaptable, paneles responsivos, comparador desplazable y controles persistentes `A− / A+`; falta una revisión visual en las resoluciones objetivo.

## Pendiente — publicación y entrega

- [~] Generador WebGL integrado en Unity; dos intentos batch quedaron bloqueados por pérdida de conexión con Unity Licensing Client antes de compilar.
- [x] Probar el build mediante servidor HTTP local.
- [ ] Medir carga, memoria y rendimiento.
- [ ] Probar Chrome, Edge y Firefox.
- [x] Probar audio después de interacción del usuario.
- [x] Crear y conectar el repositorio público `jonathnadelgadoCH/simulador-digestivo-gallina`.
- [x] Configurar GitHub Actions y preparación automática compatible con GitHub Pages.
- [x] Publicar en GitHub Pages.
- [x] Confirmar la URL pública `https://jonathnadelgadoch.github.io/simulador-digestivo-gallina/`.
- [ ] Completar README para usuarios y desarrolladores.
- [ ] Preparar reporte académico final.
- [ ] Añadir bibliografía en el formato requerido.
- [ ] Finalizar declaración de uso de IA con las herramientas realmente utilizadas.
- [ ] Ejecutar pruebas funcionales, visuales, científicas y WebGL.
- [ ] Ensayar el modo presentación en el equipo de la defensa.

## Pendiente — mejoras futuras

- [ ] Importación segura de `.speciespack`.
- [ ] Importación segura de `.foodpack`.
- [ ] Persistencia en IndexedDB para WebGL.
- [ ] Editor de especies.
- [ ] Editor de órganos.
- [ ] Editor de alimentos.
- [ ] Comparación entre especies.
- [ ] Añadir bovino, porcino, equino, perro, gato, ovino o caprino.
- [ ] Prueba formal de extensibilidad sin modificar scripts centrales.

## Insumos que debe proporcionar o validar el equipo

- Bibliografía científica seleccionada.
- Datos de anatomía, fisiología y regulación.
- Valores de composición y digestibilidad con condiciones del estudio.
- Guion científico de la presentación.
- Grabaciones de voz.
- Aprobación visual y anatómica del modelo Blender generado.
- Nombres y roles de los integrantes.
- Formato bibliográfico requerido por el curso.
- Fecha de entrega y requisitos exactos del reporte.
- Texto final de la declaración de uso de IA.

## Próximo hito recomendado

Validar y completar una primera sección vertical con calidad de entrega:

1. abrir el MVP en Unity y validar visualmente el modelo GLB exterior y digestivo;
2. revisar académicamente las diecinueve fichas de órganos y sus referencias;
3. completar los perfiles gallina–alimento con estudios de digestibilidad y condiciones experimentales;
4. grabar cinco fragmentos de audio;
5. ejecutar la simulación completa en WebGL;
6. usar esa sección como patrón para los demás órganos y alimentos.
