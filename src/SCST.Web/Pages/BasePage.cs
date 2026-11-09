using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using SCST.Core;
using SCST.Infrastructure;
using System.Security.Claims;
namespace SCST.Web.Pages;
public abstract class BasePage : PageModel
{
    public Actor Actor=>new(int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),Enum.Parse<Role>(User.FindFirstValue(ClaimTypes.Role)!));
    public bool CanWrite=>User.IsInRole("Administrador")||User.IsInRole("Capturista");
    public IActionResult Fail(Exception e){ModelState.AddModelError("",e is DomainException?e.Message:"No fue posible guardar. Revise los datos e intente nuevamente.");return Page();}
    public void Message(string text)=>TempData["Message"]=text;
}
