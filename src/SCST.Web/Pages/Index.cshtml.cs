using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCST.Core;
using SCST.Infrastructure;
namespace SCST.Web.Pages;
public class IndexModel(Database db,TramiteService service):BasePage
{
    [BindProperty(SupportsGet=true)]public int? Year{get;set;}
    public List<Tramite> Items{get;set;}=[];
    public int Historical{get;set;}
    public Dictionary<string,Dictionary<string,int>> Groups=>Reports.Summaries(Items);
    public async Task OnGet(){Year??=(await db.Settings.SingleAsync()).Year;Items=await service.Query(new(Year)).ToListAsync();Historical=await db.Tramites.CountAsync(x=>x.Year<DateTime.Today.Year&&x.Status.Pending);}
}
