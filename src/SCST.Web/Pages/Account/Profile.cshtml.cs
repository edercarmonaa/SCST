using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCST.Infrastructure;
namespace SCST.Web.Pages.Account;
[Authorize]
public class ProfileModel(Database db,UserService service):BasePage
{
    [BindProperty]public string Name{get;set;}="";
    [BindProperty]public string CurrentPassword{get;set;}="";
    [BindProperty]public string NewPassword{get;set;}="";
    public bool RequiredChange{get;set;}
    public async Task OnGet(){var u=await db.Users.SingleAsync(x=>x.Id==Actor.Id);Name=u.Name;RequiredChange=u.MustChangePassword;}
    public async Task<IActionResult> OnPostAsync(){try{await service.ChangePassword(Actor.Id,CurrentPassword,NewPassword);await HttpContext.SignOutAsync();Message("Contraseña actualizada. Inicie sesión nuevamente.");return RedirectToPage("Login");}catch(SCST.Core.DomainException e){return Fail(e);}}
    public async Task<IActionResult> OnPostNameAsync(){if(string.IsNullOrWhiteSpace(Name))return BadRequest();var u=await db.Users.SingleAsync(x=>x.Id==Actor.Id);u.Name=Name.Trim();await db.SaveChangesAsync();Message("Nombre actualizado. Se mostrará en su próximo inicio de sesión.");return RedirectToPage();}
}
