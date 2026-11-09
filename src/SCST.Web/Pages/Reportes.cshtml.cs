using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCST.Core;
using SCST.Infrastructure;
namespace SCST.Web.Pages;
public class ReportesModel(Database db,TramiteService service,IWebHostEnvironment env):BasePage
{
    [BindProperty(SupportsGet=true)]public int? Year{get;set;}
    [BindProperty(SupportsGet=true)]public int? Month{get;set;}
    [BindProperty(SupportsGet=true)]public DateOnly? From{get;set;}
    [BindProperty(SupportsGet=true)]public DateOnly? To{get;set;}
    [BindProperty(SupportsGet=true)]public bool ByIssue{get;set;}
    public List<Tramite> Items{get;set;}=[];
    public Dictionary<string,Dictionary<string,int>> Groups{get;set;}=[];
    public async Task OnGet(){Year??=(await db.Settings.SingleAsync()).Year;var start=From??new DateOnly(Math.Clamp(Year.Value,1900,9999),Math.Clamp(Month??1,1,12),1);var end=To??(Month.HasValue?start.AddMonths(1).AddDays(-1):new DateOnly(start.Year,12,31));Items=await service.Query(new(From:start,To:end,ByIssue:ByIssue)).ToListAsync();Groups=Reports.Summaries(Items);if(ByIssue)Groups["Estadísticas Mensuales"]=Items.Where(t=>t.Issue.HasValue).GroupBy(t=>t.Issue!.Value.ToString("yyyy-MM")).ToDictionary(g=>g.Key,g=>g.Count());}
    public async Task<IActionResult> OnGetExcel(){await OnGet();return File(Reports.StatisticsExcel(Items,ByIssue),"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet","Estadisticas.xlsx");}
    public async Task<IActionResult> OnGetPdf(bool download=false){await OnGet();var period=$"{From?.ToString("yyyy-MM-dd")??Year.ToString()}_{To?.ToString("yyyy-MM-dd")??Month?.ToString()??"anual"}";var file=$"Reporte_Estadisticas_{period}.pdf";var bytes=Reports.StatisticsPdf(Items,await db.Settings.SingleAsync(),period,Path.Combine(env.WebRootPath,"images/institutional/veracruz-institucional.png"),ByIssue);if(download)return File(bytes,"application/pdf",file);Response.Headers.ContentDisposition="inline; filename="+file;return File(bytes,"application/pdf");}
}
