# Sistema de gestión de solicitudes LC

## Documento de planteamiento, arquitectura y fases de implementación

**Versión:** 1.0  
**Fecha:** 17 de septiembre de 2026  
**Estado:** Propuesta para validación antes del desarrollo

---

## 1. Propósito

Desarrollar una aplicación de escritorio para Windows, construida en Python, que reciba solicitudes de compra en PDF, extraiga y valide su información, las clasifique por área y carácter, organice los archivos, permita revisión humana, mantenga un registro estructurado en JSON y genere un dashboard en Excel.

La primera versión usará archivos JSON para reducir la complejidad de instalación y evitar conexiones a servidores. La arquitectura separará la lógica de negocio del almacenamiento para permitir una migración posterior a SQL Server o Azure SQL sin rehacer el procesamiento de PDF, la interfaz ni las reglas del sistema.

La solución incluirá autenticación local mediante contraseñas protegidas con Argon2id, una interfaz minimalista y una integración opcional con Outlook mediante Microsoft Graph para recibir documentos y responder correos sin abrir el cliente de Outlook.

## 2. Alcance funcional

La aplicación deberá:

1. Crear automáticamente la carpeta `solicitudesLC` dentro del perfil del usuario.
2. Crear y administrar las subcarpetas de entrada, pendientes, procesados y rechazados.
3. Leer documentos PDF digitales y, cuando sea necesario, documentos escaneados mediante OCR.
4. Extraer los campos definidos en el formato de requisición proporcionado.
5. Identificar el área responsable de cada solicitud.
6. Registrar el carácter de la solicitud: Emergencia, Urgente, Operativa / Recurrente, Programada o Preventiva.
7. Validar campos obligatorios y detectar documentos duplicados.
8. Enviar a revisión los documentos incompletos, ambiguos o de baja confianza.
9. Permitir que un usuario autorizado corrija, apruebe, rechace o vuelva a procesar una solicitud.
10. Conservar el PDF original y registrar todos los movimientos relevantes.
11. Guardar los datos operativos en archivos JSON protegidos contra escrituras incompletas.
12. Generar un libro Excel con datos tabulados, pendientes y dashboard.
13. Autenticar usuarios locales sin almacenar contraseñas en texto legible.
14. Permitir, como módulo opcional, recibir y responder mensajes de Outlook desde la aplicación.
15. Distribuirse como instalador MSI para Windows.

## 3. Alcance inicial y límites

La primera versión estará pensada para una instalación local y un volumen moderado de solicitudes. El almacenamiento JSON no se utilizará como si fuera una base de datos multiusuario en red.

Si varias computadoras necesitan modificar simultáneamente la misma información, la migración a SQL Server o Azure SQL deberá adelantarse. Una carpeta compartida con un único JSON no ofrece transacciones ni control de concurrencia equivalente a una base de datos.

El sistema no eliminará automáticamente documentos originales. Los movimientos se harán entre carpetas controladas y quedarán registrados en el historial.

## 4. Estructura de carpetas

Al iniciar, la aplicación resolverá la carpeta del usuario mediante `Path.home()` y verificará la siguiente estructura:

```text
%USERPROFILE%\solicitudesLC\
├── Bandeja de entrada\
├── Pendientes\
├── Procesados\
│   ├── Mantenimiento\
│   ├── Produccion\
│   ├── Calidad\
│   ├── Administracion\
│   └── Otras areas\
├── Rechazados\
├── Data\
│   ├── solicitudes.json
│   ├── catalogos.json
│   ├── usuarios.json
│   ├── configuracion.json
│   ├── correo.json
│   └── historial.jsonl
├── Exportaciones\
├── Backups\
├── Logs\
└── Temporales\
```

Los documentos procesados podrán organizarse por área, año y mes:

```text
Procesados\Mantenimiento\2026\09\solicitud_000125.pdf
```

El carácter de la solicitud se almacenará como dato y filtro, no como parte obligatoria de la ruta. Esto evita mover físicamente un PDF cuando un usuario corrige su carácter.

## 5. Flujo general

