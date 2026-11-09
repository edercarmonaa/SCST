using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCST.Core;
using SCST.Infrastructure;
namespace SCST.Web.Pages.Oficios;
public class DetailModel(Database db,TramiteService service):BasePage
{
    [BindProperty]public Oficio Input{get;set;}=null!;
    [BindProperty(SupportsGet=true)]public string? Text{get;set;}
    [BindProperty]public int[] Selected{get;set;}=[];
    public List<Tramite> Available{get;set;}=[];
    public async Task<IActionResult> OnGet(int id){var o=await db.Oficios.AsNoTracking().Include(x=>x.Tramites.Where(l=>!l.Tramite.Deleted)).ThenInclude(x=>x.Tramite).ThenInclude(x=>x.Status).AsSplitQuery().SingleOrDefaultAsync(x=>x.Id==id);if(o==null)return NotFound();Input=o;if(!string.IsNullOrWhiteSpace(Text))Available=await service.Query(new(Text:Text)).OrderBy(x=>x.Folio).Take(50).ToListAsync();return Page();}
    public async Task<IActionResult> OnPostAsync(int id){if(!CanWrite)return Forbid();try{Input.Id=id;await service.SaveOficio(Input,Actor);Message("Oficio actualizado.");return RedirectToPage(new{id});}catch(DomainException e){var result=await OnGet(id);if(result is NotFoundResult)return result;return Fail(e);}}
    public async Task<IActionResult> OnPostLinkAsync(int id,bool unlink=false){if(!CanWrite)return Forbid();try{await service.Link(id,Selected,unlink,Actor);Message(unlink?"Asociaciones retiradas.":"Expedientes vinculados.");return RedirectToPage(new{id});}catch(DomainException e){await OnGet(id);return Fail(e);}}
    public async Task<IActionResult> OnPostDeleteAsync(int id){if(!CanWrite)return Forbid();await service.Toggle("Oficio",id,false,Actor);return RedirectToPage("Index");}
}
