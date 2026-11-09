using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCST.Core;
using SCST.Infrastructure;
namespace SCST.Web.Pages.Admin;
public class UsersModel(Database db,UserService users,TramiteService service):BasePage
{
    public List<User> Items{get;set;}=[];
    [BindProperty]public string Name{get;set;}="";
    [BindProperty]public string Username{get;set;}="";
    [BindProperty]public string Password{get;set;}="";
    [BindProperty]public Role Role{get;set;}=Role.Capturista;
    public async Task OnGet()=>Items=await db.Users.AsNoTracking().OrderBy(x=>x.Name).ToListAsync();
    public async Task<IActionResult> OnPostAsync(){try{await using var tx=await db.Database.BeginTransactionAsync();var id=await users.Create(Name,Username,Password,Role,Actor);service.Audit(Actor,"Creación de cuenta","User",id);await db.SaveChangesAsync();await tx.CommitAsync();Message("Usuario creado con cambio obligatorio de contraseña.");return RedirectToPage();}catch(DomainException e){await OnGet();return Fail(e);}}
    public async Task<IActionResult> OnPostManageAsync(int id,bool active,Role role,string? temporary){try{await users.Manage(id,active,role,temporary,Actor);Message("Cuenta actualizada; sus sesiones anteriores se invalidaron.");return RedirectToPage();}catch(DomainException e){await OnGet();return Fail(e);}}
}