```text
PDF o correo recibido
        ↓
Registro y cálculo de hash
        ↓
Detección de duplicados
        ↓
Extracción directa de texto u OCR
        ↓
Identificación de campos, área y carácter
        ↓
Validaciones y nivel de confianza
        ↓
┌─────────────────┬──────────────────┬─────────────────┐
│ Datos válidos   │ Revisión humana  │ Archivo inválido│
│ Procesados      │ Pendientes       │ Rechazados      │
└─────────────────┴──────────────────┴─────────────────┘
        ↓
Actualización segura del JSON
        ↓
Actualización de Excel, historial y notificaciones
```

## 6. Arquitectura propuesta

La aplicación se dividirá en componentes independientes:

```text
Interfaz PySide6
        ↓
Servicios de aplicación
        ↓
Reglas de negocio y modelos
        ↓
Repositorios abstractos
        ↓
┌────────────────────┬──────────────────────────────┐
│ Repositorio JSON   │ Repositorio SQL futuro      │
│ Primera versión    │ SQL Server o Azure SQL       │
└────────────────────┴──────────────────────────────┘

Servicios externos:
- Procesamiento PDF y OCR
- Generación de Excel
- Microsoft Graph para correo
- Administrador de credenciales de Windows
```

### 6.1 Módulos sugeridos

```text
src/
├── application/
│   ├── auth_service.py
│   ├── solicitud_service.py
│   ├── procesamiento_service.py
│   ├── exportacion_service.py
│   └── correo_service.py
├── domain/
│   ├── models.py
│   ├── enums.py
│   ├── validation.py
│   └── rules.py
├── infrastructure/
│   ├── json_repository.py
│   ├── sql_repository.py
│   ├── pdf_reader.py
│   ├── ocr_reader.py
│   ├── excel_exporter.py
│   ├── graph_mail_client.py
│   ├── credential_store.py
│   └── file_manager.py
├── presentation/
│   ├── login_window.py
│   ├── main_window.py
│   ├── review_window.py
│   └── settings_window.py
└── main.py
```

La lógica no deberá leer o escribir directamente `solicitudes.json`. Toda operación se realizará mediante una interfaz de repositorio.

```python
class SolicitudRepository:
    def guardar(self, solicitud): ...
    def obtener(self, solicitud_id): ...
    def listar(self, filtros=None): ...
    def actualizar(self, solicitud): ...
    def existe_hash(self, archivo_hash): ...
```

La primera implementación será `JsonSolicitudRepository`. Posteriormente se podrá incorporar `SqlServerSolicitudRepository` y seleccionar el proveedor desde configuración.

## 7. Modelo de almacenamiento JSON

### 7.1 Archivo principal

`solicitudes.json` contendrá una versión de esquema y una colección de solicitudes:

```json
{
  "schema_version": "1.0",
  "actualizado_en": "2026-09-17T15:30:00-06:00",
  "solicitudes": [
    {
      "id": "SOL-2026-000001",
      "numero_solicitud": "REQ-00125",
      "fecha_solicitud": "2026-09-17",
      "area_id": "MANTENIMIENTO",
      "caracter_id": "EMERGENCIA",
      "solicitante": "Juan Perez",
      "centro_costo": "CC-100",
      "estado": "PROCESADO",
      "observaciones": "",
      "archivo": {
        "nombre_original": "solicitud_125.pdf",
        "ruta_relativa": "Procesados/Mantenimiento/2026/09/solicitud_125.pdf",
        "hash_sha256": "valor-del-hash",
        "tipo_pdf": "digital",
        "correo_origen_id": null
      },
      "extraccion": {
        "metodo": "texto",
        "confianza": 0.96,
        "requiere_revision": false
      },
      "clasificacion_caracter": {
        "origen": "extraido_documento",
        "confianza": 0.94,
        "requiere_confirmacion": false,
        "justificacion": "El documento indica paro inmediato de producción."
      },
      "detalle": [
        {
          "id": "DET-000001",
          "descripcion": "Rodamiento industrial",
          "cantidad": 4,
          "unidad": "Unidad",
          "proveedor": "",
          "precio_estimado": null
        }
      ],
      "creado_en": "2026-09-17T15:28:00-06:00",
      "actualizado_en": "2026-09-17T15:30:00-06:00"
    }
  ]
}
```

Los identificadores serán estables y no dependerán de la posición del registro dentro del archivo.

