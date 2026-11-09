# Arquitectura del SCST

Se implementa un monolito modular con exactamente cuatro proyectos. Web depende de Infrastructure, e Infrastructure depende de Core. Tests depende de Infrastructure para comprobar reglas y persistencia con SQLite real. Core no referencia EF ni ASP.NET. Las anotaciones de validación pertenecen al BCL.

`Database` es el DbContext y unidad transaccional. No se agrega Generic Repository, otra Unit of Work, MediatR ni CQRS. `TramiteService` coordina reglas, permisos y auditoría de expedientes y sus relaciones. `UserService` administra autenticación local y cuentas. `ExcelService` separa validación previa y confirmación. `Reports` produce archivos en memoria. `BackupService` realiza snapshots consistentes, cifrado e integridad. `DataPaths` separa almacenamiento de binarios. Razor Pages prepara datos y resultados HTTP, usando los servicios para las reglas principales.

Los cambios de expediente usan un token Guid de concurrencia y una transacción que incluye auditoría. Los oficios comprueban su versión. Importar utiliza una transacción por fila: los rechazos no invalidan las filas correctas ni dejan relaciones incompletas.

Los listados usan consultas paginadas y lecturas `AsNoTracking`. Los detalles incluyen relaciones explícitamente y `AsSplitQuery` para evitar explosión de combinaciones. Dashboard y estadísticas materializan los resultados del periodo para agruparlos; para bases de gran volumen deberá medirse y trasladarse la agregación a SQL si el perfil real lo exige.

Bootstrap y HTMX son locales. Razor es el renderizador principal; la búsqueda con retardo usa HTMX para reemplazar la tabla. El formulario GET permanece funcional sin JavaScript. El CSS define paleta y componentes responsive. La navegación y las validaciones no dependen de servicios externos.

El proceso mantiene un bloqueo exclusivo por directorio de datos. El lanzador solicita un cierre ordenado mediante un evento Windows asociado al hash de esa ruta. El auxiliar de mantenimiento es el mismo ejecutable en modo consola, antes de iniciar Kestrel. Así se conserva el requisito de cuatro proyectos y se evita restaurar sobre la base activa.

Las migraciones están versionadas en Infrastructure. Una base nueva se inicializa automáticamente; una base existente con migraciones pendientes exige mantenimiento explícito. No se aplican migraciones destructivas silenciosamente al iniciar una instalación operativa.
