using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCST.Core;
using SCST.Infrastructure;
namespace SCST.Web.Pages;
public record TrashItem(int Id,string Entity,string Label);
public class TrashModel(Database db,TramiteService service):BasePage
{
    public List<TrashItem> Items{get;set;}=[];
    [BindProperty(SupportsGet=true)]public int P{get;set;}=1;
    [BindProperty(SupportsGet=true)]public string Entity{get;set;}="Tramite";
    public int Count{get;set;}
    public async Task OnGet(){P=Math.Max(1,P);IQueryable<TrashItem> q=Entity switch{"Oficio"=>db.Oficios.IgnoreQueryFilters().Where(x=>x.Deleted).Select(x=>new TrashItem(x.Id,"Oficio",x.Number)),"FollowUp"=>db.FollowUps.IgnoreQueryFilters().Where(x=>x.Deleted&&!x.Tramite.Deleted).Select(x=>new TrashItem(x.Id,"FollowUp",x.Activity)),"Reentry"=>db.Reentries.IgnoreQueryFilters().Where(x=>x.Deleted&&!x.Tramite.Deleted).Select(x=>new TrashItem(x.Id,"Reentry",x.Reason)),_=>db.Tramites.IgnoreQueryFilters().Where(x=>x.Deleted).Select(x=>new TrashItem(x.Id,"Tramite",x.Folio))};Count=await q.CountAsync();Items=await q.Skip((P-1)*25).Take(25).ToListAsync();}
    public async Task<IActionResult> OnPostAsync(string entity,int id){await service.Toggle(entity,id,true,Actor);Message("Registro restaurado con sus relaciones.");return RedirectToPage(new{Entity=entity});}
}
