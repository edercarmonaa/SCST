using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCST.Core;
using SCST.Infrastructure;
namespace SCST.Web.Pages.Admin;
public class SettingsModel(Database db,DataPaths paths,TramiteService service):BasePage
{
    [BindProperty]public Settings Input{get;set;}=new();
    public string Storage=>paths.Root;
    public async Task OnGet()=>Input=await db.Settings.AsNoTracking().SingleAsync();
    public async Task<IActionResult> OnPostAsync(){if(string.IsNullOrWhiteSpace(Input.Dependency)||string.IsNullOrWhiteSpace(Input.Unit)||string.IsNullOrWhiteSpace(Input.Area)||Input.Year<1900||Input.Year>9999)return Fail(new DomainException("Complete los datos institucionales y un ejercicio válido."));var s=await db.Settings.SingleAsync();s.Dependency=Input.Dependency;s.Unit=Input.Unit;s.Area=Input.Area;s.Year=Input.Year;service.Audit(Actor,"Configuración","Settings",1);await db.SaveChangesAsync();Message("Configuración guardada.");return RedirectToPage();}
}
