using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCST.Core;
using SCST.Infrastructure;
namespace SCST.Web.Pages.Tramites;
public class EditModel(Database db,TramiteService service):BasePage
{
    [BindProperty]public Tramite Input{get;set;}=new(){Reception=DateOnly.FromDateTime(DateTime.Today)};
    [BindProperty]public bool ConfirmDates{get;set;}
    public List<Catalog> Catalogs{get;set;}=[];
    private async Task Load(){Catalogs=await db.Catalogs.AsNoTracking().ToListAsync();if(Input.StatusId==0)Input.StatusId=Catalogs.Single(x=>x.Kind==CatalogKind.Estatus&&x.Name=="PENDIENTE").Id;}
    public async Task<IActionResult> OnGet(int? id){if(id.HasValue){var t=await db.Tramites.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id);if(t==null)return NotFound();Input=t;}await Load();return Page();}
    public async Task<IActionResult> OnPostAsync()
    {
        // Los campos retirados del formulario se conservan solo como datos históricos.
        var previous=Input.Id==0?null:await db.Tramites.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==Input.Id);
        Input.Assignment=previous?.Assignment;
        Input.Validation=previous?.Validation;
        Input.ValidationDate=previous?.ValidationDate;
        foreach(var field in new[]{"Assignment","Validation","ValidationDate","Conclusion"})ModelState.Remove("Input."+field);
        ModelState.Remove("Input.Type");ModelState.Remove("Input.Level");ModelState.Remove("Input.Status");
        if(!ModelState.IsValid){await Load();return Page();}
        try{var id=await service.Save(Input,Actor,ConfirmDates);Message("Expediente guardado.");return RedirectToPage("Detail",new{id});}catch(Exception e)when(e is DomainException or DbUpdateException){await Load();return Fail(e);}
    }
}
