using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SCST.Core;
using SCST.Infrastructure;
namespace SCST.Tests;
public class BackupTests
{
    [Fact]public async Task SnapshotRestoresVerifiedDatabaseAndRecoveryKey()
    {
        var root=Path.Combine(Path.GetTempPath(),"SCST-tests-"+Guid.NewGuid());var paths=new DataPaths(root);var backup=new BackupService(paths);var recovery=backup.InitializeKey();
        await using(var db=new Database(new DbContextOptionsBuilder<Database>().UseSqlite($"Data Source={paths.DatabaseFile};Pooling=False").Options))
        {
            await db.Database.MigrateAsync();await db.SeedAsync();var s=await db.Settings.SingleAsync();s.Complete=true;s.Dependency="Original de prueba";await db.SaveChangesAsync();
        }
        var file=backup.Create();
        await using(var db=new Database(new DbContextOptionsBuilder<Database>().UseSqlite($"Data Source={paths.DatabaseFile};Pooling=False").Options)){var s=await db.Settings.SingleAsync();s.Dependency="Cambiada";await db.SaveChangesAsync();}
        backup.Restore(file,recovery);
        await using(var db=new Database(new DbContextOptionsBuilder<Database>().UseSqlite($"Data Source={paths.DatabaseFile};Pooling=False").Options))Assert.Equal("Original de prueba",(await db.Settings.SingleAsync()).Dependency);
        var other=new DataPaths(Path.Combine(root,"Recovered"));new BackupService(other).Restore(file,recovery);Assert.True(File.Exists(other.DatabaseFile));Assert.True(File.Exists(new BackupService(other).Create()));
        var corrupt=File.ReadAllBytes(file);corrupt[^1]^=1;var bad=Path.Combine(paths.Temp,"corrupt.scst");File.WriteAllBytes(bad,corrupt);
        Assert.Throws<System.Security.Cryptography.AuthenticationTagMismatchException>(()=>backup.Restore(bad,recovery));BackupService.Integrity(paths.DatabaseFile);
    }
    [Fact]public async Task SyntheticWorkbookIsParsedWithoutModification()
    {
        await using var connection=new SqliteConnection("Data Source=:memory:");await connection.OpenAsync();await using var db=new Database(new DbContextOptionsBuilder<Database>().UseSqlite(connection).Options);await db.Database.EnsureCreatedAsync();await db.SeedAsync();
        using var workbook=new ClosedXML.Excel.XLWorkbook(new MemoryStream(ExcelService.Export([])));
        var sheet=workbook.Worksheet(1);
        for(int row=2;row<=4;row++)
        {
            sheet.Cell(row,1).Value=$"EJEMPLO-{row:0000}";sheet.Cell(row,2).Value="2020-01-02";
            sheet.Cell(row,3).Value="PERSONA FICTICIA";sheet.Cell(row,4).Value="APELLIDO EJEMPLO";
            sheet.Cell(row,6).Value="DUPLICADO";sheet.Cell(row,7).Value="PRIMARIA";
        }
        using var stream=new MemoryStream();workbook.SaveAs(stream);var before=stream.ToArray();stream.Position=0;
        var rows=await new ExcelService(db,new(db)).Preview(stream);
        Assert.Equal(3,rows.Count);Assert.All(rows,r=>{Assert.Empty(r.Errors);Assert.StartsWith("EJEMPLO-",r.Data.Folio);});
        Assert.Equal(before,stream.ToArray());
    }
}