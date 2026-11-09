using System.Runtime.Versioning;
using ClosedXML.Excel;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SCST.Core;
using SCST.Infrastructure;
[assembly: SupportedOSPlatform("windows")]
namespace SCST.Tests;
public class BusinessTests : IAsyncLifetime
{
    private readonly SqliteConnection connection=new("Data Source=:memory:");
    private Database db=null!;private TramiteService service=null!;private Actor actor=null!;
    public async Task InitializeAsync(){await connection.OpenAsync();db=new(new DbContextOptionsBuilder<Database>().UseSqlite(connection).Options);await db.Database.EnsureCreatedAsync();await db.SeedAsync();db.Catalogs.Add(new(){Kind=CatalogKind.Validador,Name="VALIDADOR FICTICIO"});await db.SaveChangesAsync();var id=await new UserService(db).Create("Administrador de prueba","admin","frase de prueba segura",Role.Administrador,null,true);actor=new(id,Role.Administrador);service=new(db);}
    public async Task DisposeAsync(){await db.DisposeAsync();await connection.DisposeAsync();}
    private Tramite Valid(string folio="0001")=>new(){Folio=folio,Names="José ñandú",FirstSurname="Pérez",Reception=new(2026,1,1),TypeId=db.Catalogs.First(x=>x.Kind==CatalogKind.TipoTramite).Id,LevelId=db.Catalogs.First(x=>x.Kind==CatalogKind.NivelEducativo).Id};
    private static User InjectedUser()=>new(){Name="Usuario ficticio no autorizado",Username="injected.example",NormalizedUsername="INJECTED.EXAMPLE",Role=Role.Administrador,PasswordHash="synthetic-invalid-hash"};
    [Fact]public async Task TramiteRejectsInjectedGraphsAndServerMetadata()
    {
        var t=Valid();t.Deleted=true;t.CreatedUtc=new(1970,1,1);t.OriginalName="Texto no autorizado";
        t.FollowUps.Add(new(){Activity="Actividad inyectada",Date=t.Reception,User=InjectedUser()});
        await service.Save(t,actor with{Role=Role.Capturista});
        var stored=await db.Tramites.SingleAsync();Assert.False(stored.Deleted);Assert.True(stored.CreatedUtc.Year>1970);Assert.Null(stored.OriginalName);
        Assert.Single(await db.Users.ToListAsync());Assert.Empty(await db.FollowUps.ToListAsync());
    }
    [Fact]public async Task FollowUpAndReentryIgnoreInjectedPrincipals()
    {
        var t=Valid();await service.Save(t,actor);
        var f=new FollowUp{TramiteId=t.Id,Date=t.Reception,Activity="Actividad ficticia",User=InjectedUser(),UserId=999,Tramite=Valid("INJECTED"),Deleted=true};
        await service.SaveFollowUp(f,actor with{Role=Role.Capturista});
        await service.SaveReentry(new(){TramiteId=t.Id,Date=t.Reception,Reason="Motivo ficticio",Tramite=Valid("INJECTED-REENTRY"),Deleted=true},actor);
        Assert.Single(await db.Users.ToListAsync());Assert.Single(await db.Tramites.ToListAsync());Assert.Equal(actor.Id,(await db.FollowUps.SingleAsync()).UserId);Assert.Single(await db.Reentries.ToListAsync());
    }
    [Fact]public async Task OficioIgnoresInjectedExpedientes()
    {
        var o=new Oficio{Number="OFICIO-EJEMPLO",Year=2020,Deleted=true};o.Tramites.Add(new(){Tramite=Valid("INJECTED-LETTER")});
        await service.SaveOficio(o,actor with{Role=Role.Capturista});Assert.Single(await db.Oficios.ToListAsync());Assert.Empty(await db.Tramites.ToListAsync());Assert.Empty(await db.Links.ToListAsync());
    }
    [Fact]public async Task CreatesNormalizesAndPreservesFolio(){var t=Valid();await service.Save(t,actor);Assert.Equal("0001",t.Folio);Assert.Equal("JOSÉ ÑANDÚ",t.Names);Assert.Equal(2026,t.Year);Assert.Single(await db.Audits.ToListAsync());}
    [Fact]public async Task ConclusionFollowsWindowResponseOnCreateEditAndClear()
    {
        var t=Valid();t.WindowResponse=new(2026,1,12);t.Conclusion=new(2026,1,20);
        await service.Save(t,actor);
        Assert.Equal(t.WindowResponse,(await db.Tramites.AsNoTracking().SingleAsync()).Conclusion);
        db.ChangeTracker.Clear();t=await db.Tramites.AsNoTracking().SingleAsync();t.WindowResponse=new(2026,1,15);
        await service.Save(t,actor);
        Assert.Equal(t.WindowResponse,(await db.Tramites.AsNoTracking().SingleAsync()).Conclusion);
        db.ChangeTracker.Clear();t=await db.Tramites.AsNoTracking().SingleAsync();t.WindowResponse=null;
        await service.Save(t,actor);
        Assert.Null((await db.Tramites.AsNoTracking().SingleAsync()).Conclusion);
    }
    [Fact]public async Task SearchesUnicodeFoliosWithoutCaseSensitivity()
    {
        var cs=$"Data Source={Guid.NewGuid()};Mode=Memory;Cache=Shared";await using var keeper=new SqliteConnection(cs);await keeper.OpenAsync();
        await using var local=new Database(new DbContextOptionsBuilder<Database>().UseSqlite(cs).Options);await local.Database.EnsureCreatedAsync();await local.SeedAsync();var localId=await new UserService(local).Create("Administrador de prueba","admin","frase de prueba segura",Role.Administrador,null,true);var t=Valid("ñá-0001");t.TypeId=(await local.Catalogs.FirstAsync(x=>x.Kind==CatalogKind.TipoTramite)).Id;t.LevelId=(await local.Catalogs.FirstAsync(x=>x.Kind==CatalogKind.NivelEducativo)).Id;var svc=new TramiteService(local);await svc.Save(t,new(localId,Role.Administrador));Assert.Single(await svc.Query(new(Text:"ÑÁ-0001")).ToListAsync());
    }
    [Theory][InlineData("folio")][InlineData("name")][InlineData("surname")][InlineData("date")][InlineData("type")][InlineData("level")]
    public async Task RejectsMissingFields(string field){var t=Valid();switch(field){case "folio":t.Folio="";break;case "name":t.Names="";break;case "surname":t.FirstSurname="";break;case "date":t.Reception=default;break;case "type":t.TypeId=0;break;case "level":t.LevelId=0;break;}await Assert.ThrowsAsync<DomainException>(()=>service.Save(t,actor));}
    [Fact]public async Task DeletedFolioRemainsReserved(){var t=Valid();await service.Save(t,actor);await service.Toggle("Tramite",t.Id,false,actor);Assert.Empty(await db.Tramites.ToListAsync());await Assert.ThrowsAsync<DomainException>(()=>service.Save(Valid(),actor));}
    [Fact]public async Task FolioAndYearChangeKeepRelationsAndRestore(){var t=Valid();await service.Save(t,actor);var letter=new Oficio{Number="OF/01",Year=2026};await service.SaveOficio(letter,actor);await service.Link(letter.Id,[t.Id],false,actor);await service.SaveFollowUp(new(){TramiteId=t.Id,Date=new(2026,1,2),Activity="Revisión"},actor);db.ChangeTracker.Clear();var edit=await db.Tramites.AsNoTracking().SingleAsync();edit.Folio="0002";edit.Reception=new(2025,12,31);await service.Save(edit,actor);await service.Toggle("Tramite",t.Id,false,actor);Assert.Empty(await db.FollowUps.ToListAsync());await service.Toggle("Tramite",t.Id,true,actor);var restored=await service.Detail(t.Id);Assert.Equal(2025,restored!.Year);Assert.Equal("0002",restored.Folio);Assert.Single(restored.FollowUps);Assert.Single(restored.Letters);}
    [Theory][InlineData(0,Traffic.Verde)][InlineData(15,Traffic.Verde)][InlineData(16,Traffic.Amarillo)][InlineData(30,Traffic.Amarillo)][InlineData(31,Traffic.Rojo)][InlineData(900,Traffic.Rojo)]
    public void SignalBoundaries(int days,Traffic expected){var t=Valid();Assert.Equal(expected,t.Signal(t.Reception.AddDays(days),true));Assert.Null(t.Signal(t.Reception.AddDays(days),false));}
    [Theory][InlineData(Role.Consulta,false)][InlineData(Role.Capturista,true)][InlineData(Role.Administrador,true)]
    public async Task EnforcesWritePermissions(Role role,bool allowed){if(allowed)await service.Save(Valid(),actor with{Role=role});else await Assert.ThrowsAsync<DomainException>(()=>service.Save(Valid(),actor with{Role=role}));}
    [Fact]public async Task CannotAssignInactiveValidator(){var c=await db.Catalogs.FirstAsync(x=>x.Kind==CatalogKind.Validador);c.Active=false;await db.SaveChangesAsync();var t=Valid();t.ValidatorId=c.Id;await Assert.ThrowsAsync<DomainException>(()=>service.Save(t,actor));}
    [Fact]public async Task DuplicateLetterAndLinkRejected(){var t=Valid();await service.Save(t,actor);var o=new Oficio{Number="001",Year=2026};await service.SaveOficio(o,actor);await Assert.ThrowsAsync<DomainException>(()=>service.SaveOficio(new(){Number="001",Year=2026},actor));await service.SaveOficio(new(){Number="001",Year=2025},actor);await service.Link(o.Id,[t.Id],false,actor);await Assert.ThrowsAsync<DomainException>(()=>service.Link(o.Id,[t.Id],false,actor));await service.Toggle("Oficio",o.Id,false,actor);Assert.Single(await db.Links.IgnoreQueryFilters().ToListAsync());await service.Toggle("Oficio",o.Id,true,actor);Assert.Single((await service.Detail(t.Id))!.Letters);}
    [Fact]public async Task LockoutAndPasswordInvalidation(){var users=new UserService(db);for(int i=0;i<5;i++)Assert.Null(await users.Login("ADMIN","incorrecta"));Assert.Null(await users.Login("admin","frase de prueba segura"));var u=await db.Users.SingleAsync();Assert.True(u.LockedUntilUtc>DateTime.UtcNow);u.LockedUntilUtc=null;await db.SaveChangesAsync();Assert.NotNull(await users.Login("AdMiN","frase de prueba segura"));var stamp=u.SecurityStamp;await users.ChangePassword(u.Id,"frase de prueba segura","otra frase de prueba segura");Assert.NotEqual(stamp,u.SecurityStamp);Assert.NotNull(await users.Login("admin","otra frase de prueba segura"));}
    [Fact]public async Task ProtectsLastAdminAndOwnAccount(){var users=new UserService(db);await Assert.ThrowsAsync<DomainException>(()=>users.Manage(actor.Id,false,Role.Administrador,null,actor));await Assert.ThrowsAsync<DomainException>(()=>users.Manage(actor.Id,true,Role.Consulta,null,actor));}
    [Fact]public async Task RejectsConcurrentChanges(){var t=Valid();await service.Save(t,actor);db.ChangeTracker.Clear();var stale=await db.Tramites.AsNoTracking().SingleAsync();var fresh=await db.Tramites.AsNoTracking().SingleAsync();fresh.Observations="Edición 1";await service.Save(fresh,actor);await Assert.ThrowsAsync<DomainException>(()=>service.Save(stale,actor));}
    [Fact]public async Task ReentryDoesNotRestartSignal(){var t=Valid();await service.Save(t,actor);await service.SaveReentry(new(){TramiteId=t.Id,Date=new(2026,10,1),Reason="Documentación adicional"},actor);Assert.Equal(Traffic.Rojo,(await service.Detail(t.Id))!.Signal(new(2026,10,2),true));}
    [Fact]public async Task FollowUpCanEditAndRestore(){var t=Valid();await service.Save(t,actor);var f=new FollowUp{TramiteId=t.Id,Date=new(2026,1,2),Activity="Inicial"};await service.SaveFollowUp(f,actor);f.Activity="Corregida";await service.SaveFollowUp(f,actor);await service.Toggle("FollowUp",f.Id,false,actor);Assert.Empty(await db.FollowUps.ToListAsync());await service.Toggle("FollowUp",f.Id,true,actor);Assert.Equal("Corregida",(await db.FollowUps.SingleAsync()).Activity);}
    [Fact]public async Task ExcelRoundTripAndDuplicateRows(){var t=Valid();await service.Save(t,actor);var complete=(await service.Detail(t.Id))!;var bytes=ExcelService.Export([complete,complete]);using var m=new MemoryStream(bytes);var rows=await new ExcelService(db,service).Preview(m);Assert.Equal(2,rows.Count);Assert.All(rows,r=>{Assert.Equal("0001",r.Data.Folio);Assert.Contains(r.Errors,e=>e.Contains("duplicado dentro"));});}
    [Fact]public async Task ExcelPartialImportPreservesValidRows(){using var b=new XLWorkbook(new MemoryStream(ExcelService.Export([])));var s=b.Worksheet(1);void Row(int r,string folio,string type){s.Cell(r,1).Value=folio;s.Cell(r,2).Value="2026-01-01";s.Cell(r,3).Value="ANA";s.Cell(r,4).Value="PÉREZ";s.Cell(r,6).Value=type;s.Cell(r,7).Value="PRIMARIA";}Row(2,"0007","DUPLICADO");Row(3,"0008","DESCONOCIDO");using var m=new MemoryStream();b.SaveAs(m);m.Position=0;var excel=new ExcelService(db,service);var preview=await excel.Preview(m);Assert.Empty(preview[0].Errors);Assert.NotEmpty(preview[1].Errors);Assert.Equal(1,await excel.Import(preview,actor));Assert.Single(await db.Tramites.ToListAsync());}
    [Fact]public void BackupTamperingFails(){var key=System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);var encrypted=BackupService.Encrypt("datos de prueba"u8.ToArray(),key);Assert.Equal("datos de prueba"u8.ToArray(),BackupService.Decrypt(encrypted,key));encrypted[^1]^=1;Assert.Throws<System.Security.Cryptography.AuthenticationTagMismatchException>(()=>BackupService.Decrypt(encrypted,key));}
    [Fact]public async Task StatisticsHaveSixSheetsAndExactCounts(){var t=Valid();await service.Save(t,actor);using var b=new XLWorkbook(new MemoryStream(Reports.StatisticsExcel([(await service.Detail(t.Id))!])));Assert.Equal(6,b.Worksheets.Count);Assert.Equal(1,b.Worksheet("Resumen General").Cell(2,2).GetValue<int>());}
    [Fact]public async Task PdfHasContinuationAndPerExpedientePageNumbers(){var t=Valid();await service.Save(t,actor);t=(await service.Detail(t.Id))!;t.Validation=SCST.Core.ValidationResult.ConInconsistencias;t.ValidationDate=new(2026,1,2);for(int i=0;i<90;i++)t.FollowUps.Add(new(){Date=new(2026,1,2),Activity="Actividad de prueba de paginación "+i});var bytes=Reports.Covers([t],await db.Settings.SingleAsync(),null);using var pdf=PdfSharp.Pdf.IO.PdfReader.Open(new MemoryStream(bytes),PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import);Assert.True(pdf.PageCount>=3);Directory.CreateDirectory("artifacts");await File.WriteAllBytesAsync("artifacts/Caratula_prueba.pdf",bytes);}
}
