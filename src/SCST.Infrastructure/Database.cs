using Microsoft.EntityFrameworkCore;
using SCST.Core;

namespace SCST.Infrastructure;
public class Database(DbContextOptions<Database> options) : DbContext(options)
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)=>optionsBuilder.AddInterceptors(new UnicodeSqliteInterceptor());
    public DbSet<Tramite> Tramites => Set<Tramite>();
    public DbSet<Catalog> Catalogs => Set<Catalog>();
    public DbSet<Oficio> Oficios => Set<Oficio>();
    public DbSet<TramiteOficio> Links => Set<TramiteOficio>();
    public DbSet<FollowUp> FollowUps => Set<FollowUp>();
    public DbSet<Reentry> Reentries => Set<Reentry>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Settings> Settings => Set<Settings>();
    public DbSet<Audit> Audits => Set<Audit>();
    protected override void OnModelCreating(ModelBuilder b)
    {
        foreach(var type in new[]{typeof(Tramite),typeof(Catalog),typeof(Oficio),typeof(FollowUp),typeof(Reentry),typeof(User)})
            b.Entity(type).Property(nameof(Record.Version)).IsConcurrencyToken();
        b.Entity<Tramite>().HasIndex(x=>x.Folio).IsUnique();
        b.Entity<Tramite>().HasIndex(x=>new{x.Deleted,x.Year,x.Reception});
        b.Entity<Tramite>().Ignore(x=>x.FullName);
        b.Entity<Catalog>().HasIndex(x=>new{x.Kind,x.Name}).IsUnique();
        b.Entity<User>().HasIndex(x=>x.NormalizedUsername).IsUnique();
        b.Entity<Audit>().HasOne<User>().WithMany().HasForeignKey(x=>x.UserId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Oficio>().HasIndex(x=>new{x.Number,x.Year}).IsUnique();
        b.Entity<TramiteOficio>().HasKey(x=>new{x.TramiteId,x.OficioId});
        foreach(var fk in b.Model.GetEntityTypes().SelectMany(e=>e.GetForeignKeys())) fk.DeleteBehavior=DeleteBehavior.Restrict;
        b.Entity<Tramite>().HasQueryFilter(x=>!x.Deleted);
        b.Entity<Oficio>().HasQueryFilter(x=>!x.Deleted);
        b.Entity<TramiteOficio>().HasQueryFilter(x=>!x.Tramite.Deleted && !x.Oficio.Deleted);
        b.Entity<FollowUp>().HasQueryFilter(x=>!x.Deleted && !x.Tramite.Deleted);
        b.Entity<Reentry>().HasQueryFilter(x=>!x.Deleted && !x.Tramite.Deleted);
        b.Entity<Tramite>().HasOne(x=>x.Type).WithMany().HasForeignKey(x=>x.TypeId);
        b.Entity<Tramite>().HasOne(x=>x.Level).WithMany().HasForeignKey(x=>x.LevelId);
        b.Entity<Tramite>().HasOne(x=>x.Status).WithMany().HasForeignKey(x=>x.StatusId);
        b.Entity<Tramite>().HasOne(x=>x.Validator).WithMany().HasForeignKey(x=>x.ValidatorId);
        b.Entity<Tramite>().HasOne(x=>x.Medium).WithMany().HasForeignKey(x=>x.MediumId);
    }
    public override Task<int> SaveChangesAsync(CancellationToken ct=default)
    {
        foreach(var e in ChangeTracker.Entries<Record>().Where(e=>e.State==EntityState.Modified))
        { e.Entity.ModifiedUtc=DateTime.UtcNow; e.Entity.Version=Guid.NewGuid(); }
        return base.SaveChangesAsync(ct);
    }
    public async Task SeedAsync()
    {
        if(await Catalogs.AnyAsync()) return;
        void Add(CatalogKind kind, params string[] names) { foreach(var name in names) Catalogs.Add(new(){Kind=kind,Name=name,Pending=kind==CatalogKind.Estatus && name is not "CONCLUIDO" and not "NO PROCEDENTE"}); }
        Add(CatalogKind.Estatus,"RECIBIDO","EN PROCESO","PENDIENTE","CONCLUIDO","NO PROCEDENTE");
        Add(CatalogKind.TipoTramite,"DUPLICADO","COTEJO","APOSTILLE","JURIDICÓ","OTRO");
        Add(CatalogKind.NivelEducativo,"PRIMARIA","SECUNDARIA","BACHILLERATO","NORMAL","SUPERIOR","PREESCOLAR","TELEBACHILLERATO","TEBACOM","BELVER");
        Add(CatalogKind.MedioIngreso,"VENTANILLA","TL","CORREO","OTRO");
        // Los responsables se configuran en cada instalación; no distribuir datos de personal.
        Settings.Add(new());
        await SaveChangesAsync();
    }
}

internal sealed class UnicodeSqliteInterceptor : Microsoft.EntityFrameworkCore.Diagnostics.DbConnectionInterceptor
{
    private static void Configure(System.Data.Common.DbConnection connection)
    {
        if(connection is Microsoft.Data.Sqlite.SqliteConnection sqlite)sqlite.CreateFunction<string,string>("upper",value=>value.ToUpperInvariant(),true);
    }
    public override void ConnectionOpened(System.Data.Common.DbConnection connection,Microsoft.EntityFrameworkCore.Diagnostics.ConnectionEndEventData eventData)=>Configure(connection);
    public override Task ConnectionOpenedAsync(System.Data.Common.DbConnection connection,Microsoft.EntityFrameworkCore.Diagnostics.ConnectionEndEventData eventData,CancellationToken cancellationToken=default){Configure(connection);return Task.CompletedTask;}
}

public sealed class DataPaths
{
    public string Root { get; }
    public string DatabaseFile => Path.Combine(Root,"Database","scst.db");
    public string Keys => Path.Combine(Root,"Keys");
    public string Backups => Path.Combine(Root,"Backups");
    public string Temp => Path.Combine(Root,"Temp");
    public DataPaths(string root)
    {
        Root=Path.GetFullPath(root);
        foreach(var dir in new[]{"Database","Backups","Keys","Logs","Exports","Temp"}) Directory.CreateDirectory(Path.Combine(Root,dir));
    }
}
