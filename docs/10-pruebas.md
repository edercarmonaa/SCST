# Pruebas y verificaciones

## Automatización

SCST.Tests utiliza xUnit, SQLite real en memoria y bases temporales en archivos. No se sustituyen servicios por simulaciones. Las pruebas comprueban creación y campos obligatorios, mayúsculas con acentos, ceros del folio, unicidad con papelera, cambios de folio/ejercicio preservando relaciones, concurrencia, seguimientos, reingresos, validadores inactivos, oficios duplicados y asociaciones, permisos, bloqueo de login, cambio de contraseña y último administrador.

El semáforo se prueba en 0, 15, 16, 30, 31 y 900 días y con estados excluidos. Excel comprueba duplicados, ceros, catálogo desconocido e importación parcial. Los reportes comprueban seis hojas y conteo. PDF genera un documento multipágina que se reabre. AES-GCM comprueba descifrado y manipulación. Las pruebas de archivo verifican integridad y restauración de SQLite y recuperación con clave externa.

La prueba pública de lectura construye un libro XLSX con tres filas ficticias, valida los registros y comprueba que los bytes del libro no cambian. No depende de un archivo administrativo privado ni de variables locales de referencia.

## Prueba HTTP

`scripts/smoke.ps1` exige servidor con base nueva aislada. Usa cookies reales y antifalsificación para generar clave, completar asistente, iniciar sesión, recorrer 13 pantallas, crear expediente y seguimiento, exportar listado/estadística/carátula y crear cuenta Consulta. También comprueba cambio obligatorio de contraseña, denegación de cinco páginas y rechazo de un POST de escritura. Utiliza únicamente datos sintéticos y mantiene las contraseñas temporales en memoria, sin escribirlas en archivos. No use este script sobre una instalación institucional configurada.

La ejecución del 6 de noviembre de 2026 completó el flujo HTTP. El primer ensayo detectó que la prueba esperaba 403 en una aplicación Cookie Authentication que redirige a la página Denied; se corrigió la comprobación y el siguiente ensayo pasó. Los controles de permisos del servidor permanecen activos.

## Entorno y comandos

Se usó Windows x64 con SDK 9.0.320 y runtime 9.0.22. `dotnet build SCST.sln --no-restore` terminó con cero advertencias y cero errores. Se ejecutó `dotnet test SCST.sln --no-restore`; el detalle definitivo del número de pruebas está en decisiones y pendientes. Se ejecutó `dotnet list SCST.sln package --vulnerable --include-transitive`, sin avisos después de actualizar dependencias. La restauración inicial requirió acceso permitido a NuGet; los recursos frontend están presentes localmente.

DPAPI no funcionó dentro del proceso restringido del entorno de pruebas. El servidor se ejecutó después bajo la cuenta Windows con acceso a su perfil para comprobar el mecanismo real. No se deshabilitó el cifrado para hacer pasar la prueba.

## Verificaciones pendientes

Falta revisión visual de escritorio y móvil, teclado/WCAG, renderizado comparativo Word/PDF y uso con textos extensos. La herramienta de navegador informó que el host no estaba disponible y el control de Windows se detuvo por no poder verificar la URL; no se continuó con acciones UI. Falta instalación sin SDK en equipo limpio, ensayo completo del lanzador gráfico y actualización firmada con rollback. Estas condiciones impiden declarar la aceptación total de la solicitud.

La publicación autocontenida se arrancó con DOTNET_ROOT apuntando a una ruta inexistente y ejecutó su propio runtime. La prueba HTTP volvió a completarse sobre el ejecutable publicado, incluidos XLSX y PDF. El instalador se ejecutó con InstallPath, SCST_DATA y ShortcutDirectory dentro de .local: copió binarios, estableció permisos y creó un acceso directo de prueba. No modificó el escritorio ni el menú Inicio del usuario. Falta repetirlo en un equipo limpio sin SDK y ensayar el lanzador gráfico. Una prueba unitaria o un archivo de salida válido no certifican por sí solos diseño visual, soporte del sistema operativo ni aprobación institucional.
