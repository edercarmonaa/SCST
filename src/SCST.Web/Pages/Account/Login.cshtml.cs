using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using SCST.Infrastructure;
using System.Security.Claims;
namespace SCST.Web.Pages.Account;
public class LoginModel(UserService users):BasePage
{
    [BindProperty]public string Username{get;set;}="";
    [BindProperty]public string Password{get;set;}="";
    public void OnGet(){}
    public async Task<IActionResult> OnPostAsync(string? returnUrl=null)
    {
        var u=await users.Login(Username,Password);if(u==null){ModelState.AddModelError("","Credenciales incorrectas o cuenta bloqueada. Tras cinco intentos, espere 15 minutos.");return Page();}
        var identity=new ClaimsIdentity(new[]{new Claim(ClaimTypes.NameIdentifier,u.Id.ToString()),new Claim(ClaimTypes.Name,u.Name),new Claim(ClaimTypes.Role,u.Role.ToString()),new Claim("stamp",u.SecurityStamp)},CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,new ClaimsPrincipal(identity));return LocalRedirect(u.MustChangePassword?"/Account/Profile":Url.IsLocalUrl(returnUrl)?returnUrl!:"/");
    }
}
