using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCST.Core;
using SCST.Infrastructure;
namespace SCST.Web.Pages.Tramites;
public class IndexModel(Database db,TramiteService service,IWebHostEnvironment env):BasePage
{
    [BindProperty(SupportsGet=true)]public int? Year{get;set;}
    [BindProperty(SupportsGet=true)]public bool All{get;set;}
    [BindProperty(SupportsGet=true)]public string? Text{get;set;}
    [BindProperty(SupportsGet=true)]public int? Status{get;set;}
    [BindProperty(SupportsGet=true)]public int? Type{get;set;}
    [BindProperty(SupportsGet=true)]public int? Level{get;set;}
    [BindProperty(SupportsGet=true)]public int? Validator{get;set;}
    [BindProperty(SupportsGet=true)]public bool Historical{get;set;}
    [BindProperty(SupportsGet=true)]public bool WithoutLetter{get;set;}
    [BindProperty(SupportsGet=true)]public string? Signal{get;set;}
    [BindProperty(SupportsGet=true)]public int P{get;set;}=1;
    [BindProperty]public int[] Selected{get;set;}=[];
    [BindProperty]public int? BulkStatus{get;set;}
    [BindProperty]public int? BulkValidator{get;set;}
    public List<Tramite> Items{get;set;}=[];
    public List<Catalog> Catalogs{get;set;}=[];
    public string Columns{get;set;}="";
    public int Count{get;set;}
    public Search Filter=>new(All?null:Year,Text,Status,Type,Level,Validator,Historical,WithoutLetter,Signal:Signal);
    private async Task Load(){Year??=(await db.Settings.SingleAsync()).Year;P=Math.Max(P,1);var q=service.Query(Filter);Count=await q.CountAsync();Items=await q.OrderByDescending(x=>x.Folio).Skip((P-1)*25).Take(25).ToListAsync();Catalogs=await db.Catalogs.AsNoTracking().Where(x=>x.Active).ToListAsync();Columns=(await db.Users.AsNoTracking().SingleAsync(x=>x.Id==Actor.Id)).Columns;}
    public async Task<IActionResult> OnGet(){await Load();if(Request.Headers["HX-Request"]=="true")return Partial("_Table",this);return Page();}
    public async Task<IActionResult> OnGetExcel(){Year??=(await db.Settings.SingleAsync()).Year;var list=await service.Query(Filter).Include(x=>x.Medium).Include(x=>x.Reentries).Include(x=>x.Letters.Where(l=>!l.Oficio.Deleted)).ThenInclude(l=>l.Oficio).AsSplitQuery().ToListAsync();return File(ExcelService.Export(list),"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet","Tramites.xlsx");}
    public async Task<IActionResult> OnPostAsync(string action)
    {
        if(!CanWrite)return Forbid();
        try
        {
            if(Selected.Length==0)throw new DomainException("Seleccione al menos un trámite.");
            if(action=="bulk"){await service.Bulk(Selected,BulkStatus,BulkValidator,Actor);Message("Expedientes actualizados.");return RedirectToPage();}
            var list=await db.Tramites.Where(x=>Selected.Contains(x.Id)).Include(x=>x.Type).Include(x=>x.Level).Include(x=>x.Status).Include(x=>x.Validator).Include(x=>x.Medium).Include(x=>x.FollowUps).ThenInclude(x=>x.Responsible).Include(x=>x.Reentries).Include(x=>x.Letters.Where(l=>!l.Oficio.Deleted)).ThenInclude(x=>x.Oficio).AsSplitQuery().AsNoTracking().ToListAsync();
            if(action=="excel")return File(ExcelService.Export(list),"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet","Tramites_Seleccion.xlsx");
            if(action=="pdf"){Response.Headers.ContentDisposition="inline; filename=Caratulas.pdf";return File(Reports.Covers(list,await db.Settings.SingleAsync(),Path.Combine(env.WebRootPath,"images/institutional/veracruz-institucional.png")),"application/pdf");}
            return BadRequest();
        }catch(DomainException e){await Load();return Fail(e);}
    }
    public async Task<IActionResult> OnPostColumnsAsync(string[] columns){var allowed=new[]{"Folio","Nombre","Recepción","Tipo","Nivel","Estatus","Validador","Semáforo","Validación","Ejercicio","CCT","TL"};var u=await db.Users.SingleAsync(x=>x.Id==Actor.Id);u.Columns=string.Join(',',columns.Intersect(allowed));await db.SaveChangesAsync();return RedirectToPage();}
}