### 7.2 Catálogos

`catalogos.json` almacenará áreas, estados, caracteres, unidades y otras listas configurables. Esto permitirá agregar valores sin modificar el código.

```json
{
  "schema_version": "1.0",
  "areas": [
    {"id": "MANTENIMIENTO", "nombre": "Mantenimiento", "activa": true},
    {"id": "PRODUCCION", "nombre": "Producción", "activa": true}
  ],
  "estados": [
    {"id": "PENDIENTE_REVISION", "nombre": "Pendiente de revisión"},
    {"id": "PROCESADO", "nombre": "Procesado"},
    {"id": "RECHAZADO", "nombre": "Rechazado"}
  ]
}
```

### 7.3 Escritura segura

Para reducir el riesgo de pérdida o corrupción:

1. Se bloqueará temporalmente el recurso durante la escritura.
2. Los cambios se escribirán primero en un archivo temporal.
3. Se validará el JSON resultante contra su esquema.
4. El archivo anterior se reemplazará mediante una operación atómica.
5. Se generará una copia de seguridad antes del reemplazo.
6. Se conservará una cantidad configurable de respaldos.
7. El historial se escribirá en `historial.jsonl`, agregando un evento por línea.

## 8. Catálogo de carácter

El carácter es una dimensión obligatoria y distinta del estado del flujo. Indica la naturaleza y prioridad operativa de la solicitud.

| Código | Carácter | Definición |
|---|---|---|
| `EMERGENCIA` | Emergencia | Su ausencia detiene o pone en riesgo inmediato la producción, seguridad o calidad. |
| `URGENTE` | Urgente | Debe adquirirse rápidamente para evitar afectaciones próximas. |
| `OPERATIVA_RECURRENTE` | Operativa / Recurrente | Compra normal necesaria para mantener la operación. |
| `PROGRAMADA` | Programada | Se conoce con anticipación y puede programarse. |
| `PREVENTIVA` | Preventiva | Busca evitar fallas, paros o problemas futuros. |

Cada carácter se almacenará con un nivel de prioridad configurable:

```json
{
  "id": "EMERGENCIA",
  "nombre": "Emergencia",
  "descripcion": "Su ausencia detiene o pone en riesgo inmediato la producción, seguridad o calidad.",
  "nivel_prioridad": 1,
  "activo": true
}
```

### 8.1 Reglas del carácter

1. Cada solicitud tendrá un carácter principal.
2. Si el PDF lo indica claramente, se registrará como extraído del documento.
3. Si no está marcado, el sistema podrá sugerirlo mediante reglas configurables.
4. Una coincidencia de palabras no será suficiente para declarar automáticamente una emergencia.
5. Dos caracteres marcados, un valor ausente o una confianza insuficiente enviarán la solicitud a revisión.
6. El usuario podrá corregir la selección e indicar un motivo.
7. Los cambios quedarán en el historial con valor anterior, valor nuevo, usuario y fecha.
8. Emergencias y urgencias se mostrarán primero en las colas, sin alterar el estado real del expediente.

Ejemplo válido:

```text
Área: Mantenimiento
Carácter: Emergencia
Estado: Pendiente de revisión
```

Los tiempos máximos de atención por carácter no se fijarán hasta que la organización apruebe los acuerdos de servicio. Quedarán como parámetros configurables.

## 9. Procesamiento de PDF

### 9.1 Tipos admitidos

- PDF digital con texto extraíble.
- PDF escaneado mediante OCR.
- PDF mixto con páginas digitales y escaneadas.

### 9.2 Pasos

1. Validar extensión, firma del archivo, tamaño y capacidad de lectura.
2. Calcular SHA-256 para detectar duplicados.
3. Intentar extracción directa.
4. Evaluar la cantidad y calidad del texto extraído.
5. Ejecutar OCR cuando sea necesario.
6. Normalizar espacios, fechas, números y nombres de campos.
7. Extraer cabecera y detalle.
8. Clasificar área y carácter.
9. Calcular confianza por campo y confianza global.
10. Aplicar validaciones de negocio.
11. Enviar a Procesados, Pendientes o Rechazados.

### 9.3 Revisión humana

