using System.Diagnostics;
namespace SCST.Web.Pages;
public class ErrorModel:BasePage { public string RequestId{get;set;}="";public void OnGet()=>RequestId=Activity.Current?.Id??HttpContext.TraceIdentifier;public void OnPost()=>OnGet(); }
