# Reglas de negocio

1. El folio oficial es manual, textual y único en toda la base, incluida papelera. No se genera ni reutiliza. Cambiarlo no cambia el Id.
2. El solicitante exige nombres y primer apellido. Se convierten consistentemente a mayúsculas conservando acentos y Ñ. El segundo apellido es opcional.
3. La recepción determina el ejercicio. Una edición de recepción actualiza el ejercicio sin reemplazar relaciones.
4. El semáforo cuenta días naturales desde la recepción original. Hasta 15 es verde, 16 a 30 amarillo y desde 31 rojo. Aplica a pendientes actuales e históricos. Seguimientos, reingresos y asignaciones no lo reinician.
5. RECIBIDO, EN PROCESO y PENDIENTE se clasifican inicialmente como pendientes según el catálogo original. CONCLUIDO y NO PROCEDENTE no participan. Los estatus base se preservan para que la importación tenga un valor inicial estable.
6. La validación documental admite vacío, pendiente de validar, validado o con inconsistencias. No cambia automáticamente el estatus, la conclusión ni la expedición. Su fecha es manual y se excluye de carátulas.
7. Los catálogos inactivos conservan referencias. No admiten nuevas asignaciones. Mantener el mismo valor inactivo de un expediente histórico sí es válido. Desactivar un validador con pendientes requiere confirmar la advertencia.
8. Un seguimiento conserva su fecha de captura original y el usuario que lo registró, aunque otro autorizado lo edite. La fecha de actividad y responsable son campos separados.
9. Cada reingreso pertenece al expediente original y guarda fecha, motivo y observaciones. No genera otro trámite.
10. Un oficio es único por número y ejercicio, incluso en papelera. Los vínculos son muchos a muchos. Desvincular exige confirmación; eliminar lógicamente conserva los vínculos.
11. Las fechas de captura de origen, respuesta del validador, expedición y ventanilla son independientes. En captura manual se advierten fechas futuras o anteriores a recepción, y el usuario puede confirmarlas. La importación rechaza fechas futuras y formatos ambiguos.
12. La papelera no purga ni reutiliza identificadores. Consulta no tiene acceso. Cada restauración queda auditada.
13. Los cambios múltiples de estatus y validador verifican catálogos activos y permiso de escritura. Excel/PDF agrupados usan únicamente los expedientes seleccionados activos.
14. Las cuentas temporales deben cambiar la contraseña antes de acceder al resto del sistema. Cinco fallos bloquean 15 minutos. Cambiar contraseña o rol invalida sesiones anteriores por sello de seguridad.
15. No se permite desactivar la cuenta propia ni desactivar o degradar al último administrador activo. Los usuarios inactivos conservan sus referencias históricas.