La pantalla de revisión mostrará el PDF y los campos extraídos en paralelo. Los campos dudosos estarán marcados de forma discreta y accesible. El usuario podrá corregirlos, confirmar el carácter, agregar observaciones y finalizar el expediente.

## 10. Autenticación local con Argon2id

### 10.1 Principio

Las contraseñas no se cifrarán de manera reversible ni se guardarán en texto claro. Se almacenará únicamente el resultado de Argon2id, incluyendo los parámetros y la sal necesarios para verificar la contraseña.

Se recomienda `argon2-cffi` y su API de alto nivel. Los parámetros de memoria, tiempo y paralelismo deberán configurarse y probarse en los equipos destino para mantener un equilibrio entre seguridad y tiempo de inicio de sesión. El sistema podrá volver a calcular el hash cuando cambien los parámetros recomendados.

Ejemplo conceptual de usuario:

```json
{
  "id": "USR-0001",
  "usuario": "administrador",
  "nombre": "Administrador local",
  "password_hash": "$argon2id$v=19$...",
  "rol": "ADMINISTRADOR",
  "activo": true,
  "intentos_fallidos": 0,
  "bloqueado_hasta": null,
  "ultimo_acceso": null,
  "creado_en": "2026-09-17T15:00:00-06:00"
}
```

### 10.2 Controles

- Creación segura del primer administrador durante la configuración inicial.
- Política configurable de longitud de contraseña.
- Bloqueo temporal después de varios intentos fallidos.
- Retraso progresivo para limitar intentos automatizados.
- Cierre de sesión manual y por inactividad.
- Cambio de contraseña con verificación de la contraseña actual.
- Restablecimiento administrativo registrado en auditoría.
- Roles iniciales: Administrador, Revisor y Consulta.
- Protección de `usuarios.json` mediante permisos del sistema operativo.
- Uso del Administrador de credenciales de Windows o DPAPI para secretos de la aplicación.
- Exclusión de contraseñas, tokens y contenido sensible de los logs.

El archivo `.env` se usará para opciones de desarrollo o configuración no sensible. No contendrá contraseñas de usuarios, tokens de Outlook ni secretos productivos en texto claro.

## 11. Pantalla de inicio minimalista

La aplicación abrirá con una ventana simple y centrada:

```text
┌─────────────────────────────────────┐
│                                     │
│          Solicitudes LC             │
│     Gestión y seguimiento           │
│                                     │
│  Usuario                            │
│  [____________________________]     │
│                                     │
│  Contraseña                         │
│  [___________________________] [◉]  │
│                                     │
│          [ Iniciar sesión ]         │
│                                     │
│  Estado local: disponible           │
│                                     │
└─────────────────────────────────────┘
```

### 11.1 Criterios visuales

- Fondo gris cálido, blanco suave o tono neutro.
- Panel central con borde ligero y sombra discreta.
- Acento azul grisáceo o verde apagado.
- Sin fondos negros, rojos saturados ni combinaciones agresivas.
- Tipografía del sistema, preferiblemente Segoe UI.
- Espaciado amplio y pocos elementos visibles.
- Mensajes breves y claros.
- Indicadores de error en tono moderado, acompañados de texto y no solo de color.
- Contraste suficiente para legibilidad y accesibilidad, aunque la paleta sea suave.
- Navegación completa con teclado.
- El botón de mostrar contraseña solo actuará mientras se mantenga o active explícitamente.

Después del acceso se mostrará un panel principal con Procesados, Pendientes, Rechazados, Emergencias abiertas y accesos a revisión, exportación y correo.

## 12. Integración con Outlook

### 12.1 Viabilidad

Sí es posible leer y responder correos desde la aplicación Python sin abrir Outlook. La opción recomendada es Microsoft Graph, no la automatización directa de la ventana de Outlook.

Microsoft Graph permite:

- Consultar mensajes y archivos adjuntos.
- Descargar PDFs recibidos.
- Crear respuestas en borrador.
- Responder un mensaje existente.
- Enviar mensajes nuevos.
- Guardar los envíos en Elementos enviados.
- Trabajar aunque Outlook no esté instalado o abierto.

La cuenta deberá iniciar sesión en Microsoft al menos durante la configuración inicial y cumplir la autenticación multifactor y las políticas de la organización. La aplicación no solicitará ni almacenará la contraseña de Outlook.

