# Revisión científica de perfiles gallina–alimento

Fecha de corte: 2026-08-15  
Versión de contenido revisada: 0.3.0  
Estado editorial: borrador documentado (`pending`), pendiente de aprobación académica.

## Alcance y criterio

Esta revisión separa los resultados medidos de los valores tabulados o predichos. No se convierten tasas de digestión en porcentajes de digestibilidad ni se interpreta la digestibilidad media de aminoácidos como digestibilidad de proteína cruda. Los valores nulos son intencionales cuando no se encontró un resultado directamente comparable y trazable.

## Evidencia integrada

| Perfil | Evidencia cuantitativa integrada | Condiciones y límites principales |
|---|---|---|
| Maíz | 78,0 % de digestibilidad ileal aparente media de 15 aminoácidos; AMEn 15,1 MJ/kg MS | Pollos de engorde de 5 semanas, dieta sin fitasa para aminoácidos; AMEn tabulada por Feedipedia. |
| Trigo | 77,7 % de digestibilidad ileal aparente media de 15 aminoácidos; AMEn 13,8 MJ/kg MS | Mismo ensayo de aminoácidos; la tasa de digestión de almidón publicada (0,117 min⁻¹) se conserva como respuesta fisiológica, no como porcentaje. |
| Sorgo | 74,7 % de digestibilidad ileal aparente media de 15 aminoácidos; AMEn 15,4 MJ/kg MS | Mismo ensayo de aminoácidos; tasa de digestión de almidón 0,075 min⁻¹ consignada sólo como contexto. |
| Cebada | AMEn 11,3 MJ/kg MS | Tasa de digestión de almidón 0,104 min⁻¹ consignada como contexto; falta coeficiente ileal comparable. |
| Avena | AMEn 11,9 MJ/kg MS | Valor energético tabulado; faltan coeficientes ileales comparables. |
| Harina de soya | 82,2 % de digestibilidad ileal aparente media de 15 aminoácidos; AMEn 10,7 MJ/kg MS | Harina de soya del ensayo y tipo 48 para el valor energético tabulado. |
| Harina de canola | 78,7 % de digestibilidad ileal aparente media de 15 aminoácidos; AMEn 6,8 MJ/kg MS | Harina de canola del ensayo; energía tabulada dependiente de variedad y procesamiento. |
| Harina de girasol | 76,7 % de digestibilidad ileal aparente media de 15 aminoácidos | Falta un valor energético suficientemente comparable para la ficha actual. |
| Aceite de soya | 82,0 % de digestibilidad ileal aparente de grasa; AMEn 36,92 MJ/kg | Pollos de 2 semanas y dieta con 50 g/kg de aceite para grasa; energía procedente de guía de alimentación, no del mismo ensayo. |
| Harina de alfalfa | AMEn 3,8 MJ/kg MS | Valor tabulado en gallináceas; no se extrapolaron coeficientes ileales de otros ingredientes. |

## Fuentes primarias principales

- Ravindran et al. (1999), *Poultry Science* 78(5):699–706. DOI: 10.1093/ps/78.5.699.
- Selle et al. (2021), *Animal Nutrition* 7(2):450–459. DOI: 10.1016/j.aninu.2020.12.006.
- Tancharoenrat et al. (2014), *Poultry Science* 93(2):371–379. DOI: 10.3382/ps.2013-03344.
- Feedipedia, fichas tabuladas de ingredientes y valores nutritivos para aves.
- Hy-Line International, guía de manejo/alimentación empleada para el valor energético del aceite.

Las citas completas y sus enlaces se encuentran en `references/references.json` y se muestran dentro de la pantalla **Proyecto y bibliografía** de la aplicación.

## Rutas generales de absorción

- Los carbohidratos disponibles se representan como monosacáridos absorbidos principalmente en intestino delgado y conducidos por circulación portal hacia el hígado. La contribución segmentaria no se cuantifica en la animación.
- Los productos de la proteína se representan como aminoácidos y productos peptídicos procesados por el enterocito, con transporte portal y destinos generales de síntesis o catabolismo.
- Los lípidos aviares se representan con absorción intestinal y transporte sanguíneo portal mediante portomicrones. No se reutilizó el esquema linfático típico de mamíferos.
- Las fracciones fermentables se conectan con los ciegos y con la producción variable de ácidos grasos de cadena corta; la visualización no implica que toda la fibra fermente.

Fuentes añadidas para estas rutas: Shibata et al. (2023), Lerner (1984), Fraser et al. (1986), Oketch et al. (2023) y Józefiak et al. (2004). Las rutas son explicaciones fisiológicas generales, no balances cuantitativos ni predicciones de deposición tisular.

## Coordinación nerviosa y endocrina

La simulación representa cualitativamente CCK hacia páncreas y vía biliar, gastrina en la regulación gástrica y el reflejo de distensión del buche. La ficha conserva origen, receptor, órgano objetivo, respuesta y referencia de cada señal. La animación indica dirección funcional, pero no representa concentración hormonal, latencia, intensidad ni una relación dosis-respuesta.

## Vacíos antes de aprobar

- Completar digestibilidad ileal comparable para cebada, avena y alfalfa.
- Obtener digestibilidad de materia seca y grasa bajo condiciones explícitas para los ingredientes que aún tienen valores nulos.
- Incorporar porcentajes de digestibilidad de almidón sólo si el método y el punto de muestreo son comparables; las constantes cinéticas actuales no son porcentajes.
- Confirmar variedad, origen, procesamiento y composición de cada lote representado por el modelo educativo.
- Someter las diez fichas a revisión por un especialista en nutrición aviar antes de cambiar `scientificStatus` a `reviewed` o `approved`.
