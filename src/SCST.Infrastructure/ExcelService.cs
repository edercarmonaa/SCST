using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using SCST.Core;
using System.Globalization;

namespace SCST.Infrastructure;
public sealed class ImportRow
{
    public int Row { get; set; }
    public Tramite Data { get; set; } = new();
    public List<string> Errors { get; set; } = [];
    public List<Reentry> Reentries { get; set; } = [];
    public List<string> Oficios { get; set; } = [];
    public string? Raw { get; set; }
    public bool Exists { get; set; }
}
public class ExcelService(Database db,TramiteService service)
{
    public static readonly string[] Headers=["FOLIO DE ENTRADA","FECHA DE INGRESO","NOMBRE","APELLIDO PATERNO","APELLIDO MATERNO","TIPO DE TRÁMITE","NIVEL EDUCATIVO","ESTATUS","MEDIO DE INGRESO","FOLIO TL","CCT","CICLO ESCOLAR","FOLIO DE CERTIFICADO","DOCUMENTACIÓN / ANEXOS","INCIDENCIA/ PENDIENTE","OBSERVACION","VALIDADOR","FECHA DE RESPUESTA DE VALIDADOR","FECHA DE CAPTURA","FECHA DE EXPEDICIÓN","FECHA DE RESPUESTA Y/O NOTIFICACION A VENTANILLA","FECHA DE CONCLUSIÓN","FECHA DE ASIGNACIÓN","FECHA DE REINGRESO","OFICIO DE NOTIFICACIÓN/RESPUESTA"];
    public async Task<List<ImportRow>> Preview(Stream stream)
    {
        using var input=new MemoryStream();await stream.CopyToAsync(input);if(input.Length>20*1024*1024)throw new DomainException("El archivo supera 20 MB.");input.Position=0;
        using(var zip=new System.IO.Compression.ZipArchive(input,System.IO.Compression.ZipArchiveMode.Read,true))
            if(zip.Entries.Count>10000 || zip.Entries.Sum(e=>e.Length)>200L*1024*1024)throw new DomainException("El contenido descomprimido del libro supera el límite permitido.");
        input.Position=0;using var book=new XLWorkbook(input);
        var sheet=book.Worksheets.FirstOrDefault(x=>x.Name=="Duplicados")??book.Worksheets.First();
        var headers=sheet.Row(1).CellsUsed().Where(c=>!string.IsNullOrWhiteSpace(c.GetString())).ToDictionary(c=>c.GetString().Trim().ToUpperInvariant(),c=>c.Address.ColumnNumber);
        if(!Headers.Take(7).All(headers.ContainsKey))throw new DomainException("La hoja debe contener los encabezados de la plantilla SCST o Duplicados.");
        var catalogs=await db.Catalogs.AsNoTracking().ToListAsync();var existing=(await db.Tramites.IgnoreQueryFilters().Select(x=>x.Folio).ToListAsync()).ToHashSet(StringComparer.Ordinal);
        var rows=new List<ImportRow>();
        foreach(var row in sheet.RowsUsed().Skip(1))
        {
            string Get(string key)=>headers.TryGetValue(key,out var col)?row.Cell(col).GetFormattedString().Trim():"";
            if(Headers.All(k=>Get(k)==""))continue;
            if(rows.Count>=20000)throw new DomainException("El límite por archivo es de 20 000 registros.");
            var r=new ImportRow{Row=row.RowNumber()};rows.Add(r);var t=r.Data;
            r.Raw=string.Join(" | ",Headers.Select(Get));
            int? Cat(string key,CatalogKind kind,bool required=false)
            {var value=Get(key);if(value==""&&!required)return null;var c=catalogs.SingleOrDefault(x=>x.Kind==kind && x.Active && x.Name.Equals(value,StringComparison.OrdinalIgnoreCase));if(c==null)r.Errors.Add($"{key}: catálogo desconocido o inactivo ({value}).");return c?.Id;}
            DateOnly? Date(string key,bool required=false,string? supplied=null)
            {
                var text=supplied??Get(key);if(text==""){if(required)r.Errors.Add($"{key}: obligatorio.");return null;}
                DateOnly result;
                if(supplied==null && headers.TryGetValue(key,out var col) && row.Cell(col).DataType==XLDataType.DateTime) result=DateOnly.FromDateTime(row.Cell(col).GetDateTime());
                else if(!DateOnly.TryParseExact(text,["yyyy-MM-dd","dd/MM/yyyy","d/M/yyyy","dd-MM-yyyy"],CultureInfo.InvariantCulture,DateTimeStyles.None,out result)) {r.Errors.Add($"{key}: fecha inválida o ambigua ({text}).");return null;}
                if(result>DateOnly.FromDateTime(DateTime.Today))r.Errors.Add($"{key}: no se permiten fechas futuras.");return result;
            }
            t.Folio=Get(Headers[0]);t.Reception=Date(Headers[1],true)??default;t.Names=Get(Headers[2]);t.FirstSurname=Get(Headers[3]);t.SecondSurname=Get(Headers[4]);
            t.OriginalName=string.Join(" ",new[]{t.Names,t.FirstSurname,t.SecondSurname}.Where(x=>x!=""));
            if(t.FirstSurname=="" && t.Names.Split(' ',StringSplitOptions.RemoveEmptyEntries) is var parts && parts.Length>=3){t.SecondSurname=parts[^1];t.FirstSurname=parts[^2];t.Names=string.Join(" ",parts[..^2]);}
            t.TypeId=Cat(Headers[5],CatalogKind.TipoTramite,true)??0;t.LevelId=Cat(Headers[6],CatalogKind.NivelEducativo,true)??0;
            t.StatusId=Get(Headers[7])==""?catalogs.Single(x=>x.Kind==CatalogKind.Estatus&&x.Name=="PENDIENTE").Id:Cat(Headers[7],CatalogKind.Estatus,true)??0;
            t.MediumId=Cat(Headers[8],CatalogKind.MedioIngreso);t.TlFolio=Get(Headers[9]);t.Cct=Get(Headers[10]);t.SchoolCycle=Get(Headers[11]);t.CertificateFolio=Get(Headers[12]);t.Documentation=Get(Headers[13]);t.Incident=Get(Headers[14]);t.Observations=Get(Headers[15]);t.ValidatorId=Cat(Headers[16],CatalogKind.Validador);
            t.ValidatorResponse=Date(Headers[17]);t.SourceCapture=Date(Headers[18]);t.Issue=Date(Headers[19]);t.WindowResponse=Date(Headers[20]);t.Conclusion=Date(Headers[21]);t.Assignment=Date(Headers[22]);
            if(Get(Headers[23])!="")foreach(var part in Get(Headers[23]).Split(['\r','\n'],StringSplitOptions.RemoveEmptyEntries)) {var date=Date(Headers[23],supplied:part.Trim());if(date.HasValue)r.Reentries.Add(new(){Date=date.Value,Reason="Reingreso registrado en el libro original"});}
            if(Get(Headers[24])!="") foreach(var line in Get(Headers[24]).Split(['\r','\n'],StringSplitOptions.RemoveEmptyEntries))
                foreach(var letter in System.Text.RegularExpressions.Regex.Split(line,@"\s+(?=SEV/)").Where(x=>!string.IsNullOrWhiteSpace(x)))r.Oficios.Add(letter.Trim());
            try{t.Normalize();}catch(DomainException e){r.Errors.Add(e.Message);}
            r.Exists=existing.Contains(t.Folio);if(r.Exists)r.Errors.Add("Folio existente: se omitirá sin sobrescribir.");
        }
        foreach(var group in rows.GroupBy(x=>x.Data.Folio,StringComparer.Ordinal).Where(g=>g.Count()>1)) foreach(var r in group)r.Errors.Add("Folio duplicado dentro del archivo: se rechazan todas las apariciones.");
        return rows;
    }
    public async Task<int> Import(List<ImportRow> rows,Actor actor)
    {
        Access.RequireWrite(actor.Role);int count=0;
        foreach(var row in rows.Where(x=>x.Errors.Count==0))
        {
            var t=row.Data;t.Normalize();
            if(await db.Tramites.IgnoreQueryFilters().AnyAsync(x=>x.Folio==t.Folio)){row.Errors.Add("Folio existente al confirmar.");row.Exists=true;continue;}
            await service.CheckCatalog(t.TypeId,CatalogKind.TipoTramite);await service.CheckCatalog(t.LevelId,CatalogKind.NivelEducativo);await service.CheckCatalog(t.StatusId,CatalogKind.Estatus);await service.CheckCatalog(t.ValidatorId,CatalogKind.Validador);await service.CheckCatalog(t.MediumId,CatalogKind.MedioIngreso);
            await using var tx=await db.Database.BeginTransactionAsync();
            db.Tramites.Add(t);foreach(var re in row.Reentries)t.Reentries.Add(re);
            foreach(var number in row.Oficios.Distinct())
            {
                var letter=await db.Oficios.IgnoreQueryFilters().SingleOrDefaultAsync(x=>x.Number==number && x.Year==t.Year);
                if(letter==null){letter=new(){Number=number,Year=t.Year};db.Oficios.Add(letter);}
                t.Letters.Add(new(){Oficio=letter});
            }
            await db.SaveChangesAsync();service.Audit(actor,"Importación","Tramite",t.Id,t.Folio);await db.SaveChangesAsync();await tx.CommitAsync();count++;
        }
        return count;
    }
    public static byte[] Export(IEnumerable<Tramite> items)
    {
        using var book=new XLWorkbook();var s=book.AddWorksheet("Duplicados");for(var i=0;i<Headers.Length;i++)s.Cell(1,i+1).Value=Headers[i];int row=2;
        foreach(var t in items)
        {
            string D(DateOnly? d)=>d?.ToString("yyyy-MM-dd")??"";
            string[] values=[t.Folio,D(t.Reception),t.Names,t.FirstSurname,t.SecondSurname??"",t.Type.Name,t.Level.Name,t.Status.Name,t.Medium?.Name??"",t.TlFolio??"",t.Cct??"",t.SchoolCycle??"",t.CertificateFolio??"",t.Documentation??"",t.Incident??"",t.Observations??"",t.Validator?.Name??"",D(t.ValidatorResponse),D(t.SourceCapture),D(t.Issue),D(t.WindowResponse),D(t.Conclusion),D(t.Assignment),string.Join("\n",t.Reentries.Select(x=>D(x.Date))),string.Join("\n",t.Letters.Select(x=>x.Oficio.Number))];
            for(int c=0;c<values.Length;c++){s.Cell(row,c+1).Style.NumberFormat.Format="@";s.Cell(row,c+1).Value=values[c];}
            DateOnly?[] dates=[t.Reception,t.ValidatorResponse,t.SourceCapture,t.Issue,t.WindowResponse,t.Conclusion,t.Assignment];int[] positions=[2,18,19,20,21,22,23];for(int d=0;d<dates.Length;d++)if(dates[d].HasValue){s.Cell(row,positions[d]).Value=dates[d]!.Value.ToDateTime(TimeOnly.MinValue);s.Cell(row,positions[d]).Style.NumberFormat.Format="yyyy-mm-dd";}row++;
        }
        Style(s);using var m=new MemoryStream();book.SaveAs(m);return m.ToArray();
    }
    public static byte[] Errors(IEnumerable<ImportRow> rows)
    {
        using var b=new XLWorkbook();var s=b.AddWorksheet("Errores");s.Cell(1,1).Value="Fila";s.Cell(1,2).Value="Datos originales";s.Cell(1,3).Value="Errores";int i=2;foreach(var r in rows.Where(r=>r.Errors.Count>0)){s.Cell(i,1).Value=r.Row;s.Cell(i,2).Value=r.Raw;s.Cell(i++,3).Value=string.Join("; ",r.Errors);}Style(s);using var m=new MemoryStream();b.SaveAs(m);return m.ToArray();
    }
    internal static void Style(IXLWorksheet s){s.Row(1).Style.Font.Bold=true;s.Row(1).Style.Fill.BackgroundColor=XLColor.FromHtml("#7A1737");s.Row(1).Style.Font.FontColor=XLColor.White;s.SheetView.FreezeRows(1);s.Columns().Width=24;s.RangeUsed()?.SetAutoFilter();}
}
