# Prototipo interactivo MVP

La vista inicial se construye completamente desde los catálogos y `digestive_graph.json`.

## Pantallas

- `Inicio`: acceso a los módulos principales.
- `Anatomía`: exploración, selección de órganos y vistas exterior/interna.
- `Simulación`: recorrido por etapas y partícula educativa.
- `Comparar`: comparación de dos ingredientes para la especie activa.
- `Proyecto`: estado bibliográfico, arquitectura y declaración provisional de IA.

El comparador distingue un valor pendiente de un valor igual a cero. También muestra el estado científico, método, procesamiento y referencias del perfil especie–alimento.

Durante la simulación, pequeños marcadores orbitan la partícula del alimento y usan una leyenda consistente: amarillo para almidón/energía, azul para proteínas/aminoácidos, naranja para lípidos y verde para fibra o carbohidratos estructurales. Son identificadores cualitativos; la ficha lateral muestra por separado los coeficientes cuantitativos disponibles.

La partícula principal cambia cualitativamente durante el recorrido: puede conservar volumen durante el almacenamiento, comprimirse durante la trituración y reducirse a medida que avanza la digestión y absorción. La etiqueta **Proceso** identifica la operación declarada por cada nodo del grafo; el tamaño mostrado es didáctico y no una medición experimental.

En los segmentos donde una ruta corresponde a los nutrientes destacados del alimento, una línea y un marcador animado conectan el sitio de absorción con el hígado. La ficha **Rutas de absorción activas** explica la forma absorbida, el transporte y los destinos generales. Para lípidos se representa la ruta portal aviar mediante portomicrones, no una ruta linfática de mamífero.

La ficha **Coordinación nerviosa y endocrina** muestra el control nervioso de la etapa y las señales estructuradas. Las trayectorias rosas representan hormonas, las cian reflejos y las violetas otras señales reguladoras. Los circuitos locales regresan al mismo órgano; las señales interorgánicas conectan el origen de la etapa con su órgano objetivo.

El botón **Qué cambia con este alimento** despliega motilidad, secreciones, respuesta neuroendocrina, notas metabólicas, condiciones del estudio y referencias del perfil seleccionado. Molleja, duodeno e íleon muestran mensajes específicos del ingrediente; las demás etapas presentan la función general documentada del órgano.

## Controles

- `A−` y `A+`: ajustar el texto entre 90 % y 150 %. El nivel queda guardado para las siguientes ejecuciones.
- Clic izquierdo sobre una estructura: seleccionarla y abrir su ficha.
- Botón izquierdo o derecho y arrastre sobre el área 3D: girar la figura.
- Botón central o `Shift` + arrastre: desplazar la figura.
- Rueda del ratón: zoom.
- `Exterior`, `Transparente` y `Solo aparato digestivo`: cambiar la visibilidad.
- `Reproducir`, `Pausar`, `Reiniciar` y `Siguiente`: controlar la secuencia.

## Alcance visual

Las primitivas son un recurso de integración temporal. Comprueban carga, selección, navegación, grafo, perfiles y simulación, pero no son modelos anatómicos ni representan escala, morfología o relaciones reales. Se reemplazarán por GLB validados conservando los mismos IDs.

Al abrir el proyecto, `MvpSceneInstaller` crea `Assets/Scenes/00_MVP.unity` y la incorpora al build si todavía no existe. También puede ejecutarse manualmente desde `Digestive Simulator > Crear o reparar escena MVP`.
