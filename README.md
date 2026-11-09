# Sistema de Control y Seguimiento de Trámites

SCST organiza expedientes administrativos que antes se controlaban en un libro de Excel. Permite registrar solicitudes, consultar pendientes, documentar actividades y reingresos, relacionar oficios y generar reportes desde una sola aplicación local.

## El problema

Una oficina que recibe trámites necesita saber qué se recibió, quién lo solicitó, qué documentación falta y qué acciones se han realizado. Cuando ese control depende de una hoja de cálculo, es fácil repetir folios, mezclar fechas de captura con fechas administrativas o perder la relación entre un oficio y los expedientes que atiende.

El proyecto parte de un control de trámites educativos con folios, niveles, validadores, incidencias y respuestas. Sin una herramienta integrada, el personal debe reconstruir el estado de cada expediente revisando filas, observaciones y documentos por separado. SCST busca mantener esas relaciones y reglas en un registro consultable.

## La solución

El personal registra un expediente con su folio oficial, solicitante, fecha de recepción, tipo y nivel educativo. Después agrega actividades, reingresos y oficios dentro del mismo expediente. La aplicación calcula el ejercicio desde la recepción, distingue estatus y validación documental y muestra los pendientes según días naturales transcurridos.

Los registros pueden consultarse con filtros, exportarse a Excel o utilizarse para generar una carátula PDF. La papelera conserva las relaciones de los elementos eliminados lógicamente. La administración local controla cuentas, catálogos, auditoría y respaldos cifrados.

## Funcionalidades principales

- Registro, búsqueda, edición y restauración de trámites con folio textual único, incluso en papelera.
- Solicitante dividido en nombres y apellidos, fechas administrativas independientes y asignación opcional de validador.
- Seguimientos con responsable, usuario de captura y cronología; reingresos dentro del expediente original.
- Oficios con número único por ejercicio y relaciones de muchos a muchos con los trámites.
- Listado paginado, filtros por ejercicio y campos administrativos, preferencias de columnas y acciones múltiples.
- Semáforo de pendientes: verde hasta 15 días, amarillo entre 16 y 30, rojo después de 30; los reingresos no reinician el plazo.
- Estadísticas por periodo, estatus, nivel y validador; exportación de listados y un libro estadístico de seis hojas.
- Importación XLSX con vista previa, revisión de nombres, validación, confirmación parcial y descarga de errores.
- Carátulas y resúmenes estadísticos PDF con previsualización, descarga y paginación.
- Autenticación local, roles, cambio de contraseña temporal, bloqueo por intentos fallidos y auditoría administrativa básica.
- Respaldos SQLite consistentes, cifrado AES-GCM, recuperación externa y mantenimiento con el servidor detenido.

## ¿Qué mejora este proyecto?

| Dificultad del control manual | Función que la aborda |
|---|---|
| Repetir o reutilizar un folio | Restricción única de base de datos que incluye papelera |
| Perder contexto al registrar un reingreso | Reingresos ligados al identificador del expediente original |
| Buscar qué expedientes atiende un oficio | Relaciones explícitas y consulta desde el oficio |
| Confundir estado, validación y expedición | Campos y reglas independientes |
| Preparar conteos y carátulas manualmente | Filtros, estadísticas y archivos generados bajo demanda |
| Perder información por eliminación accidental | Eliminación lógica y restauración de relaciones |
| Conservar una sola copia del libro | Respaldos cifrados y procedimiento de recuperación |

## ¿Para quién está pensado?

Para oficinas administrativas que necesitan un control local de trámites educativos en una computadora Windows. Los roles reales son:

- **Administrador:** usuarios, catálogos, configuración, auditoría, respaldos y operaciones del sistema.
- **Capturista:** captura y edición de expedientes, actividades, oficios, importación y papelera.
- **Consulta:** lectura y reportes, sin acceso a escritura ni papelera.

La distribución inicial no contiene cuentas, expedientes ni un directorio de personal. Los validadores se crean en cada instalación con autorización de su administrador.

