using Microsoft.EntityFrameworkCore;
using SCST.Core;
using SCST.Infrastructure;
using System.Runtime.Versioning;
namespace SCST.Web;
[SupportedOSPlatform("windows")]
public static class Maintenance
{
    public static string? Recovery { get; private set; }
    public static int? ActorId { get; private set; }
    private static string Secret(string prompt)
    {
        Console.Write(prompt);var value="";ConsoleKeyInfo key;
        while((key=Console.ReadKey(true)).Key!=ConsoleKey.Enter)
        {
            if(key.Key==ConsoleKey.Backspace){if(value.Length>0)value=value[..^1];}
            else if(!char.IsControl(key.KeyChar))value+=key.KeyChar;
        }
        Console.WriteLine();return value;
    }
    public static async Task Authenticate(Database db,BackupService backup,DataPaths paths,string command)
    {
        _=backup;string? temp=null;Database authDb=db;
        if(command=="restore")Recovery=Secret("Clave de recuperación externa (vacío: clave local): ");
        try
        {
            if(!File.Exists(paths.DatabaseFile))
            {
                if(command!="restore"||string.IsNullOrWhiteSpace(Recovery))throw new DomainException("Se necesita un respaldo y su clave externa para recuperar en otro equipo.");
                var file=Environment.GetEnvironmentVariable("SCST_BACKUP")??throw new DomainException("Falta SCST_BACKUP.");
                temp=Path.Combine(paths.Temp,Guid.NewGuid()+".db");File.WriteAllBytes(temp,BackupService.Decrypt(File.ReadAllBytes(file),Convert.FromBase64String(Recovery)));BackupService.Integrity(temp);
                authDb=new(new DbContextOptionsBuilder<Database>().UseSqlite($"Data Source={temp};Pooling=False").Options);
            }
            Console.Write("Usuario administrador SCST: ");var username=Console.ReadLine()??"";var password=Secret("Contraseña: ");
            var user=await new UserService(authDb).Login(username,password);
            if(user==null||user.Role!=Role.Administrador)throw new DomainException("Credenciales administrativas incorrectas o cuenta bloqueada.");
            ActorId=user.Id;
        }
        finally{if(authDb!=db)await authDb.DisposeAsync();if(temp!=null&&File.Exists(temp))File.Delete(temp);}
    }
}
