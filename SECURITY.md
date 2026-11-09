# Seguridad y privacidad

SCST es una aplicación local para Windows. La configuración inicial escucha exclusivamente en loopback. Este repositorio distribuye código; no debe incluir cuentas, expedientes, libros administrativos, respaldos, claves de recuperación ni archivos de una instalación operativa.

## Reportar un problema

Cuando el repositorio esté en GitHub y el mantenedor haya habilitado informes privados, use **Security → Report a vulnerability**. Si esa opción no está disponible, solicite al mantenedor un canal privado antes de enviar detalles sensibles. No publique credenciales, expedientes, capturas con datos personales o respaldos en issues, discusiones ni pull requests. No se define un correo de seguridad porque todavía no existe un canal confirmado para el proyecto.

Indique versión o commit, entorno y pasos mínimos de reproducción con datos ficticios. No incluya el valor de un secreto. Si una credencial real apareció en Git o fue compartida, debe rotarse o revocarse en su proveedor; borrarla del archivo actual no elimina la exposición anterior.

## Configuración y datos

Consulte `.env.example` como referencia. ASP.NET Core lee variables del proceso; no carga `.env` automáticamente. No se necesita token externo ni contraseña de servidor de base de datos. Las contraseñas de SCST se crean mediante el asistente y las claves de recuperación se generan localmente. Nunca se fijan en código, scripts, variables de ejemplo o GitHub Actions.

Instale los datos en una carpeta privada fuera de `wwwroot` y de los binarios. El instalador configura permisos Windows; al ejecutar manualmente con una ubicación personalizada, verifique que solo la cuenta autorizada tenga acceso. SQLite no cifra la base operativa. Los respaldos sí usan cifrado autenticado y una clave protegida con DPAPI, con recuperación externa independiente. No comparta esa clave junto con el respaldo.

Los logs técnicos registran metadatos de eventos y tipos de excepción, sin serializar estado arbitrario, consultas, cuerpos de solicitud ni excepciones completas. La auditoría administrativa contiene identificadores operativos y debe tratarse como dato privado. No comparta ninguno de estos directorios como material público de diagnóstico.

## Revisión previa a publicación

Ejecute `scripts/audit-public.ps1` y revise además los hallazgos manualmente. Un escáner no puede reconocer todos los datos personales o secretos. Revise el historial completo, la configuración remota y el contenido de cualquier archivo adjunto por separado.

La distribución pública parte de un único commit inicial, sin historial de desarrollo anterior. No incluye directorios de personal, expedientes, credenciales ni datos operativos. `scripts/export-public-source.ps1` permite preparar una copia de los fuentes actuales sin `.git` para revisiones independientes. El script no hace commit, push ni publicación.

## Soporte

El stack solicitado es .NET 9. Su [política de soporte](https://dotnet.microsoft.com/en-us/platform/support/policy) fija el fin de soporte el 10 de noviembre de 2026. No se declara esta versión apta para producción ni se cambia el framework automáticamente. La continuidad de soporte y la migración deben resolverse antes de una adopción productiva.
