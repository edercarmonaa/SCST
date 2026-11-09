using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
namespace SCST.Web.Pages.Account;
public class LogoutModel:BasePage {public async Task<IActionResult> OnPostAsync(){await HttpContext.SignOutAsync();return RedirectToPage("Login");}public IActionResult OnGet()=>RedirectToPage("/Index");}
