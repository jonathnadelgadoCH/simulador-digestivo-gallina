# Arquitectura modular

## Principio central

El runtime combina cuatro entidades independientes:

1. `SpeciesDefinition`: identidad, modelos y órganos declarados de una especie.
2. `OrganDefinition`: anatomía, fisiología, regulación y referencias de un órgano.
3. `FoodDefinition`: identidad y composición documentada de un ingrediente.
4. `SpeciesFoodProfile`: respuesta y digestibilidad de un alimento en una especie y bajo condiciones de estudio concretas.

`SimulationEngine` recibe esas entidades junto con un `DigestionGraph`. No toma decisiones usando nombres como `chicken`, `gizzard` o `corn`. Cada nodo declara además un `process` (por ejemplo `storage`, `mechanical_digestion` o `absorption`) que conduce la presentación visual sin acoplarla a una especie concreta.

`SpeciesDefinition.absorptionRoutes` contiene las rutas fisiológicas propias de cada especie. Cada ruta declara claves de nutrientes, sitios anatómicos, forma absorbida, transporte, órgano objetivo, destinos, color y referencias. La presentación filtra esas rutas contra `SpeciesFoodProfile.simulation.highlightedNutrients`, por lo que no asume que todos los alimentos activan todas las rutas.

## Flujo de carga

```text
species_catalog.json ──> SpeciesLoader ──> RuntimeSpeciesDatabase
food_catalog.json ─────> FoodLoader ─────> RuntimeFoodDatabase
species + food ────────> SpeciesFoodProfileLoader
species graph + profile + food ──────────> SimulationEngine
```

Los catálogos son obligatorios porque WebGL no puede enumerar directorios de `StreamingAssets` de forma portable.

## Estructura

```text
UnityProject/Assets/
├── Scripts/DigestiveSimulator/
│   ├── Data/
│   └── Runtime/
└── StreamingAssets/DigestiveSimulator/
    ├── catalogs/
    ├── species/chicken/
    ├── foods/<food-id>/
    └── profiles/chicken/
```

## Política científica

- Un valor desconocido se representa con `null`, arreglo vacío o texto `[DATO PENDIENTE DE FUENTE CIENTÍFICA]`.
- `scientificStatus` usa `pending`, `reviewed` o `approved`.
- Un registro no puede considerarse aprobado sin referencias.
- Composición y digestibilidad mantienen referencias separadas.
- El guion oral científico no forma parte de este repositorio hasta que lo elaboren y validen los estudiantes.

## Próximos incrementos

1. Validador de esquemas y paquetes en Editor/CI.
2. Escena vertical mínima: selección → anatomía → simulación.
3. Integración GLB y selección de órganos.
4. UI, narración sincronizada y comparador.
5. Build WebGL y publicación automatizada.