## Tecnologías utilizadas

| Componente | Tecnología verificada |
|---|---|
| Aplicación | C#, ASP.NET Core 9, Razor Pages |
| Persistencia | Entity Framework Core SQLite 9.0.22 y SQLite local |
| Interfaz | Bootstrap 5.3.3, CSS, JavaScript y HTMX 1.9.12 locales |
| Excel | ClosedXML 0.105.0 |
| PDF | PDFsharp 6.2.0 y fuentes del sistema Windows |
| Seguridad | Cookie Authentication, Data Protection con DPAPI y AES-GCM del BCL |
| Pruebas | xUnit 2.9.3 y Microsoft.NET.Test.Sdk 17.12.0 |
| Operación | PowerShell y publicación autocontenida Windows x64 |

Las versiones directas están en los `.csproj` y las dependencias resueltas en `packages.lock.json`. No se utiliza SPA, CDN, servicio cloud ni contenedor como requisito operativo.

## Requisitos

Para desarrollar o ejecutar desde los fuentes:

- Windows x64 con una cuenta que tenga su perfil cargado; DPAPI y los PDF usan recursos de Windows.
- SDK .NET **9.0.320**, o un parche compatible de esa banda, según `global.json`.
- Git y PowerShell para las tareas de preparación.
- Acceso a NuGet durante la primera restauración, o una caché previamente preparada.

Para usar una publicación autocontenida no es necesario instalar el SDK ni el runtime .NET. La operación normal no necesita internet.

