using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCST.Core;
using SCST.Infrastructure;
namespace SCST.Web.Pages.Admin;
public class AuditModel(Database db):BasePage
{
    [BindProperty(SupportsGet=true)]public int? UserId{get;set;}
    [BindProperty(SupportsGet=true)]public string? Action{get;set;}
    [BindProperty(SupportsGet=true)]public string? Folio{get;set;}
    [BindProperty(SupportsGet=true)]public DateOnly? Date{get;set;}
    [BindProperty(SupportsGet=true)]public int P{get;set;}=1;
    public List<Audit> Items{get;set;}=[];public List<User> Users{get;set;}=[];public int Count{get;set;}
    public async Task OnGet(){var q=db.Audits.AsNoTracking();if(UserId.HasValue)q=q.Where(x=>x.UserId==UserId);if(!string.IsNullOrWhiteSpace(Action))q=q.Where(x=>x.Action.Contains(Action));if(!string.IsNullOrWhiteSpace(Folio))q=q.Where(x=>x.Folio!=null&&x.Folio.Contains(Folio));if(Date.HasValue){var start=Date.Value.ToDateTime(TimeOnly.MinValue).ToUniversalTime();var end=start.AddDays(1);q=q.Where(x=>x.TimestampUtc>=start&&x.TimestampUtc<end);}P=Math.Max(1,P);Count=await q.CountAsync();Items=await q.OrderByDescending(x=>x.Id).Skip((P-1)*25).Take(25).ToListAsync();Users=await db.Users.AsNoTracking().ToListAsync();}
}