### 12.2 Requisitos organizacionales

1. Registrar la aplicación en Microsoft Entra ID.
2. Obtener el identificador de aplicación y el identificador del tenant.
3. Configurar la aplicación como cliente de escritorio.
4. Solicitar únicamente permisos mínimos.
5. Obtener consentimiento del usuario o del administrador según la política de la empresa.
6. Guardar la caché de tokens mediante protección de Windows.
7. Definir qué buzón o buzones puede utilizar la aplicación.

Para leer y responder como el usuario conectado se emplearán permisos delegados. `Mail.Read` permite lectura y `Mail.Send` permite enviar o responder. Si se necesita modificar mensajes, categorías o borradores se evaluará `Mail.ReadWrite`. No se solicitarán permisos superiores si no son necesarios.

Una operación completamente desatendida, sin un usuario conectado, requiere permisos de aplicación y aprobación administrativa. Ese modo tiene mayor alcance y deberá limitarse a buzones autorizados por el equipo de Microsoft 365.

### 12.3 Modalidades de respuesta

Se proponen cuatro niveles:

1. **Solo recepción:** descarga el PDF y registra el correo, sin responder.
2. **Borrador automático:** prepara la respuesta y el usuario la revisa antes de enviarla.
3. **Confirmación rápida:** muestra el texto propuesto y envía después de una confirmación.
4. **Envío automático:** responde sin intervención únicamente cuando se cumple una regla aprobada.

La primera implementación debería usar borrador automático o confirmación rápida. El envío automático se habilitará después de validar plantillas, remitentes y reglas.

### 12.4 Ejemplos de respuestas

- Recepción: “La solicitud REQ-00125 fue recibida y está pendiente de revisión.”
- Procesada: “La solicitud REQ-00125 fue procesada correctamente.”
- Información faltante: enumerar solamente los campos requeridos.
- Rechazada: indicar el motivo autorizado y el canal para corregirla.
- Emergencia: confirmar recepción sin prometer tiempos no definidos.

Las plantillas serán configurables y no dependerán de texto incorporado directamente en el código.

### 12.5 Flujo del correo

```text
Correo nuevo en buzón o carpeta autorizada
        ↓
Validar remitente, asunto y adjuntos
        ↓
Registrar identificador del mensaje
        ↓
Descargar PDF a Bandeja de entrada
        ↓
Procesar solicitud
        ↓
Crear borrador o respuesta según la regla
        ↓
Registrar resultado y evitar respuestas duplicadas
```

### 12.6 Salvaguardas

- Lista de remitentes o dominios admitidos.
- Carpeta de correo específica para solicitudes LC.
- Detección de identificadores de mensaje ya procesados.
- Prevención de respuestas a mensajes automáticos, rebotes y fuera de oficina.
- Límite de una respuesta automática por mensaje y por solicitud.
- No responder cuando el archivo esté dañado o la identidad de la solicitud sea ambigua, salvo con una plantilla controlada.
- Registro del destinatario, asunto, plantilla, resultado y hora de envío.
- Cola de reintentos con límites; nunca reintentos infinitos.
- Botón para desactivar inmediatamente el envío automático.
- Separación entre el inicio de sesión local y la autorización de Microsoft 365.

## 13. Dashboard de Excel

La aplicación generará:

```text
Exportaciones\Dashboard_Solicitudes.xlsx
```

Hojas propuestas:

1. `Dashboard`: indicadores, filtros y gráficos.
2. `Solicitudes`: cabecera tabulada de cada expediente.
3. `Detalle`: artículos o servicios solicitados.
4. `Pendientes`: registros que requieren intervención.
5. `Catalogos`: valores necesarios para consulta o validación.

### 13.1 Indicadores

- Total de documentos recibidos.
- Procesados, pendientes y rechazados.
- Solicitudes por área.
- Solicitudes por carácter.
- Emergencias abiertas.
- Urgentes pendientes.
- Distribución del carácter por área.
- Documentos sin carácter confirmado.
- Tiempo promedio de procesamiento.
- Antigüedad de solicitudes pendientes.
- Tendencia mensual.
- Motivos frecuentes de rechazo.
- Correos recibidos, respondidos y con error.

