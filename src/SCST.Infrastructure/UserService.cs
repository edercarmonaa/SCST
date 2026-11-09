using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SCST.Core;

namespace SCST.Infrastructure;
public class UserService(Database db)
{
    private readonly PasswordHasher<User> hasher=new();
    public static void ValidatePassword(string password)
    {
        if(password.Length<12 || password.Length>512 || new[]{"passwordpassword","123456789012","contraseña123","administrador","qwerty123456"}.Contains(password.ToLowerInvariant())) throw new DomainException("Utilice una frase de contraseña de 12 a 512 caracteres que no sea común.");
    }
    public async Task<User?> Login(string username,string password)
    {
        if(password.Length>512||username.Length>64)return null;
        var u=await db.Users.SingleOrDefaultAsync(x=>x.NormalizedUsername==username.Trim().ToUpperInvariant());
        if(u==null) { hasher.VerifyHashedPassword(new(),hasher.HashPassword(new(),"constant-dummy-password"),password); return null; }
        if(!u.Active || u.LockedUntilUtc>DateTime.UtcNow) return null;
        if(hasher.VerifyHashedPassword(u,u.PasswordHash,password)==PasswordVerificationResult.Failed)
        { u.FailedAttempts++; if(u.FailedAttempts>=5){u.LockedUntilUtc=DateTime.UtcNow.AddMinutes(15);u.FailedAttempts=0;} await db.SaveChangesAsync(); return null; }
        u.FailedAttempts=0;u.LockedUntilUtc=null;await db.SaveChangesAsync();return u;
    }
    public async Task<int> Create(string name,string username,string password,Role role,Actor? actor,bool initial=false)
    {
        if(initial) {if(await db.Users.AnyAsync())throw new DomainException("La cuenta inicial ya existe.");} else Access.RequireAdmin(actor!.Role);
        ValidatePassword(password);
        if(string.IsNullOrWhiteSpace(name)||!Regex.IsMatch(username,"^[a-zA-Z0-9._-]{3,64}$")||!Enum.IsDefined(role)) throw new DomainException("Nombre obligatorio. Usuario: 3 a 64 letras, números, punto, guion o guion bajo.");
        var normalized=username.ToUpperInvariant(); if(await db.Users.AnyAsync(x=>x.NormalizedUsername==normalized)) throw new DomainException("Nombre de usuario ya utilizado.");
        var u=new User{Name=name.Trim(),Username=username,NormalizedUsername=normalized,Role=initial?Role.Administrador:role,MustChangePassword=!initial}; u.PasswordHash=hasher.HashPassword(u,password);db.Users.Add(u);await db.SaveChangesAsync();return u.Id;
    }
    public async Task ChangePassword(int id,string current,string password)
    {
        ValidatePassword(password); var u=await db.Users.SingleAsync(x=>x.Id==id);
        if(hasher.VerifyHashedPassword(u,u.PasswordHash,current)==PasswordVerificationResult.Failed) throw new DomainException("La contraseña actual no es correcta.");
        u.PasswordHash=hasher.HashPassword(u,password);u.MustChangePassword=false;u.SecurityStamp=Guid.NewGuid().ToString();await db.SaveChangesAsync();
    }
    public async Task Manage(int id,bool active,Role role,string? temporary,Actor a)
    {
        Access.RequireAdmin(a.Role); if(!Enum.IsDefined(role))throw new DomainException("Rol inválido.");
        await using var tx=await db.Database.BeginTransactionAsync(); var u=await db.Users.SingleAsync(x=>x.Id==id);
        if(!active && id==a.Id)throw new DomainException("No puede desactivar su propia cuenta.");
        if(u.Role==Role.Administrador && u.Active && (!active||role!=Role.Administrador) && await db.Users.CountAsync(x=>x.Active&&x.Role==Role.Administrador)<=1)throw new DomainException("Debe conservar al último administrador activo.");
        if(!string.IsNullOrEmpty(temporary)){ValidatePassword(temporary);u.PasswordHash=hasher.HashPassword(u,temporary);u.MustChangePassword=true;}
        u.Active=active;u.Role=role;u.SecurityStamp=Guid.NewGuid().ToString();
        db.Audits.Add(new(){UserId=a.Id,Action="Administración de cuenta",Entity="User",EntityId=id.ToString()});await db.SaveChangesAsync();await tx.CommitAsync();
    }
}
