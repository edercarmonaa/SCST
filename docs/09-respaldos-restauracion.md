# Respaldos y restauración

Los respaldos automáticos se generan al iniciar si no existe uno exitoso del día. La retención elimina exclusivamente automáticos que excedan 30; no afecta manuales. No hay respaldo al cerrar. Administración muestra la fecha del último respaldo registrado y los archivos disponibles. El administrador puede descargar una copia para USB.

El snapshot usa `SqliteConnection.BackupDatabase`, por lo que no copia inseguramente el archivo activo ni omite transacciones en WAL. Se valida `PRAGMA integrity_check` y `foreign_key_check`, se cifra con AesGcm y se comprueba el descifrado antes de escribir el archivo definitivo. Los temporales se eliminan en finally. Una falla no se registra como respaldo exitoso.

La clave aleatoria de 32 bytes se protege con DPAPI CurrentUser en Keys/backup.key. La representación Base64 es el material de recuperación externo. No se almacena en el respaldo ni en el repositorio. Conservar una copia del respaldo en el mismo disco no reemplaza una copia externa. Una cuenta Windows distinta necesita la clave externa.

## Recuperación

1. Conserve el archivo .scst y la clave externa. Compruebe que está operando sobre el equipo y directorio correctos.
2. Ejecute restore.ps1 con la ruta del respaldo y escriba RESTAURAR para confirmar sustitución.
3. El script detiene el servidor y el proceso auxiliar adquiere el mismo bloqueo exclusivo que usa Kestrel.
4. Introduzca la clave externa o deje vacío para usar DPAPI local. Autentíquese como administrador SCST. En un equipo sin base existente se valida una cuenta administradora del propio respaldo.
5. Se descifra en Temp y se comprueba formato, autenticidad, integridad y configuración completa.
6. Si existe base actual se crea un respaldo previo consistente. Se cierran pools, se verifica el checkpoint WAL y se reemplaza atómicamente la base en el mismo volumen, conservando una copia de rollback hasta validar el resultado.
7. Si falla la comprobación posterior se repone la copia anterior. La clave externa se vuelve a proteger con DPAPI del equipo receptor. Inicie SCST y revise datos.

El bloqueo impide restaurar mientras una instancia SCST usa esos datos. Una etiqueta GCM alterada o una clave equivocada provoca rechazo antes de sustituir archivos. Se comprueba integridad en cada inicio, incluido después de cierres anormales; no se restaura una copia anterior sin intervención del administrador.

## Fallos

Si no hay espacio o permisos para respaldar, corrija la condición y reintente. Si falta la clave local, utilice la externa y conserve los archivos originales. Si faltan ambas, el cifrado no permite recuperar el respaldo. Si la base actual está corrupta y no puede producirse el respaldo previo, preserve el directorio completo con el servidor detenido para diagnóstico; no elimine archivos WAL o SHM de forma improvisada.

Las pruebas automatizadas verifican cifrado, rechazo de alteraciones, snapshot de base real, restauración a la misma ruta y recuperación a un directorio nuevo con material externo. Todavía debe ensayarse la recuperación operativa en otro equipo físico y la interfaz de consola con credenciales de la institución.