Colores sugeridos para el carácter:

| Carácter | Color de apoyo |
|---|---|
| Emergencia | Rojo moderado |
| Urgente | Naranja suave |
| Operativa / Recurrente | Azul grisáceo |
| Programada | Verde suave |
| Preventiva | Turquesa suave |

Los colores serán apoyo visual y no reemplazarán las etiquetas de texto.

## 14. Configuración

Ejemplo conceptual:

```env
APP_ENV=production
STORAGE_PROVIDER=json
DATA_ROOT=%USERPROFILE%\solicitudesLC
LOG_LEVEL=INFO
EMAIL_ENABLED=false
EMAIL_MODE=draft
```

Los datos no sensibles podrán estar en `.env` o `configuracion.json`. Los identificadores públicos de Microsoft podrán estar en configuración. Tokens, secretos y credenciales se almacenarán mediante mecanismos protegidos de Windows.

Cuando se migre:

```env
STORAGE_PROVIDER=sqlserver
```

La cadena de conexión se obtendrá desde el Administrador de credenciales de Windows, una identidad administrada u otro mecanismo aprobado, no desde un archivo distribuido con el MSI.

## 15. Migración futura a SQL Server o Azure SQL

La estructura JSON se diseñará como representación de las tablas futuras:

| Origen JSON | Tabla futura |
|---|---|
| `solicitudes` | `Solicitudes` |
| `detalle` | `SolicitudDetalles` |
| `archivo` | `Archivos` |
| `areas` | `Areas` |
| `caracteres` | `Caracteres` |
| `estados` | `Estados` |
| `usuarios` | `Usuarios` |
| `historial.jsonl` | `HistorialSolicitudes` |
| registro de correo | `CorreosProcesados` |

Relaciones principales:

```text
Solicitudes.AreaId      → Areas.AreaId
Solicitudes.CaracterId  → Caracteres.CaracterId
Solicitudes.EstadoId    → Estados.EstadoId
SolicitudDetalles.SolicitudId → Solicitudes.SolicitudId
Archivos.SolicitudId    → Solicitudes.SolicitudId
```

Se desarrollará una utilidad de migración que:

1. Valide la versión de esquema.
2. Lea los JSON.
3. Cree o valide catálogos.
4. Inserte solicitudes y detalles mediante transacciones.
5. Verifique totales, identificadores y hashes.
6. Genere un informe de registros migrados y rechazados.
7. Mantenga el JSON original como respaldo de solo lectura.

## 16. Tecnologías propuestas

| Necesidad | Tecnología propuesta |
|---|---|
| Lenguaje | Python 3.12 o versión LTS compatible |
| Interfaz | PySide6 |
| Modelos y validación | Pydantic |
| Contraseñas | argon2-cffi con Argon2id |
| PDF digital | PyMuPDF o pdfplumber |
| OCR | OCRmyPDF y Tesseract |
| Imágenes | Pillow |
| Excel | openpyxl o generación controlada desde plantilla |
| Correo | Microsoft Graph con MSAL para Python |
| JSON | Biblioteca estándar, esquemas JSON y bloqueo de archivos |
| SQL futuro | SQLAlchemy y controlador oficial para SQL Server |
| Credenciales Windows | Credential Manager o DPAPI |
| Ejecutable | PyInstaller |
| Instalador | WiX Toolset |
| Pruebas | pytest |

La selección definitiva de librerías PDF se realizará después de evaluar documentos reales.

## 17. Fases de implementación

### Fase 0. Validación del planteamiento

- Confirmar alcance del MVP.
- Confirmar si el uso será individual o compartido.
- Confirmar áreas iniciales.
- Confirmar campos obligatorios.
- Confirmar si Outlook será parte del MVP o una segunda entrega.
- Confirmar reglas de respuesta automática.
- Confirmar roles y política de contraseñas.

**Salida:** especificación aprobada y decisiones registradas.

### Fase 1. Análisis del formato y muestras

- Revisar el formato Excel proporcionado.
- Mapear campos, tipos, listas y obligatoriedad.
- Reunir muestras de PDF digital y escaneado.
- Identificar variaciones del documento.
- Definir reglas iniciales de área y carácter.
- Establecer umbrales de confianza.

