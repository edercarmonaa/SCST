# Especificación funcional del SCST

El SCST administra localmente expedientes y su seguimiento administrativo. La solicitud maestra proporcionada por el usuario define el alcance. El Excel y la carátula son referencias de datos y diseño, no instrucciones operativas adicionales.

## Actores y permisos

| Operación | Administrador | Capturista | Consulta |
|---|---|---|---|
| Consultar trámites, oficios y estadísticas | Sí | Sí | Sí |
| Exportar listados y reportes | Sí | Sí | Sí |
| Crear/editar trámites y oficios | Sí | Sí | No |
| Seguimientos, reingresos y relaciones | Sí | Sí | No |
| Importar Excel y acciones múltiples | Sí | Sí | No |
| Papelera y restauración lógica | Sí | Sí | No |
| Usuarios, catálogos, configuración y auditoría | Sí | No | No |
| Respaldo y restauración de base | Sí | No | No |

Las páginas administrativas tienen política Admin. Las escrituras de expedientes, oficios, relaciones y actividades verifican permiso en el servicio y/o manejador. La lectura no concede acceso a la papelera al rol Consulta.

## Módulos

El acceso incluye configuración inicial, login local, cambio obligatorio de contraseña temporal y perfil. Inicio resume el ejercicio y enlaza pendientes históricos. Trámites incluye listado paginado de 25, filtros de año o todos, búsqueda, columnas por usuario, captura, detalle, edición, seguimiento, reingresos y carátula. Oficios incluye listado, captura, detalle, edición y vinculación múltiple. Reportes filtra año, mes o intervalo por recepción o expedición, y presenta seis grupos estadísticos con exportación. Importación ofrece plantilla, validación, revisión de nombres, confirmación parcial y archivo de errores. Administración incluye usuarios, cinco catálogos, configuración, auditoría, papelera y respaldo.

## Datos y relaciones

Cada expediente exige folio, nombre compuesto, recepción, tipo y nivel. El solicitante se divide en nombres, primer apellido y segundo opcional. No se crea catálogo de personas. El folio permanece textual y reservado incluso en papelera. El identificador interno sostiene las relaciones cuando cambia el folio o ejercicio.

Un expediente tiene actividades y reingresos múltiples. Cada actividad registra fecha de negocio, descripción, observaciones, responsable opcional y usuario capturista. Un oficio pertenece a un ejercicio y se relaciona con muchos expedientes; cada expediente admite varios oficios. Los catálogos se desactivan, no se eliminan físicamente. No hay catálogo de CCT.

Se diferencian estatus, resultado de validación, fecha de validación, respuesta del validador, conclusión, expedición y respuesta a ventanilla. No se deduce uno de otro. El resultado documental nunca se imprime en la carátula.

## Criterios de aceptación

La unicidad, conservación de relaciones, permisos, actualización de ejercicio, importación parcial, semáforo y recuperación deben comprobarse mediante pruebas. Las pantallas y documentos requieren revisión visual y uso con teclado. La instalación debe ensayarse sin SDK ni internet. Una actualización debe verificar firma e integridad, respaldar y conservar datos. La lista de aceptación original continúa siendo la referencia final; un build correcto no certifica por sí solo todos esos criterios.

## Exclusiones

No se implementan adjuntos, repositorio de documentos, purga, ERP, formatos automáticos de folio, aprobación compleja de reingresos, fusión, análisis predictivo, historial por campo ni notificaciones automáticas. Las carátulas se generan bajo demanda y no se almacenan como versiones históricas.
