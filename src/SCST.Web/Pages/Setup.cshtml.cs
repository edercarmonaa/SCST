using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCST.Core;
using SCST.Infrastructure;
using System.ComponentModel.DataAnnotations;
namespace SCST.Web.Pages;
public class SetupModel(Database db,UserService users,BackupService backup,DataPaths paths):BasePage
{
    [BindProperty,Required]public string Name{get;set;}="";
    [BindProperty,Required]public string Username{get;set;}="";
    [BindProperty,Required]public string Password{get;set;}="";
    [BindProperty]public Settings Input{get;set;}=new();
    [BindProperty]public bool SavedRecovery{get;set;}
    [BindProperty]public string? Recovery{get;set;}
    public string Storage=>paths.Root;
    public async Task<IActionResult> OnGet(){if(await db.Settings.AnyAsync(x=>x.Complete))return RedirectToPage("/Account/Login");return Page();}
    public async Task<IActionResult> OnPostKeyAsync()
    {
        if(await db.Users.AnyAsync())return BadRequest();
        ModelState.Clear();
        try{Recovery=backup.InitializeKey();}catch(DomainException e){ModelState.AddModelError("",e.Message);}
        return Page();
    }
    public Task<IActionResult> OnPostCompleteAsync()=>OnPostAsync();
    public async Task<IActionResult> OnPostAsync()
    {
        if(await db.Settings.AnyAsync(x=>x.Complete))return BadRequest();
        if(!SavedRecovery || string.IsNullOrWhiteSpace(Recovery)||string.IsNullOrWhiteSpace(Input.Dependency)||string.IsNullOrWhiteSpace(Input.Unit)||string.IsNullOrWhiteSpace(Input.Area)||Input.Year<1900||Input.Year>9999)ModelState.AddModelError("","Complete la institución, ejercicio y confirmación de recuperación.");
        if(!ModelState.IsValid)return Page();
        try
        {
            // Verify recovery material against a fresh consistent backup before completing setup.
            var file=backup.Create();var bytes=BackupService.Decrypt(System.IO.File.ReadAllBytes(file),Convert.FromBase64String(Recovery!));if(bytes.Length==0)throw new DomainException("Clave de recuperación incorrecta.");
            await using var tx=await db.Database.BeginTransactionAsync();await users.Create(Name,Username,Password,Role.Administrador,null,true);var s=await db.Settings.SingleAsync();s.Dependency=Input.Dependency;s.Unit=Input.Unit;s.Area=Input.Area;s.Year=Input.Year;s.Complete=true;await db.SaveChangesAsync();await tx.CommitAsync();
            backup.Create(true);s.LastBackupUtc=DateTime.UtcNow;await db.SaveChangesAsync();return RedirectToPage("/Account/Login");
        }catch(Exception e)when(e is DomainException or FormatException or System.Security.Cryptography.CryptographicException){ModelState.AddModelError("",e is DomainException?e.Message:"La clave de recuperación no corresponde a este equipo.");return Page();}
    }
}
