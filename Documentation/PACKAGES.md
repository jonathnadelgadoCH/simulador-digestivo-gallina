# Paquetes importables

## `.speciespack`

ZIP con extensión propia. Debe incluir `species.json`, al menos un modelo GLB, `digestive_graph.json` y un `organ.json` por órgano declarado. Se rechazan rutas absolutas, `..`, ejecutables, bibliotecas y scripts.

## `.foodpack`

ZIP con extensión propia. Debe incluir `food.json`; `model.glb`, icono y audio son opcionales. Se aplican los mismos límites de ruta, tamaño, extensión y versión.

En WebGL, la importación se realiza desde un selector de archivo del navegador, se valida en memoria y se persiste en IndexedDB. El contenido publicado de fábrica se carga desde catálogos; no se enumeran carpetas.

