using ClosedXML.Excel;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Fonts;
using SCST.Core;

namespace SCST.Infrastructure;
public class Reports
{
    static Reports() { GlobalFontSettings.UseWindowsFontsUnderWindows=true; }
    public static Dictionary<string,Dictionary<string,int>> Summaries(List<Tramite> items)
    {
        Dictionary<string,int> Group(Func<Tramite,string> key)=>items.GroupBy(key).ToDictionary(g=>g.Key,g=>g.Count());
        return new(){["Resumen General"]=new(){["Total"]=items.Count},["Estadísticas Mensuales"]=Group(t=>t.Reception.ToString("yyyy-MM")),["Estatus"]=Group(t=>t.Status.Name),["Niveles Educativos"]=Group(t=>t.Level.Name),["Validadores"]=Group(t=>t.Validator?.Name??"Sin asignar"),["Semáforos"]=Group(t=>t.Signal(DateOnly.FromDateTime(DateTime.Today),t.Status.Pending)?.ToString()??"Fuera del semáforo")};
    }
    public static byte[] StatisticsExcel(List<Tramite> items,bool byIssue=false)
    {
        using var b=new XLWorkbook();var groups=Summaries(items);
        if(byIssue)groups["Estadísticas Mensuales"]=items.Where(t=>t.Issue.HasValue).GroupBy(t=>t.Issue!.Value.ToString("yyyy-MM")).ToDictionary(g=>g.Key,g=>g.Count());
        foreach(var (name,values) in groups){var s=b.AddWorksheet(name);s.Cell(1,1).Value="Concepto";s.Cell(1,2).Value="Cantidad";s.Cell(1,3).Value="Porcentaje";int i=2;foreach(var (label,count) in values){s.Cell(i,1).Value=label;s.Cell(i,2).Value=count;s.Cell(i,3).Value=items.Count==0?0:(double)count/items.Count;s.Cell(i,3).Style.NumberFormat.Format="0.0%";i++;}ExcelService.Style(s);s.Column(1).Width=45;}using var m=new MemoryStream();b.SaveAs(m);return m.ToArray();
    }
    public static byte[] Covers(List<Tramite> items,Settings settings,string? logo)
    {
        using var pdf=new PdfDocument();pdf.Info.Title="Carátulas SCST";
        foreach(var t in items.OrderBy(x=>x.Folio,StringComparer.Ordinal))
        {
            var pages=new List<PdfPage>(); XGraphics? g=null;double y=0;
            void NewPage(){g?.Dispose();var p=pdf.AddPage();p.Size=PdfSharp.PageSize.Letter;pages.Add(p);g=XGraphics.FromPdfPage(p);Header(g,settings,logo);y=140;}
            void Line(string text,bool bold=false){foreach(var line in Wrap(text,g!,new XFont("Arial",bold?11:10,bold?XFontStyleEx.Bold:XFontStyleEx.Regular),510)){if(y>710)NewPage();g!.DrawString(line,new XFont("Arial",bold?11:10,bold?XFontStyleEx.Bold:XFontStyleEx.Regular),XBrushes.Black,new XPoint(50,y));y+=16;}}
            NewPage();Line("Trámite: "+(t.Type.Name=="DUPLICADO"?"Duplicado de Certificado":t.Type.Name),true);y+=15;
            Line("Usuario: "+t.FullName);Line("Nivel Educativo: "+t.Level.Name);Line("Ingreso del trámite: "+t.Reception.ToString("dd/MM/yyyy"));Line("Folio DAAC: "+t.Folio);Line("Observaciones: "+t.Observations);y+=18;Line("Atención y Seguimiento:",true);y+=6;
            foreach(var f in t.FollowUps.Where(f=>!f.Deleted).OrderBy(f=>f.Date).ThenBy(f=>f.CreatedUtc)){Line($"{f.Date:dd/MM/yyyy}  |  {f.Responsible?.Name??"Sin asignar"}",true);Line(f.Activity);if(!string.IsNullOrEmpty(f.Observations))Line(f.Observations);y+=10;}
            g?.Dispose();for(int i=0;i<pages.Count;i++){using var footer=XGraphics.FromPdfPage(pages[i]);footer.DrawString($"Página {i+1} de {pages.Count}",new XFont("Arial",9),XBrushes.Black,new XPoint(270,755));}
        }
        using var m=new MemoryStream();pdf.Save(m);return m.ToArray();
    }
    public static byte[] StatisticsPdf(List<Tramite> items,Settings s,string period,string? logo,bool byIssue=false)
    {
        using var pdf=new PdfDocument();pdf.Info.Title="Reporte estadístico SCST";XGraphics? g=null;double y=0;
        void Page(){g?.Dispose();var p=pdf.AddPage();p.Size=PdfSharp.PageSize.Letter;g=XGraphics.FromPdfPage(p);Header(g,s,logo);y=145;}
        Page();g!.DrawString("Estadísticas · "+period,new XFont("Arial",13,XFontStyleEx.Bold),XBrushes.Black,new XPoint(50,y));y+=22;g.DrawString("Generado: "+DateTime.Now.ToString("dd/MM/yyyy HH:mm"),new XFont("Arial",9),XBrushes.Black,new XPoint(50,y));y+=25;
        var groups=Summaries(items);if(byIssue)groups["Estadísticas Mensuales"]=items.Where(t=>t.Issue.HasValue).GroupBy(t=>t.Issue!.Value.ToString("yyyy-MM")).ToDictionary(x=>x.Key,x=>x.Count());
        foreach(var (title,values) in groups){if(y>660)Page();g!.DrawString(title,new XFont("Arial",11,XFontStyleEx.Bold),XBrushes.Black,new XPoint(50,y));y+=20;foreach(var (label,count) in values){foreach(var line in Wrap($"{label}: {count} ({(items.Count==0?0:100d*count/items.Count):0.0}%)",g,new XFont("Arial",10),510)){if(y>710)Page();g!.DrawString(line,new XFont("Arial",10),XBrushes.Black,new XPoint(60,y));y+=16;}}y+=16;}
        g?.Dispose();for(int i=0;i<pdf.PageCount;i++){using var f=XGraphics.FromPdfPage(pdf.Pages[i]);f.DrawString($"Página {i+1} de {pdf.PageCount}",new XFont("Arial",9),XBrushes.Black,new XPoint(270,755));}using var m=new MemoryStream();pdf.Save(m);return m.ToArray();
    }
    private static void Header(XGraphics g,Settings s,string? logo)
    {
        if(logo!=null && File.Exists(logo)){using var img=XImage.FromFile(logo);g.DrawImage(img,50,30,510,510*img.PixelHeight/img.PixelWidth);}
        var font=new XFont("Arial",9);g.DrawString(s.Dependency,font,XBrushes.Black,new XPoint(50,105));g.DrawString(s.Unit+" · "+s.Area,font,XBrushes.Black,new XPoint(50,120));
    }
    private static IEnumerable<string> Wrap(string text,XGraphics g,XFont font,double width)
    {
        foreach(var paragraph in text.Replace("\r","").Split('\n')){var line="";foreach(var word in paragraph.Split(' ')){if(g.MeasureString(line+word,font).Width>width && line.Length>0){yield return line.TrimEnd();line="";}foreach(var ch in word){if(g.MeasureString(line+ch,font).Width>width){yield return line;line="";}line+=ch;}line+=" ";}yield return line.TrimEnd();}
    }
}
