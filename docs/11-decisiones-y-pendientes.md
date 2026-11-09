# Decisiones y pendientes

## Corrección del asistente inicial

Se corrigió el destino del formulario de finalización: al mostrarse después de `Setup?handler=Key`, el formulario sin manejador explícito heredaba Key y volvía a intentar generar la clave. Ahora apunta explícitamente a Complete. Al regresar al asistente se puede introducir la clave guardada directamente en el formulario. La prueba HTTP obtiene y usa la acción real del HTML después de generar la clave, y completó configuración e inicio de sesión con datos aislados. No se reemplazó la clave existente del usuario.

## Inspección y decisiones

El repositorio SCST estaba vacío. No existían solución, código, datos ni AGENTS.md que preservar. Los dos originales proporcionados se pudieron leer y permanecen sin modificación. El Excel tiene Duplicados, Correspondencia y Catalogos. Se identificaron 45 trámites con folio y una proyección por fórmulas para correspondencia, no un historial separado. La carátula contiene dos ejemplos del mismo formato con datos de expediente y seguimiento.

Se crearon cuatro proyectos y se mantuvo el stack solicitado. El SDK 9.0.320 y runtime 9.0.22 están instalados. EF Core/SQLite permanecen en 9 y se actualizaron sus parches a 9.0.22. ClosedXML y PDFsharp son dependencias abiertas para XLSX y PDF. Bootstrap 5.3.3 y HTMX se copiaron como recursos locales de proyectos disponibles en el equipo. Los logotipos se copiaron exactamente desde los archivos suministrados.

No se encontró el manual oficial de identidad ni archivos autorizados de Gotham/Minion. Se aplica la paleta indicada y Arial del sistema. La aprobación institucional del diseño sigue pendiente. El sistema no contiene datos institucionales inventados: el asistente solicita dependencia, unidad y área.

El estado inicial es PENDIENTE. Los catálogos administrativos generales alimentan los valores iniciales y sus referencias son estables. El directorio de personal del origen se excluye de los nuevos fuentes públicos; los validadores se capturan por instalación. Los reingresos tienen entidad propia porque existen celdas con varias fechas. Los documentos no se guardan como repositorio. Se conserva el nombre original importado y se ofrece revisión humana de su división.

El auxiliar de mantenimiento comparte ejecutable con Web, sin crear un quinto proyecto. El acceso inicial es solo loopback; no se habilita red local. Los respaldos vinculan la clave local al usuario Windows mediante DPAPI y permiten recuperación externa con material independiente.

## Estado verificable

Implementados: solución, migraciones, servicios, autenticación, roles, configuración inicial, catálogos, trámites, validadores, seguimiento, reingresos, oficios y relaciones, semáforo, dashboard, reportes, XLSX, importación con vista previa, PDF, auditoría básica, papelera, respaldos cifrados, auxiliar de restauración, scripts y documentación. La prueba HTTP recorrió el flujo principal con datos sintéticos y validó las restricciones de Consulta. Se generó y reabrió PDF multipágina. Compilación sin advertencias ni errores. La revisión de paquetes no detectó avisos conocidos después de actualizar.

Se ejecutaron 33 pruebas con resultado correcto, incluyendo búsqueda Unicode, lectura del libro original sin modificación y restauración de base real con DPAPI. El análisis original produjo 45 filas: 40 válidas y 5 rechazadas por valores de fecha; esos rechazos requieren revisión, no sustitución por fechas inventadas. Se verificó que el hash del archivo original no cambió. La publicación autocontenida Windows x64 se generó correctamente (aproximadamente 139 MB sin comprimir).

La aceptación **no está completa**. El código existente es una implementación funcional inicial con los siguientes límites que deben resolverse antes de entrega productiva:

| Pendiente | Motivo y criterio para cerrar |
|---|---|
| Fidelidad visual de carátula | La composición inicial preserva contenido pero simplifica tablas y posiciones. Cotejar y ajustar contra Word renderizado |
| UI responsive y accesibilidad visual | Herramienta UI no disponible o detenida; revisar escritorio/móvil, teclado, contraste, pestañas y formularios |
| Instalador y lanzador | Instalación, permisos y acceso directo probados en carpetas aisladas; falta ensayo gráfico y equipo limpio |
| Actualización firmada | Mecanismo escrito; falta certificado institucional y ensayo completo de actualización/rollback |
| Recuperación en otro equipo físico | Pruebas de recuperación en otro directorio no certifican otra instalación Windows |
| Revisión de migración original | Validar división de nombres, catálogos desconocidos y ejercicio de los oficios importados |
| Soporte de .NET 9 | Finaliza el 10 de noviembre de 2026; acordar continuidad y transición antes de producción |

No se han cargado datos reales en una base operativa ni se han creado credenciales de producción. El instalador se ensayó exclusivamente en .local, con un acceso directo en una carpeta de prueba, sin modificar escritorio ni menú Inicio del usuario. La prueba HTTP también pasó sobre el ejecutable autocontenido con DOTNET_ROOT inválido para verificar que utiliza su runtime incluido. Los datos de pruebas no se incluyen en la publicación.

## Siguiente validación

Renderizar la carátula Word y PDF y ajustar el formato; probar la interfaz con teclado y móvil; ensayar el paquete autocontenido e instalación limpia; firmar y probar actualización; ejecutar un ciclo de respaldo/restauración en otro equipo autorizado. El registro de pruebas debe actualizarse con evidencia real, sin convertir verificaciones pendientes en afirmaciones de funcionamiento.

## Preparación de publicación pública

La revisión actual y sus resultados están en [preparación pública](13-preparacion-publica.md). Las verificaciones anteriores son registros de desarrollo y no reemplazan la batería actual. Se eliminó el directorio de personal del seed público, se aislaron los grafos recibidos por formularios y se bloquearon las dependencias. La distribución pública inicia con un único commit limpio, sin historial de desarrollo anterior, y utiliza licencia MIT para el código y la documentación propios.