**Salida:** diccionario de datos, mapa del formulario y conjunto de pruebas.

### Fase 2. Base del proyecto

- Crear estructura del código.
- Crear configuración por entorno.
- Implementar creación de carpetas.
- Configurar logs sin datos sensibles.
- Definir modelos Pydantic.
- Definir contratos de repositorio.
- Crear esquemas JSON y datos iniciales de catálogo.

**Salida:** aplicación inicial capaz de arrancar y validar configuración.

### Fase 3. Autenticación y pantalla de inicio

- Crear asistente del primer administrador.
- Implementar Argon2id.
- Crear pantalla minimalista de acceso.
- Implementar roles, bloqueo e inactividad.
- Proteger secretos mediante Windows.
- Registrar accesos y cambios de contraseña.

**Salida:** acceso local seguro y perfiles de usuario.

### Fase 4. Repositorio JSON

- Implementar altas, consultas y actualizaciones.
- Incorporar bloqueo y escritura atómica.
- Implementar copias de seguridad.
- Crear historial JSON Lines.
- Detectar duplicados mediante hash.
- Preparar migraciones por `schema_version`.

**Salida:** almacenamiento local estable y recuperable.

### Fase 5. Lectura de PDF y OCR

- Implementar extracción directa.
- Incorporar OCR como respaldo.
- Normalizar datos.
- Extraer cabecera y líneas de detalle.
- Calcular confianza por campo.
- Guardar evidencia técnica de extracción sin duplicar datos sensibles.

**Salida:** prototipo medido con documentos reales.

### Fase 6. Clasificación y reglas de negocio

- Clasificar por área.
- Integrar el catálogo de carácter.
- Aplicar validaciones obligatorias.
- Crear reglas de pendientes y rechazo.
- Implementar prioridades y trazabilidad.
- Evitar clasificación automática insegura de emergencias.

**Salida:** flujo completo desde entrada hasta estado final.

### Fase 7. Interfaz operativa

- Crear panel principal.
- Crear listas de procesados, pendientes y rechazados.
- Mostrar PDF junto con campos extraídos.
- Permitir corrección y aprobación.
- Incorporar filtros, búsqueda y orden por prioridad.
- Crear configuración de áreas, caracteres y plantillas.

**Salida:** aplicación utilizable por revisores.

### Fase 8. Excel y dashboard

- Crear tablas de solicitudes y detalles.
- Crear indicadores y gráficos.
- Agregar filtros por fecha, área, carácter y estado.
- Validar actualización desde Python.
- Probar apertura y presentación en Microsoft Excel.

**Salida:** dashboard operativo y exportaciones consistentes.

### Fase 9. Integración con Outlook

- Registrar la aplicación en Microsoft Entra ID.
- Implementar autenticación con MSAL.
- Proteger la caché de tokens.
- Leer la carpeta autorizada.
- Descargar PDFs sin duplicarlos.
- Crear borradores y respuestas.
- Implementar plantillas y salvaguardas.
- Activar primero confirmación manual.
- Probar envío automático únicamente con reglas aprobadas.

**Salida:** recepción y respuesta desde la aplicación sin abrir Outlook.

### Fase 10. Empaquetado MSI

- Generar ejecutable con PyInstaller.
- Incluir recursos y dependencias necesarias.
- Evaluar la distribución de Tesseract y OCRmyPDF.
- Crear MSI con WiX Toolset.
- Crear accesos directos y registro de versión.
- Implementar desinstalación sin borrar los datos del usuario.
- Diseñar proceso de actualización.

**Salida:** instalador probado en una computadora limpia.

### Fase 11. Piloto y estabilización

- Ejecutar piloto con usuarios seleccionados.
- Medir precisión y tiempos.
- Revisar falsos positivos de carácter.
- Revisar pendientes y rechazos.
- Ajustar reglas, OCR y plantillas.
- Realizar respaldo y recuperación controlados.

**Salida:** versión candidata para producción.

### Fase 12. Migración a SQL, cuando sea necesaria

- Implementar el repositorio SQL.
- Crear tablas, índices y restricciones.
- Ejecutar la utilidad de migración.
- Conciliar conteos y hashes.
- Cambiar `STORAGE_PROVIDER`.
- Mantener temporalmente exportaciones y respaldo JSON.