.NET 9 termina su soporte el **10 de noviembre de 2026**, según la [política oficial de Microsoft](https://dotnet.microsoft.com/en-us/platform/support/policy). El proyecto conserva el stack solicitado; la continuidad de soporte debe resolverse antes de una adopción productiva.

## Instalación

Obtenga los fuentes del repositorio público:

```powershell
git clone https://github.com/edercarmonaa/SCST.git
cd SCST
dotnet restore SCST.sln --locked-mode
dotnet build SCST.sln --no-restore
```

Para preparar un paquete local autocontenido desde los fuentes revisados:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\publish.ps1
```

El resultado regenerable queda en `artifacts/publish` y no se versiona. El script también prepara documentación y un manifiesto. No publica una release ni sube archivos a un servicio.

El instalador local es `scripts/setup.ps1`. Copia los binarios fuera del directorio de datos, prepara permisos de la cuenta Windows y crea accesos directos. Su uso y las opciones para ensayar en carpetas aisladas se describen en [instalación Windows](docs/08-instalacion-windows.md). La opción ExecutionPolicy de los ejemplos se limita a ese proceso; en una distribución institucional deben respetarse la firma y las políticas de ejecución autorizadas.

## Configuración

[.env.example](.env.example) documenta las variables utilizadas. **ASP.NET Core no carga `.env` automáticamente.** Establézcalas en el proceso PowerShell o en el entorno del lanzador. No se requiere API key ni contraseña de servidor SQL.

| Variable | Uso | Valor predeterminado |
|---|---|---|
| `SCST_DATA` | Carpeta privada local para base, claves, respaldo y temporales | `%LOCALAPPDATA%/SCSTData` cuando está vacía o ausente |
| `SCST_PORT` | Puerto de escucha, siempre en loopback | `5186` |
| `ASPNETCORE_ENVIRONMENT` | Entorno ASP.NET Core | `Production` si no se establece |
| `SCST_BACKUP` | Ruta que `restore.ps1` pasa al auxiliar de recuperación | Solo durante mantenimiento |

Ejemplo con una carpeta privada alternativa y el mismo puerto local:

```powershell
$env:SCST_DATA = Join-Path $env:LOCALAPPDATA 'SCSTData'
$env:SCST_PORT = '5186'
$env:ASPNETCORE_ENVIRONMENT = 'Production'
```

No coloque claves de recuperación ni contraseñas en estas variables de ejemplo. Se generan o capturan mediante el flujo local. La aplicación rechaza almacenar datos dentro de `wwwroot`. Para una ubicación personalizada, compruebe los permisos Windows y no use una carpeta pública, sincronizada o de red.

## Base de datos

El motor es SQLite mediante EF Core. No se necesita un servidor separado. La primera ejecución sobre una ubicación nueva aplica la migración inicial y carga catálogos administrativos generales. Los validadores se dejan vacíos; no se distribuyen datos de empleados o solicitantes.

El almacenamiento tiene esta estructura:

```text
SCSTData/
  Database/   Base SQLite
  Backups/    Copias cifradas
  Keys/       Material local protegido
  Logs/       Metadatos técnicos
  Exports/    Directorio reservado para exportaciones
  Temp/       Vistas previas y archivos temporales
```

Las migraciones están en `src/SCST.Infrastructure/Migrations`. Una instalación existente con migraciones pendientes necesita mantenimiento explícito; no se aplican cambios destructivos silenciosamente al arrancar. Nunca copie una base encima de otra activa.

## Ejecutar el proyecto

Desde la raíz de los fuentes:

```powershell
dotnet run --project src/SCST.Web --no-restore
```

Abra **http://127.0.0.1:5186**. Si cambió SCST_PORT, use ese puerto. Conserve la consola abierta y detenga el proceso con Ctrl+C.

Desde una publicación autocontenida, ejecute en su propia carpeta:

```powershell
cd artifacts/publish
.\SCST.Web.exe
```

El lanzador `scripts/run.ps1` permite abrir, reiniciar y detener una publicación local. La aplicación utiliza un bloqueo por directorio para evitar que dos instancias operen simultáneamente sobre los mismos datos.

## Uso

1. En el primer inicio complete institución, unidad, área y ejercicio; cree su cuenta administradora sin credenciales predeterminadas.
2. Genere la clave de recuperación, guárdela fuera del equipo y confirme su conservación. Si vuelve al asistente, introduzca la clave guardada; no necesita generar otra.
3. Cree los validadores y las cuentas necesarias. Las contraseñas temporales requieren cambio al ingresar.
4. Registre un trámite, mantenga vacíos los datos desconocidos y agregue actividades o reingresos al mismo expediente.
5. Registre oficios y vincule los trámites que correspondan. Consulte filtros, pendientes históricos y reportes.
6. Si importa un libro, revise la vista previa antes de confirmar. Los folios existentes se omiten sin sobrescritura y las filas inválidas se reportan.
7. Genere un respaldo, descargue una copia externa y ensaye su recuperación con datos de prueba.

Los libros y carátulas administrativos originales no forman parte del repositorio público. El formato aceptado y las reglas de revisión están en [importación Excel](docs/06-importacion-excel.md); el procedimiento de recuperación está en [respaldos y restauración](docs/09-respaldos-restauracion.md).

## Estructura del proyecto

```text
SCST.sln
src/
  SCST.Core/              Entidades y reglas independientes de infraestructura
  SCST.Infrastructure/    SQLite, migraciones, servicios, Excel, PDF y respaldos
  SCST.Web/               Razor Pages, autenticación y recursos locales
tests/SCST.Tests/         Pruebas de reglas, persistencia y seguridad
scripts/                 Desarrollo, mantenimiento y revisión pública
docs/                    Documentación técnica y operativa
```

## Capturas

No se incluyen capturas del sistema con expedientes, nombres, usuarios o claves. Pueden agregarse capturas revisadas de una instalación con datos completamente ficticios. Los recursos de identidad institucional existentes no representan una captura de pantalla ni una licencia general de reutilización.

## Seguridad

Las contraseñas se protegen con `PasswordHasher`, las sesiones usan cookies y sellos de seguridad, y los permisos de escritura se verifican en servidor. Los servicios no adjuntan relaciones recibidas desde formularios como si fueran entidades autorizadas. La base conserva restricciones e integridad referencial.

Los formularios usan antifalsificación y Razor codifica la salida. Data Protection persiste claves protegidas con DPAPI. Los respaldos usan cifrado autenticado AES-GCM y material externo de recuperación. La base operativa no está cifrada y depende de los permisos Windows. La modalidad inicial escucha solamente en loopback; no está preparada para exponerse públicamente en internet.

No suba `.env`, bases, respaldos, exportaciones, claves, capturas privadas ni credenciales a Git. Las exclusiones no eliminan archivos ya presentes en commits anteriores. Esta distribución comienza con un único commit inicial limpio; consulte [SECURITY.md](SECURITY.md) y las [comprobaciones de publicación](docs/13-preparacion-publica.md).

Para informar vulnerabilidades, use un canal privado del mantenedor o la opción de informes privados de GitHub cuando esté habilitada. No envíe secretos ni datos administrativos en un issue público.

## Pruebas

```powershell
dotnet restore SCST.sln --locked-mode
dotnet build SCST.sln --no-restore
dotnet test SCST.sln --no-build --no-restore
dotnet list SCST.sln package --vulnerable --include-transitive
```

La batería actual incluye 37 casos con bases aisladas y datos ficticios. Comprueba folios, papelera, relaciones, concurrencia, permisos, cuentas, semáforo, Excel, PDF, respaldo y restauración. Las pruebas de seguridad también comprueban el descarte de grafos no autorizados y que los logs no escriban contenidos sensibles arbitrarios. No se necesita un libro administrativo privado para ejecutarlas.

La prueba HTTP debe apuntar a una instalación **nueva y aislada**, nunca a datos operativos. Puede iniciar otro proceso con SCST_DATA diferente y SCST_PORT=5187, y después ejecutar:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\smoke.ps1 -BaseUrl http://127.0.0.1:5187
```

Revisión de los fuentes y del historial:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\audit-public.ps1
```

El escáner omite los valores encontrados y devuelve 2 cuando detecta elementos para revisión. Complementa la inspección manual; no garantiza ausencia de secretos. No hay linter independiente configurado: el build trata advertencias C# como errores y los scripts se pueden comprobar con el parser de PowerShell.

## Estado del proyecto

**Versión inicial funcional, en desarrollo activo.** Se han comprobado compilación, pruebas automatizadas, restauración bloqueada y flujos HTTP con datos ficticios. Esto no certifica todos los criterios de aceptación ni constituye una declaración de aptitud para producción.

## Limitaciones actuales

- Ejecución y recuperación ligadas a Windows, sus fuentes y su protección DPAPI.
- Revisión visual de accesibilidad y fidelidad de carátulas aún pendiente.
- Instalación limpia, actualización firmada y recuperación en otro equipo físico pendientes de ensayo completo.
- La división propuesta de nombres importados y el ejercicio asignado a oficios históricos requieren revisión humana.
- La lista local de contraseñas comunes es básica.
- Los recursos institucionales son de terceros y no se relicencian bajo la licencia MIT del código.
- El soporte próximo a finalizar de .NET 9 requiere una decisión antes de uso productivo.

## Próximas mejoras

Estas tareas están planeadas y no se presentan como funcionalidades verificadas: cotejar carátulas renderizadas, validar teclado y móvil, ensayar instalación y actualización en equipos limpios, ampliar la lista de contraseñas comunes y acordar una transición a una versión con soporte adecuado.

## Contribuciones

Cree un fork y una rama para su cambio. Mantenga el alcance concreto, use datos ficticios, ejecute build y pruebas y abra un pull request describiendo el problema, el cambio y su validación. No incluya datos operativos ni recursos sobre los que no tenga autorización. Los problemas de seguridad se reportan por el canal privado descrito en SECURITY.md.

## Licencia

El código y la documentación propios de SCST se distribuyen bajo la [licencia MIT](LICENSE). Las dependencias conservan sus licencias respectivas; consulte [componentes de terceros](docs/12-terceros.md). Los logotipos e identidades institucionales son recursos de terceros y no se relicencian ni se autoriza su uso de marca mediante la licencia del código.
