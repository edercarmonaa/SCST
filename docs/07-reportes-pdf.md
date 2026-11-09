# Reportes y carátulas PDF

La carátula de referencia es `Caratula.docx`. La inspección OOXML identifica papel carta vertical, encabezado con imagen institucional y dos ejemplos del mismo formato. Cada ejemplo contiene tipo, usuario, nivel educativo, ingreso del trámite, fecha, folio DAAC, observaciones y Atención y Seguimiento. El texto de los ejemplos es dato personal de muestra del original, no una plantilla fija para nuevos expedientes.

`Reports.Covers` genera una carátula por trámite en tamaño carta. Usa tipo, nombre completo, nivel, recepción, folio y observaciones. Agrega actividades cronológicamente de más antigua a más reciente y páginas de continuación. Cada expediente comienza en página nueva y su numeración Página X de Y vuelve a empezar. La agrupación se ordena por folio ascendente. No se incluye resultado ni fecha de validación documental. Los archivos se producen en memoria y no se guardan como un archivo histórico del expediente.

La previsualización usa una respuesta inline y la descarga individual sanea únicamente el nombre `Caratula_<folio>.pdf`. La selección múltiple produce un PDF único. El encabezado utiliza el recurso oficial suministrado, sin reconstruirlo ni cambiar proporciones. Las fuentes empleadas por PDFsharp se resuelven desde Windows; no se redistribuye Gotham o Minion sin licencia.

Los reportes estadísticos usan carta vertical con institución, periodo, fecha de generación y numeración. Presentan únicamente resúmenes, sin listados individuales. El usuario elige recepción o expedición como fecha base, año, mes o rango. Las gráficas en pantalla usan barras mediante elementos meter y tablas/textos con sus valores. Excel contiene seis hojas con tablas y porcentajes.

## Verificación y límite de fidelidad

Las pruebas generan PDF real, lo reabren con PDFsharp y verifican páginas de continuación; la prueba HTTP comprueba las respuestas PDF de estadística y carátula. El contenido se selecciona expresamente y no utiliza un volcado genérico de propiedades, evitando filtrar validación documental.

**La disposición generada todavía requiere cotejo visual contra la plantilla Word.** La versión inicial reproduce campos y secuencia, pero simplifica su distribución en una composición de texto. No se certifica equivalencia de tablas, bordes, posición o alturas. La herramienta de navegador no estuvo disponible y el control nativo se detuvo por no poder verificar la URL. No se declara aprobada la revisión visual del PDF ni de la interfaz. Antes de aceptación, renderice ambas referencias y compare página por página, especialmente textos largos, saltos de página y agrupaciones de varios expedientes.