**Salida:** operación sobre SQL Server o Azure SQL sin cambios en la interfaz ni en las reglas principales.

## 18. Pruebas

### 18.1 Pruebas unitarias

- Validación de modelos.
- Hash y detección de duplicados.
- Clasificación por área y carácter.
- Reglas de estado.
- Verificación Argon2id.
- Escritura y recuperación JSON.
- Generación de rutas.
- Selección de plantillas de correo.

### 18.2 Pruebas de integración

- PDF digital a JSON.
- PDF escaneado a JSON.
- JSON a Excel.
- Inicio de sesión y permisos por rol.
- Recepción de adjunto desde Outlook.
- Creación de borrador y respuesta.
- Respaldo y restauración.
- Migración de una versión de esquema.

### 18.3 Casos críticos

- Dos archivos iguales con nombres diferentes.
- PDF corrupto o protegido.
- Solicitud sin área.
- Solicitud sin carácter.
- Dos caracteres marcados.
- Emergencia sugerida con baja confianza.
- Corte durante escritura del JSON.
- Token de Microsoft vencido o revocado.
- Correo automático o respuesta fuera de oficina.
- Falta temporal de internet.
- Excel abierto durante una actualización.

## 19. Criterios de aceptación del MVP

El MVP se considerará funcional cuando:

1. Cree automáticamente la estructura de carpetas.
2. Permita crear un administrador e iniciar sesión mediante Argon2id.
3. Procese las variantes de PDF acordadas.
4. Extraiga los campos obligatorios con el nivel de precisión aprobado.
5. Clasifique área y carácter, enviando ambigüedades a revisión.
6. Detecte archivos duplicados.
7. Separe procesados, pendientes y rechazados.
8. Mantenga JSON válido, respaldos e historial.
9. Permita revisión y corrección manual con trazabilidad.
10. Genere un Excel legible con dashboard y datos conciliados.
11. Se instale y desinstale mediante MSI sin perder los datos del usuario.
12. Si correo forma parte del MVP, descargue PDFs y cree al menos respuestas en borrador sin requerir que Outlook esté abierto.

## 20. Decisiones pendientes

Antes de programar deben confirmarse:

- Cantidad estimada de solicitudes por día y por año.
- Uso en una o varias computadoras.
- Lista completa de áreas.
- Campos obligatorios del formato.
- Existencia de un número único de solicitud.
- Porcentaje mínimo de confianza para procesar automáticamente.
- Acuerdos de tiempo por carácter.
- Roles y usuarios iniciales.
- Tipo de cuenta Outlook: personal, Microsoft 365 empresarial o buzón compartido.
- Carpeta de Outlook que se vigilará.
- Remitentes autorizados.
- Casos que podrán responderse automáticamente.
- Si el administrador de Microsoft 365 autorizará Microsoft Graph.
- Política de conservación de PDFs, logs y respaldos.

## 21. Recomendación de entrega

El desarrollo debería iniciar sin envío automático de correos. La primera entrega puede recibir documentos, procesarlos y generar borradores de respuesta. Después de validar la precisión, las plantillas y los permisos de Microsoft 365, se puede habilitar la confirmación rápida y finalmente el envío automático para casos cerrados y deterministas.

La combinación de JSON versionado, repositorios independientes, identificadores estables y catálogos normalizados permitirá construir una primera versión simple y mantener una ruta clara hacia SQL Server o Azure SQL.

## 22. Referencias técnicas para la integración Microsoft

- Microsoft Graph `sendMail`: https://learn.microsoft.com/en-us/graph/api/user-sendmail?view=graph-rest-1.0
- Microsoft Graph `reply`: https://learn.microsoft.com/en-us/graph/api/message-reply?view=graph-rest-1.0
- Configuración de aplicaciones de escritorio: https://learn.microsoft.com/en-us/entra/identity-platform/scenario-desktop-app-configuration
- Adquisición de tokens con MSAL para Python: https://learn.microsoft.com/en-us/entra/msal/python/getting-started/acquiring-tokens
- Referencia de permisos Microsoft Graph: https://learn.microsoft.com/en-us/graph/permissions-reference
