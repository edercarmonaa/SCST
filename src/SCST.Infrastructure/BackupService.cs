using System.Runtime.Versioning;
using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SCST.Core;

namespace SCST.Infrastructure;
[SupportedOSPlatform("windows")]
public class BackupService(DataPaths paths)
{
    private string KeyFile=>Path.Combine(paths.Keys,"backup.key");
    public string InitializeKey()
    {
        if(File.Exists(KeyFile)) throw new DomainException("La clave ya está configurada. Utilice la recuperación guardada.");
        var key=RandomNumberGenerator.GetBytes(32);
        File.WriteAllBytes(KeyFile,ProtectedData.Protect(key,null,DataProtectionScope.CurrentUser));
        return Convert.ToBase64String(key);
    }
    private byte[] Key(string? recovery) => string.IsNullOrWhiteSpace(recovery)?ProtectedData.Unprotect(File.ReadAllBytes(KeyFile),null,DataProtectionScope.CurrentUser):Convert.FromBase64String(recovery.Trim());
    public static byte[] Encrypt(byte[] data,byte[] key)
    {
        var nonce=RandomNumberGenerator.GetBytes(12);var tag=new byte[16];var cipher=new byte[data.Length];
        using var aes=new AesGcm(key,16);aes.Encrypt(nonce,data,cipher,tag,"SCST01"u8);
        return "SCST01"u8.ToArray().Concat(nonce).Concat(tag).Concat(cipher).ToArray();
    }
    public static byte[] Decrypt(byte[] envelope,byte[] key)
    {
        if(envelope.Length<34 || !envelope.AsSpan(0,6).SequenceEqual("SCST01"u8))throw new DomainException("Formato de respaldo inválido.");
        var result=new byte[envelope.Length-34];using var aes=new AesGcm(key,16);aes.Decrypt(envelope.AsSpan(6,12),envelope.AsSpan(34),envelope.AsSpan(18,16),result,"SCST01"u8);return result;
    }
    public static void Integrity(string file)
    {
        using var c=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=file,Mode=SqliteOpenMode.ReadOnly,Pooling=false}.ToString());c.Open();using var cmd=c.CreateCommand();cmd.CommandText="PRAGMA integrity_check";if((string?)cmd.ExecuteScalar()!="ok")throw new DomainException("La verificación de integridad SQLite falló.");
        cmd.CommandText="PRAGMA foreign_key_check";using var r=cmd.ExecuteReader();if(r.Read())throw new DomainException("El respaldo contiene relaciones inconsistentes.");
    }
    public string Create(bool automatic=false)
    {
        var temp=Path.Combine(paths.Temp,Guid.NewGuid()+".db");
        try
        {
            using(var source=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=paths.DatabaseFile,Pooling=false}.ToString()))
            using(var target=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=temp,Pooling=false}.ToString())) {source.Open();target.Open();source.BackupDatabase(target);}
            Integrity(temp);var data=File.ReadAllBytes(temp);var key=Key(null);var encrypted=Encrypt(data,key);
            if(!data.AsSpan().SequenceEqual(Decrypt(encrypted,key)))throw new DomainException("La verificación del cifrado falló.");
            var file=Path.Combine(paths.Backups,$"{(automatic?"Auto":"Manual")}_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.scst");
            File.WriteAllBytes(file+".tmp",encrypted);File.Move(file+".tmp",file);
            if(automatic) foreach(var old in Directory.GetFiles(paths.Backups,"Auto_*.scst").OrderByDescending(File.GetCreationTimeUtc).Skip(30))File.Delete(old);
            return file;
        } finally {if(File.Exists(temp))File.Delete(temp);}
    }
    // Called only by the offline maintenance process after the launcher releases the instance lock.
    public void Restore(string file,string? recovery)
    {
        var temp=Path.Combine(paths.Temp,Guid.NewGuid()+".db");
        try
        {
            File.WriteAllBytes(temp,Decrypt(File.ReadAllBytes(file),Key(recovery)));Integrity(temp);
            using(var c=new SqliteConnection($"Data Source={temp};Pooling=False")){c.Open();using var cmd=c.CreateCommand();cmd.CommandText="SELECT COUNT(*) FROM Settings WHERE Complete = 1";if(Convert.ToInt32(cmd.ExecuteScalar())!=1)throw new DomainException("El respaldo no contiene una configuración SCST completa.");}
            if(File.Exists(paths.DatabaseFile))Create();
            SqliteConnection.ClearAllPools();
            if(File.Exists(paths.DatabaseFile))
            {
                using var old=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=paths.DatabaseFile,Pooling=false}.ToString());old.Open();using var checkpoint=old.CreateCommand();checkpoint.CommandText="PRAGMA wal_checkpoint(TRUNCATE)";using var result=checkpoint.ExecuteReader();if(result.Read()&&result.GetInt32(0)!=0)throw new DomainException("La base sigue en uso. Detenga el servidor antes de restaurar.");
            }
            // File.Replace is atomic on the same volume. Keep a rollback copy until success.
            var rollback=paths.DatabaseFile+".rollback";
            if(File.Exists(paths.DatabaseFile))File.Replace(temp,paths.DatabaseFile,rollback,true);else File.Move(temp,paths.DatabaseFile);
            try {Integrity(paths.DatabaseFile);}catch {if(File.Exists(rollback))File.Replace(rollback,paths.DatabaseFile,null);throw;}
            if(File.Exists(rollback))File.Delete(rollback);
            if(!string.IsNullOrWhiteSpace(recovery))File.WriteAllBytes(KeyFile,ProtectedData.Protect(Key(recovery),null,DataProtectionScope.CurrentUser));
        } finally {if(File.Exists(temp))File.Delete(temp);}
    }
}
