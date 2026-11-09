using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace SCST.Infrastructure;
public class DesignFactory : IDesignTimeDbContextFactory<Database>
{
    public Database CreateDbContext(string[] args)=>new(new DbContextOptionsBuilder<Database>().UseSqlite("Data Source=scst-design.db").Options);
}
