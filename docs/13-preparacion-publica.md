# Comprobaciones de publicación

La distribución pública de SCST comienza con un único commit inicial limpio. El historial de desarrollo anterior no forma parte del repositorio público. El código y la documentación propios se distribuyen bajo la licencia MIT de la raíz; los recursos institucionales y las dependencias conservan sus derechos y licencias respectivos.

## Contenido distribuido

Se incluyen los cuatro proyectos, sus pruebas, recursos frontend locales, scripts, lockfiles de NuGet y documentación. No se incluyen datos operativos, directorios de personal, credenciales, material de recuperación, archivos administrativos, resultados de pruebas, ejecutables ni paquetes compilados.

Los validadores se configuran por instalación. Las pruebas construyen datos ficticios y un libro XLSX en memoria; no dependen de documentos privados.

## Verificación de fuentes e historial

Desde la raíz:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\audit-public.ps1
git status --short
git rev-list --count HEAD
```

La publicación inicial debe tener un solo commit, sin padres. El escáner omite los valores encontrados y devuelve 2 si detecta elementos que requieren revisión. La inspección automatizada complementa la revisión humana y no garantiza ausencia de todos los tipos posibles de secretos o datos personales.

Para revisar una copia independiente de los fuentes actuales sin metadatos Git:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\export-public-source.ps1 -Destination .local\revision-nueva
```

El script exige un destino nuevo y no hace commits, push ni publicación. Las carpetas locales privadas y los respaldos de desarrollo están excluidos por `.gitignore`; no deben adjuntarse a issues ni distribuirse por separado.

## Dependencias y pruebas

```powershell
dotnet restore SCST.sln --locked-mode
dotnet build SCST.sln --no-restore
dotnet test SCST.sln --no-build --no-restore
dotnet list SCST.sln package --vulnerable --include-transitive
```

Los lockfiles y el destino win-x64 están definidos para restaurar de manera reproducible. Las pruebas utilizan bases aisladas y verifican reglas administrativas, permisos, relaciones, importación, documentos, cifrado, restauración y exclusión de estado sensible en logs.

## Reglas para contribuciones y futuras publicaciones

- No agregar archivos .env, claves, bases, respaldos, exportaciones ni capturas operativas.
- Ejecutar las comprobaciones de seguridad y pruebas antes de confirmar cambios.
- Conservar los avisos de las dependencias y distinguir recursos institucionales de la licencia MIT del código.
- Mantener los datos persistentes fuera de wwwroot y de los binarios publicados.
- Tratar los reportes de vulnerabilidad por un canal privado, según SECURITY.md.
- No convertir el estado funcional inicial en una declaración de aptitud para producción; revisar las limitaciones del README.
