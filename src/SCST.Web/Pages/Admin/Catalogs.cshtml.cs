using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCST.Core;
using SCST.Infrastructure;
namespace SCST.Web.Pages.Admin;
public class CatalogsModel(Database db,TramiteService service):BasePage
{
    public List<Catalog> Items{get;set;}=[];
    [BindProperty]public Catalog Input{get;set;}=new();
    [BindProperty]public bool ConfirmPending{get;set;}
    public async Task OnGet(int? edit){Items=await db.Catalogs.AsNoTracking().OrderBy(x=>x.Kind).ThenBy(x=>x.Name).ToListAsync();if(edit.HasValue)Input=Items.Single(x=>x.Id==edit);}
    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            if(string.IsNullOrWhiteSpace(Input.Name)||!Enum.IsDefined(Input.Kind))throw new DomainException("Nombre y tipo de catálogo obligatorios.");
            Input.Name=Input.Name.Trim().ToUpperInvariant();
            if(await db.Catalogs.AnyAsync(x=>x.Kind==Input.Kind&&x.Name==Input.Name&&x.Id!=Input.Id))throw new DomainException("Ese elemento ya existe.");
            if(Input.Id==0)db.Catalogs.Add(Input);
            else
            {
                var current=await db.Catalogs.SingleAsync(x=>x.Id==Input.Id);
                if(current.Kind!=Input.Kind)throw new DomainException("No se permite cambiar el tipo de catálogo.");
                if(current.Kind==CatalogKind.Estatus && current.Name is "PENDIENTE" or "CONCLUIDO" or "NO PROCEDENTE")
                {if(Input.Name!=current.Name||!Input.Active||Input.Pending!=(current.Name=="PENDIENTE"))throw new DomainException("Conserve activos y sin cambios los estatus base del sistema.");}
                if(current.Kind==CatalogKind.Validador&&!Input.Active&&await db.Tramites.AnyAsync(x=>x.ValidatorId==current.Id&&x.Status.Pending)&&!ConfirmPending)throw new DomainException("Este validador tiene trámites pendientes. Confirme la desactivación después de revisarlos.");
                current.Name=Input.Name;current.Active=Input.Active;current.Pending=Input.Kind==CatalogKind.Estatus&&Input.Pending;
            }
            await using var tx=await db.Database.BeginTransactionAsync();await db.SaveChangesAsync();service.Audit(Actor,"Guardar catálogo","Catalog",Input.Id);await db.SaveChangesAsync();await tx.CommitAsync();Message("Catálogo guardado. Las referencias históricas se conservaron.");return RedirectToPage();
        }catch(DomainException e){Items=await db.Catalogs.AsNoTracking().ToListAsync();return Fail(e);}
    }
}
