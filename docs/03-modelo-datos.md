# Modelo de datos

SQLite almacena fechas de negocio `DateOnly`, marcas técnicas UTC `DateTime` y claves internas enteras. Los folios oficiales se mantienen como cadenas. Las entidades con papelera heredan Id, Deleted, CreatedUtc, ModifiedUtc y Version.

| Tabla | Propósito y restricciones |
|---|---|
| Tramites | Folio único global, ejercicio de recepción, solicitante dividido, referencias a tipo/nivel/estatus/medio/validador y campos originales |
| Catalogs | Kind y Name únicos, Active, y Pending para clasificar estatus; cinco tipos de catálogo |
| FollowUps | Expediente, fecha, actividad, observaciones, responsable de catálogo y usuario de captura |
| Reentries | Expediente, fecha, motivo y observaciones |
| Oficios | Number y Year únicos, fecha opcional y observaciones |
| TramiteOficio | Clave compuesta TramiteId/OficioId, sin vínculos repetidos |
| Users | Usuario normalizado único, hash, rol, activo, bloqueo, sello de sesión, contraseña temporal y columnas |
| Settings | Registro único Id=1 con institución, ejercicio, configuración completa y último respaldo |
| Audits | Usuario, instante UTC, acción, entidad, ID y folio cuando aplica |

Las claves foráneas usan Restrict. La aplicación no expone borrado físico. Los filtros globales excluyen expedientes, oficios, actividades y vínculos cuyos principales estén eliminados. Papelera utiliza consultas explícitas sin esos filtros. Restaurar un expediente recupera la visibilidad de sus actividades no eliminadas individualmente y sus asociaciones conservadas. Una actividad eliminada de forma individual requiere su propia restauración.

El índice único de folio no es filtrado, por lo que incluye eliminados. La comparación de unicidad es ordinal de SQLite: se conserva exactamente el texto oficial. La búsqueda de folios usa `upper` registrado por conexión con `ToUpperInvariant` de .NET, lo que permite comparar letras Unicode y acentos sin distinguir mayúsculas. Los nombres se almacenan normalizados en mayúsculas.

Los índices de ejercicio/recepción/papelera y las claves únicas cubren las búsquedas administrativas iniciales. La cadena de conexión activa foreign keys y timeout de 30 segundos. EF configura WAL durante la creación. Los respaldos usan SQLite Backup API, compatible con WAL.

El campo OriginalName conserva el nombre recibido del Excel, sin crear un campo permanente de origen de importación. No contiene un historial de cambios. Los campos opcionales desconocidos permanecen nulos o vacíos; no se inventan fechas de oficio al importar.
