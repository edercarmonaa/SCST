using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCST.Core;
using SCST.Infrastructure;
namespace SCST.Web.Pages.Oficios;
public class IndexModel(Database db,TramiteService service):BasePage
{
    [BindProperty(SupportsGet=true)]public string? Text{get;set;}
    [BindProperty(SupportsGet=true)]public int P{get;set;}=1;
    [BindProperty]public Oficio Input{get;set;}=new(){Year=DateTime.Today.Year};
    public List<Oficio> Items{get;set;}=[];
    public int Count{get;set;}
    public async Task OnGet(){P=Math.Max(1,P);var q=db.Oficios.AsNoTracking();if(!string.IsNullOrWhiteSpace(Text))q=q.Where(x=>x.Number.Contains(Text));Count=await q.CountAsync();Items=await q.OrderByDescending(x=>x.Year).ThenByDescending(x=>x.Number).Skip((P-1)*25).Take(25).ToListAsync();}
    public async Task<IActionResult> OnPostAsync(){if(!CanWrite)return Forbid();try{await service.SaveOficio(Input,Actor);Message("Oficio registrado.");return RedirectToPage("Detail",new{id=Input.Id});}catch(DomainException e){await OnGet();return Fail(e);}}
}
