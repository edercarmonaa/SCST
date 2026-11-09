using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCST.Infrastructure;
namespace SCST.Web.Pages.Admin;
public class BackupsModel(Database db,BackupService backup,DataPaths paths,TramiteService service):BasePage
{
    public List<FileInfo> Files{get;set;}=[];
    public DateTime? Last{get;set;}
    public async Task OnGet(){Files=new DirectoryInfo(paths.Backups).GetFiles("*.scst").OrderByDescending(x=>x.CreationTimeUtc).ToList();Last=(await db.Settings.SingleAsync()).LastBackupUtc;}
    public async Task<IActionResult> OnPostAsync(){try{backup.Create();var s=await db.Settings.SingleAsync();s.LastBackupUtc=DateTime.UtcNow;service.Audit(Actor,"Respaldo manual","Settings",1);await db.SaveChangesAsync();Message("Respaldo consistente, cifrado y verificado.");return RedirectToPage();}catch(Exception e)when(e is IOException or System.Security.Cryptography.CryptographicException){await OnGet();return Fail(new SCST.Core.DomainException("No fue posible crear el respaldo. Revise espacio y claves de recuperación."));}}
    public async Task<IActionResult> OnGetDownload(string name){if(Path.GetFileName(name)!=name||!name.EndsWith(".scst",StringComparison.Ordinal))return BadRequest();var file=Path.Combine(paths.Backups,name);if(!System.IO.File.Exists(file))return NotFound();service.Audit(Actor,"Exportar respaldo","Settings",1);await db.SaveChangesAsync();return PhysicalFile(file,"application/octet-stream",name);}
}
