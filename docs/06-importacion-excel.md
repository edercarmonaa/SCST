# Importación y exportación Excel

## Inspección del original

Se inspeccionó `Control_Duplicados_2026.xlsx` en modo lectura mediante sus partes OOXML y el importador ClosedXML. Hay tres hojas: Duplicados, Correspondencia y Catalogos. Duplicados contiene 45 filas con folio; el resto de las 999 filas declaradas incluye formato y fórmulas sin datos de expediente. Correspondencia reproduce por fórmulas No., nombre completo, nivel, recepción, folio y observaciones de Duplicados. No constituye un historial nuevo y no debe duplicar los expedientes al migrar.

La hoja Catalogos orientó los valores administrativos generales de estatus, nivel, medio y tipo. La distribución pública omite el directorio de validadores: cada instalación configura sus responsables autorizados. La documentación/anexos permanece como texto, ya que la solicitud limita los catálogos administrativos a cinco. Los originales administrativos no se distribuyen. Las pruebas públicas generan libros ficticios y comprueban que su contenido no se modifica al leerlos.

## Mapeo

| Columna original | Destino |
|---|---|
| No. | No se usa como clave; se conserva la fila en la vista previa |
| FOLIO DE ENTRADA | Tramite.Folio textual |
| FECHA DE INGRESO | Reception y Year derivado |
| FECHA DE REINGRESO | Reentries.Date, una entrada por fecha |
| TIPO DE TRÁMITE | Catálogo TipoTramite |
| MEDIO DE INGRESO | Catálogo MedioIngreso |
| FOLIO TL | TlFolio |
| CCT | Cct libre |
| NOMBRE / APELLIDO PATERNO / APELLIDO MATERNO | Names / FirstSurname / SecondSurname y OriginalName |
| NIVEL EDUCATIVO | Catálogo NivelEducativo |
| DOCUMENTACIÓN / ANEXOS | Documentation |
| VALIDADOR | Catálogo Validador opcional |
| FECHA DE RESPUESTA DE VALIDADOR | ValidatorResponse |
| ESTATUS | Catálogo Estatus, PENDIENTE cuando está vacío |
| INCIDENCIA/ PENDIENTE | Incident |
| FECHA DE CAPTURA | SourceCapture, independiente de CreatedUtc |
| CICLO ESCOLAR | SchoolCycle |
| FOLIO DE CERTIFICADO | CertificateFolio |
| FECHA DE EXPEDICIÓN | Issue |
| OFICIO DE NOTIFICACIÓN/RESPUESTA | Oficios y relaciones, con ejercicio de recepción y fecha desconocida vacía |
| FECHA DE RESPUESTA Y/O NOTIFICACION A VENTANILLA | WindowResponse |
| DIAS DE ATENCIÓN | No se importa; el semáforo se recalcula según reglas definitivas |
| OBSERVACION | Observations |

La plantilla SCST agrega fechas opcionales de conclusión y asignación. Resultado y fecha de validación se editan en expediente; no se deducen de incidencias ni se inventan desde el original.

## Validación y confirmación

El archivo debe ser .xlsx de hasta 20 MB y hasta 20 000 filas. Los encabezados son los del original o los de la plantilla descargable. El importador prefiere Duplicados; de no existir, toma la primera hoja. No se ejecutan macros.

Los folios usan el texto/formato recuperable de Excel para conservar ceros. Si el archivo almacenó un número sin formato, no es posible reconstruir ceros que ya no están presentes. Los catálogos se comparan sin distinguir mayúsculas, con espacios exteriores normalizados; no se crean valores desconocidos automáticamente.

Se aceptan fechas Excel tipadas y textos `yyyy-MM-dd`, `dd/MM/yyyy`, `d/M/yyyy` y `dd-MM-yyyy`. La convención explícita de la plantilla es año-mes-día. No se adivinan formatos mes/día/año. Se rechazan fechas futuras, inválidas y valores ambiguos fuera de esas convenciones. Las celdas con varios reingresos se separan por salto de línea. Las fechas ausentes siguen vacías. El motivo original no existe en el libro: se identifica como reingreso registrado en el libro original, sin inventar una causa administrativa.

Las celdas de oficios se separan por salto de línea y, para los números institucionales concatenados del original, por espacios antes de `SEV/`. Los números con espacios internos no se dividen indiscriminadamente. Se vincula por número y ejercicio de recepción; cuando este no coincide con el ejercicio real del oficio, el operador debe corregir el archivo antes de confirmar o editar el oficio posteriormente. No se inventa una fecha de emisión. Debe revisarse este criterio en la aceptación de la migración histórica.

Si faltan apellidos y el origen entrega un nombre completo, se propone una división por las dos últimas palabras, conservando el original y mostrando campos editables. Los apellidos compuestos requieren corrección humana; no se presume una división infalible.

Los folios existentes, incluidos eliminados, se omiten sin sobrescritura. Todos los registros de un folio repetido dentro del archivo se rechazan. Cada fila válida se importa atómicamente con reingresos y vínculos. Los campos de nombres pueden corregirse en la vista previa; otros errores se corrigen en el Excel y se carga nuevamente. La vista previa se guarda temporalmente bajo el Id del usuario y un token aleatorio y caduca en 24 horas.

El resumen presenta procesadas, válidas, omitidas, inválidas e importadas. El XLSX de errores contiene fila, texto original y explicación. No se añade un campo permanente de origen al expediente.

## Exportación

El listado completo o filtrado y la selección se exportan a XLSX con identificadores textuales y fechas tipadas. La plantilla vacía usa los mismos encabezados, por lo que puede volver a importarse. Los reportes estadísticos producen seis hojas editables: Resumen General, Estadísticas Mensuales, Estatus, Niveles Educativos, Validadores y Semáforos. Incluyen cantidades y porcentajes numéricos, sin gráficos incrustados.
