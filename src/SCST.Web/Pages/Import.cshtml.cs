using Microsoft.AspNetCore.Mvc;
using SCST.Core;
using SCST.Infrastructure;
using System.Text.Json;
namespace SCST.Web.Pages;
public class ImportModel(ExcelService excel,DataPaths paths):BasePage
{
    [BindProperty]public IFormFile? Upload{get;set;}
    [BindProperty]public string? Token{get;set;}
    public List<ImportRow> Rows{get;set;}=[];
    public int? Imported{get;set;}
    private string PreviewPath(){if(!Guid.TryParseExact(Token,"N",out var id))throw new DomainException("Vista previa inválida o vencida.");return Path.Combine(paths.Temp,$"import_{Actor.Id}_{id:N}.json");}
    public void OnGet(){}
    public IActionResult OnGetTemplate()=>File(ExcelService.Export([]),"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet","Plantilla_SCST.xlsx");
    public async Task<IActionResult> OnPostAsync()
    {
        try{if(Upload==null||Upload.Length>20*1024*1024||!Path.GetExtension(Upload.FileName).Equals(".xlsx",StringComparison.OrdinalIgnoreCase))throw new DomainException("Seleccione un archivo .xlsx de hasta 20 MB.");using var stream=Upload.OpenReadStream();Rows=await excel.Preview(stream);Token=Guid.NewGuid().ToString("N");await System.IO.File.WriteAllTextAsync(PreviewPath(),JsonSerializer.Serialize(Rows));foreach(var old in Directory.GetFiles(paths.Temp,"import_*.json").Where(f=>System.IO.File.GetCreationTimeUtc(f)<DateTime.UtcNow.AddHours(-24)))System.IO.File.Delete(old);return Page();}catch(Exception e)when(e is DomainException or InvalidDataException or ArgumentException){return Fail(e);}
    }
    private async Task Read(){var file=PreviewPath();if(!System.IO.File.Exists(file)||System.IO.File.GetCreationTimeUtc(file)<DateTime.UtcNow.AddHours(-24))throw new DomainException("Vista previa vencida. Cargue el archivo nuevamente.");Rows=JsonSerializer.Deserialize<List<ImportRow>>(await System.IO.File.ReadAllTextAsync(file))!;}
    public async Task<IActionResult> OnPostConfirmAsync()
    {
        try{await Read();foreach(var row in Rows){var prefix=$"names_{row.Row}_";if(Request.Form.TryGetValue(prefix+"n",out var n)){row.Data.Names=n.ToString();row.Data.FirstSurname=Request.Form[prefix+"p"].ToString();row.Data.SecondSurname=Request.Form[prefix+"m"].ToString();}row.Errors.RemoveAll(e=>e.StartsWith("Capture folio"));try{row.Data.Normalize();}catch(DomainException e){row.Errors.Add(e.Message);}}Imported=await excel.Import(Rows,Actor);await System.IO.File.WriteAllTextAsync(PreviewPath(),JsonSerializer.Serialize(Rows));return Page();}catch(DomainException e){return Fail(e);}
    }
    public async Task<IActionResult> OnPostErrorsAsync(){try{await Read();return File(ExcelService.Errors(Rows),"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet","Errores_Importacion.xlsx");}catch(DomainException e){return Fail(e);}}
}
