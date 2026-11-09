# Instalación y operación Windows

Objetivo: Windows x64, una sola computadora, navegador y ejecución sin internet. La publicación es autocontenida y no exige instalar el runtime .NET. La compilación necesita SDK .NET 9 y paquetes NuGet disponibles. Windows 10 debe tener un esquema de soporte vigente; el soporte del sistema operativo y de .NET se validará institucionalmente antes de producción. Esta implementación se comprobó en el Windows 11 del equipo de desarrollo, no en todas las ediciones Windows 10/11.

## Preparación

Ejecute `scripts/publish.ps1` desde el repositorio. Genera `artifacts/publish`, documentación y scripts en artifacts, más un manifiesto SHA-256. Si proporciona `-CertificateThumbprint`, firma el manifiesto con un certificado del almacén CurrentUser/My. Un certificado no suministrado no se inventa ni se almacena como secreto en el repositorio.

Ejecute `scripts/setup.ps1` para copiar binarios a `%LOCALAPPDATA%\Programs\SCST\app`, instalar scripts y crear accesos en escritorio e inicio. El directorio puede indicarse con InstallPath. Los datos se mantienen en `%LOCALAPPDATA%\SCSTData` o en SCST_DATA, fuera de los binarios. El script establece permisos exclusivos de la cuenta Windows que instala. No instala un servicio, no abre puertos ni modifica el firewall.

Los scripts del repositorio son UTF-8. Para PowerShell 5.1 se distribuyen con BOM para que los mensajes españoles se lean correctamente. Si la política local no permite scripts sin firma, firme la distribución institucional o ejecute explícitamente el proceso documentado en README. No se cambia la política de ejecución del equipo.

## Lanzador

El acceso directo abre una ventana con Abrir sistema, Reiniciar y Detener sistema. El servidor se inicia oculto; el navegador abre 127.0.0.1:5186. Antes de iniciar se detecta una instancia HTTP existente y el servidor mantiene además un bloqueo exclusivo del directorio de datos. Un evento Windows solicita el apagado ordenado; el script espera hasta liberar el bloqueo. Cerrar el lanzador no detiene el servidor.

Opciones de consola: `run.ps1 -Action Start`, `Stop` o `Restart`. El ejecutable debe trabajar desde la carpeta publicada para encontrar wwwroot y appsettings. No se deben usar dos directorios de datos diferentes en el mismo puerto al mismo tiempo.

## Configuración inicial

Defina institución, unidad, área y ejercicio. Genere la clave de recuperación, guárdela fuera del equipo y confirme. Establezca cuenta y frase de contraseña propia; no hay contraseña predeterminada. El asistente verifica la clave contra un respaldo antes de marcarse completo. Si se recargó el asistente después de generar la clave, introdúzcala en la sección de recuperación del propio asistente. No genere otra clave ni borre la existente sin evaluar los respaldos ya creados.

## Actualización local

`update.ps1 -PackageDirectory <carpeta> -TrustedSignerThumbprint <huella autorizada>` verifica firma CMS, hashes, rutas dentro del paquete y target net9.0. El paquete es la carpeta artifacts de una publicación firmada, que contiene publish y manifest.json/p7s. El certificado autorizado debe comprobarse por un canal institucional independiente.

El procedimiento detiene el servidor, exige respaldo con autenticación administrativa, conserva binarios anteriores, copia únicamente los archivos del manifiesto y ejecuta migraciones de forma explícita. Ante error de migración recupera los binarios anteriores y solicita restaurar el respaldo antes de iniciar. No realiza una restauración silenciosa. La autenticidad no se valida aceptando el certificado que venga dentro del mismo paquete sin contrastar su huella.

El instalador y la creación de un acceso directo se probaron con InstallPath, SCST_DATA y ShortcutDirectory dentro de .local. Esa opción permite ensayar sin modificar escritorio o menú Inicio. El proceso completo y la actualización firmada necesitan todavía un equipo de prueba limpio. No se ejecutó setup sobre el perfil operativo del usuario.
