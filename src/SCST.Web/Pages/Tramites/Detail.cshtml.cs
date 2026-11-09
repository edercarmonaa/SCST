using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCST.Core;
using SCST.Infrastructure;
namespace SCST.Web.Pages.Tramites;
public class DetailModel(Database db,TramiteService service,IWebHostEnvironment env):BasePage
{
    public Tramite Item{get;set;}=null!;
    public List<Catalog> Validators{get;set;}=[];
    [BindProperty]public FollowUp Follow{get;set;}=new(){Date=DateOnly.FromDateTime(DateTime.Today)};
    [BindProperty]public Reentry Reentry{get;set;}=new(){Date=DateOnly.FromDateTime(DateTime.Today)};
    private async Task<bool> Load(int id){Item=(await service.Detail(id))!;Validators=await db.Catalogs.Where(x=>x.Kind==CatalogKind.Validador).AsNoTracking().ToListAsync();return Item!=null;}
    public async Task<IActionResult> OnGet(int id,int? follow){if(!await Load(id))return NotFound();if(follow.HasValue)Follow=Item.FollowUps.Single(x=>x.Id==follow);return Page();}
    public async Task<IActionResult> OnGetPdf(int id,bool download=false){if(!await Load(id))return NotFound();var bytes=Reports.Covers([Item],await db.Settings.SingleAsync(),Path.Combine(env.WebRootPath,"images/institutional/veracruz-institucional.png"));var name="Caratula_"+string.Concat(Item.Folio.Select(c=>char.IsLetterOrDigit(c)||c is '-' or '_'?c:'_'))+".pdf";if(download)return File(bytes,"application/pdf",name);Response.Headers.ContentDisposition="inline; filename="+name;return File(bytes,"application/pdf");}
    public async Task<IActionResult> OnPostFollowAsync(int id){if(!CanWrite)return Forbid();try{Follow.TramiteId=id;await service.SaveFollowUp(Follow,Actor);Message("Seguimiento guardado.");return RedirectToPage(new{id});}catch(DomainException e){if(!await Load(id))return NotFound();return Fail(e);}}
    public async Task<IActionResult> OnPostReentryAsync(int id){if(!CanWrite)return Forbid();try{Reentry.TramiteId=id;await service.SaveReentry(Reentry,Actor);Message("Reingreso registrado en el expediente original.");return RedirectToPage(new{id});}catch(DomainException e){if(!await Load(id))return NotFound();return Fail(e);}}
    public async Task<IActionResult> OnPostDeleteAsync(int id){if(!CanWrite)return Forbid();await service.Toggle("Tramite",id,false,Actor);Message("Expediente enviado a papelera. Se conservaron sus relaciones.");return RedirectToPage("Index");}
    public async Task<IActionResult> OnPostDeleteChildAsync(int id,string entity,int child){if(!CanWrite)return Forbid();var valid=entity=="FollowUp"?await db.FollowUps.AnyAsync(x=>x.Id==child&&x.TramiteId==id):entity=="Reentry"&&await db.Reentries.AnyAsync(x=>x.Id==child&&x.TramiteId==id);if(!valid)return BadRequest();await service.Toggle(entity,child,false,Actor);return RedirectToPage(new{id});}
}
