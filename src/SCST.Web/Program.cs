using System.Runtime.Versioning;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using SCST.Core;
using SCST.Infrastructure;
using SCST.Web;

[assembly: SupportedOSPlatform("windows")]

var builder=WebApplication.CreateBuilder(args);
var configuredRoot=builder.Configuration["SCST_DATA"];
var root=string.IsNullOrWhiteSpace(configuredRoot)?Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"SCSTData"):configuredRoot;
var webRoot=Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath,"wwwroot"));
var absoluteRoot=Path.GetFullPath(root);
if(absoluteRoot.Equals(webRoot,StringComparison.OrdinalIgnoreCase)||absoluteRoot.StartsWith(webRoot+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("El almacenamiento privado debe quedar fuera del directorio público de la aplicación.");
var paths=new DataPaths(root);
// An OS file lock covers both server and offline maintenance operations.
using var instance=new FileStream(Path.Combine(paths.Root,"instance.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
builder.Services.AddSingleton(paths);
builder.Services.AddDbContext<Database>(o=>o.UseSqlite(new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder{DataSource=paths.DatabaseFile,ForeignKeys=true,DefaultTimeout=30}.ToString()));
builder.Services.AddScoped<TramiteService>();builder.Services.AddScoped<UserService>();builder.Services.AddScoped<ExcelService>();builder.Services.AddSingleton<BackupService>();
builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(paths.Keys)).SetApplicationName("SCST").ProtectKeysWithDpapi();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(o=>{o.LoginPath="/Account/Login";o.AccessDeniedPath="/Denied";o.ExpireTimeSpan=TimeSpan.FromMinutes(30);o.SlidingExpiration=true;o.Cookie.HttpOnly=true;o.Cookie.SameSite=SameSiteMode.Strict;o.Cookie.SecurePolicy=CookieSecurePolicy.SameAsRequest;o.Events.OnValidatePrincipal=async c=>{var db=c.HttpContext.RequestServices.GetRequiredService<Database>();var id=int.TryParse(c.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,out var v)?v:0;var u=await db.Users.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id);if(u==null||!u.Active||u.SecurityStamp!=c.Principal?.FindFirst("stamp")?.Value){c.RejectPrincipal();await c.HttpContext.SignOutAsync();}};});
builder.Services.AddAuthorization(o=>{o.AddPolicy("Write",p=>p.RequireRole("Administrador","Capturista"));o.AddPolicy("Admin",p=>p.RequireRole("Administrador"));});
builder.Services.AddRazorPages(o=>{o.Conventions.AuthorizeFolder("/");o.Conventions.AllowAnonymousToPage("/Account/Login");o.Conventions.AllowAnonymousToPage("/Setup");o.Conventions.AllowAnonymousToPage("/Error");o.Conventions.AuthorizeFolder("/Admin","Admin");o.Conventions.AuthorizePage("/Trash","Write");o.Conventions.AuthorizePage("/Import","Write");o.Conventions.AuthorizePage("/Tramites/Edit","Write");});
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o=>{o.MultipartBodyLengthLimit=20*1024*1024;o.ValueCountLimit=70000;});
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddFilter<Microsoft.Extensions.Logging.Console.ConsoleLoggerProvider>("Microsoft",LogLevel.None);
builder.Logging.AddProvider(new FileLoggerProvider(Path.Combine(paths.Root,"Logs")));
var configuredPort=builder.Configuration["SCST_PORT"];
var port=5186;
if(!string.IsNullOrWhiteSpace(configuredPort)&&(!int.TryParse(configuredPort,out port)||port<1||port>65535))throw new InvalidOperationException("SCST_PORT debe ser un puerto válido entre 1 y 65535.");
builder.WebHost.UseUrls($"http://127.0.0.1:{port}");
var app=builder.Build();
using(var scope=app.Services.CreateScope())
{
    var db=scope.ServiceProvider.GetRequiredService<Database>();var backup=scope.ServiceProvider.GetRequiredService<BackupService>();
    var command=args.FirstOrDefault(x=>x.StartsWith("--maintenance="))?.Split('=')[1];
    if(command is "restore" or "backup") await Maintenance.Authenticate(db,backup,paths,command);
    if(command=="restore"){var file=Environment.GetEnvironmentVariable("SCST_BACKUP")??throw new InvalidOperationException("Falta SCST_BACKUP.");backup.Restore(file,Maintenance.Recovery);db.ChangeTracker.Clear();db.Audits.Add(new(){UserId=await db.Users.AnyAsync(x=>x.Id==Maintenance.ActorId)?Maintenance.ActorId:null,Action="Restauración de base de datos",Entity="Settings",EntityId="1"});await db.SaveChangesAsync();Console.WriteLine("Restauración verificada.");return;}
    if(command=="backup"){Console.WriteLine(backup.Create());var settings=await db.Settings.SingleAsync();settings.LastBackupUtc=DateTime.UtcNow;db.Audits.Add(new(){UserId=Maintenance.ActorId,Action="Respaldo manual auxiliar",Entity="Settings",EntityId="1"});await db.SaveChangesAsync();return;}
    if(command=="migrate" || !File.Exists(paths.DatabaseFile)){await db.Database.MigrateAsync();await db.SeedAsync();if(command=="migrate")return;}
    if((await db.Database.GetPendingMigrationsAsync()).Any())throw new InvalidOperationException("Hay migraciones pendientes. Ejecute setup.ps1 antes de iniciar.");
    BackupService.Integrity(paths.DatabaseFile);
    if(await db.Settings.AnyAsync(x=>x.Complete))
    {
        try{if(!Directory.GetFiles(paths.Backups,$"Auto_{DateTime.Now:yyyyMMdd}_*.scst").Any()){backup.Create(true);var settings=await db.Settings.SingleAsync();settings.LastBackupUtc=DateTime.UtcNow;await db.SaveChangesAsync();}}
        catch(Exception e){app.Logger.LogError("No fue posible crear el respaldo automático. Revise administración. Tipo: {ExceptionType}",e.GetType().Name);}
    }
}
app.UseExceptionHandler("/Error");
app.Use(async(c,next)=>{if(c.Request.Host.Host is not "127.0.0.1" and not "localhost"){c.Response.StatusCode=400;return;}c.Response.Headers.ContentSecurityPolicy="default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; frame-src 'self'; object-src 'none'; base-uri 'self'; frame-ancestors 'self'; form-action 'self'";c.Response.Headers.XContentTypeOptions="nosniff";c.Response.Headers["Referrer-Policy"]="no-referrer";c.Response.Headers.XFrameOptions="SAMEORIGIN";c.Response.Headers.CacheControl="no-store";await next();});
app.UseStaticFiles();app.UseRouting();app.UseAuthentication();app.UseAuthorization();
app.Use(async(c,next)=>{var p=c.Request.Path;if(!p.StartsWithSegments("/Setup")&&!p.StartsWithSegments("/Error")){var db=c.RequestServices.GetRequiredService<Database>();if(!await db.Settings.AnyAsync(x=>x.Complete)){c.Response.Redirect("/Setup");return;}if(c.User.Identity?.IsAuthenticated==true&&!p.StartsWithSegments("/Account")){var id=int.Parse(c.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);if(await db.Users.AnyAsync(x=>x.Id==id&&x.MustChangePassword)){c.Response.Redirect("/Account/Profile");return;}}}await next();});
var eventHash=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(paths.Root.ToUpperInvariant())));
using var shutdownEvent=new EventWaitHandle(false,EventResetMode.AutoReset,"Local\\SCST_"+eventHash);
var shutdownRegistration=ThreadPool.RegisterWaitForSingleObject(shutdownEvent,(_,_)=>app.Lifetime.StopApplication(),null,Timeout.Infinite,true);
app.Lifetime.ApplicationStopped.Register(()=>shutdownRegistration.Unregister(null));
app.MapRazorPages();app.Logger.LogInformation("SCST iniciado en loopback.");await app.RunAsync();
