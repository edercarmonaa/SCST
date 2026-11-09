using Microsoft.EntityFrameworkCore;
using SCST.Core;

namespace SCST.Infrastructure;
public sealed record Actor(int Id, Role Role);
public sealed record Search(int? Year=null,string? Text=null,int? Status=null,int? Type=null,int? Level=null,int? Validator=null,bool Historical=false,bool WithoutLetter=false,DateOnly? From=null,DateOnly? To=null,bool ByIssue=false,string? Signal=null);
public class TramiteService(Database db)
{
    public IQueryable<Tramite> Query(Search s)
    {
        var q=db.Tramites.AsNoTracking().Include(x=>x.Type).Include(x=>x.Level).Include(x=>x.Status).Include(x=>x.Validator).AsQueryable();
        if(s.Year.HasValue) q=q.Where(x=>x.Year==s.Year);
        if(s.Historical) q=q.Where(x=>x.Year<DateTime.Today.Year && x.Status.Pending);
        if(!string.IsNullOrWhiteSpace(s.Text)) { var t=s.Text.Trim().ToUpperInvariant(); q=q.Where(x=>x.Folio.ToUpper().Contains(t) || x.Names.Contains(t) || x.FirstSurname.Contains(t) || (x.SecondSurname!=null && x.SecondSurname.Contains(t))); }
        if(s.Status.HasValue) q=q.Where(x=>x.StatusId==s.Status);
        if(s.Type.HasValue) q=q.Where(x=>x.TypeId==s.Type);
        if(s.Level.HasValue) q=q.Where(x=>x.LevelId==s.Level);
        if(s.Validator.HasValue) q=q.Where(x=>x.ValidatorId==s.Validator);
        if(s.WithoutLetter) q=q.Where(x=>!x.Letters.Any(l=>!l.Oficio.Deleted));
        if(s.From.HasValue) q=s.ByIssue?q.Where(x=>x.Issue>=s.From):q.Where(x=>x.Reception>=s.From);
        if(s.To.HasValue) q=s.ByIssue?q.Where(x=>x.Issue<=s.To):q.Where(x=>x.Reception<=s.To);
        if(s.Signal!=null) { var today=DateOnly.FromDateTime(DateTime.Today); q=q.Where(x=>x.Status.Pending); q=s.Signal switch {"Verde"=>q.Where(x=>x.Reception>=today.AddDays(-15)),"Amarillo"=>q.Where(x=>x.Reception<today.AddDays(-15)&&x.Reception>=today.AddDays(-30)),"Rojo"=>q.Where(x=>x.Reception<today.AddDays(-30)),_=>q}; }
        return q;
    }
    public Task<Tramite?> Detail(int id) => db.Tramites.AsNoTracking().Include(x=>x.Type).Include(x=>x.Level).Include(x=>x.Status).Include(x=>x.Medium).Include(x=>x.Validator).Include(x=>x.FollowUps.OrderByDescending(f=>f.Date)).ThenInclude(x=>x.Responsible).Include(x=>x.FollowUps).ThenInclude(x=>x.User).Include(x=>x.Reentries).Include(x=>x.Letters.Where(l=>!l.Oficio.Deleted)).ThenInclude(x=>x.Oficio).AsSplitQuery().SingleOrDefaultAsync(x=>x.Id==id);
    public void Audit(Actor a,string action,string entity,object id,string? folio=null) => db.Audits.Add(new(){UserId=a.Id,Action=action,Entity=entity,EntityId=id.ToString()!,Folio=folio});
    public async Task CheckCatalog(int? id,CatalogKind kind,int? old=null)
    {
        if(id==null) return;
        if(!await db.Catalogs.AnyAsync(x=>x.Id==id && x.Kind==kind && (x.Active || id==old))) throw new DomainException("Seleccione un elemento de catálogo activo y válido.");
    }
    public async Task<int> Save(Tramite input,Actor actor,bool confirmDates=false)
    {
        Access.RequireWrite(actor.Role); input.Normalize();
        if(input.Validation.HasValue && !Enum.IsDefined(input.Validation.Value))throw new DomainException("Resultado de validación documental inválido.");
        if(input.DateWarning(DateOnly.FromDateTime(DateTime.Today)) && !confirmDates) throw new DomainException("Hay fechas futuras o anteriores a la recepción. Revise y confirme la advertencia.");
        if(await db.Tramites.IgnoreQueryFilters().AnyAsync(x=>x.Folio==input.Folio && x.Id!=input.Id)) throw new DomainException("El folio ya existe, incluso en papelera.");
        var current=input.Id==0?null:await db.Tramites.SingleOrDefaultAsync(x=>x.Id==input.Id)??throw new DomainException("Expediente no disponible.");
        await CheckCatalog(input.TypeId,CatalogKind.TipoTramite,current?.TypeId);
        await CheckCatalog(input.LevelId,CatalogKind.NivelEducativo,current?.LevelId);
        if(input.StatusId==0) input.StatusId=await db.Catalogs.Where(x=>x.Kind==CatalogKind.Estatus && x.Name=="PENDIENTE").Select(x=>x.Id).SingleAsync();
        await CheckCatalog(input.StatusId,CatalogKind.Estatus,current?.StatusId);
        await CheckCatalog(input.MediumId,CatalogKind.MedioIngreso,current?.MediumId);
        await CheckCatalog(input.ValidatorId,CatalogKind.Validador,current?.ValidatorId);
        var saved=current??new Tramite();
        if(current==null)
        {
            // Copiar solo columnas; nunca adjuntar el grafo recibido desde un formulario.
            db.Entry(saved).CurrentValues.SetValues(input);
            saved.Id=0;saved.Deleted=false;saved.CreatedUtc=DateTime.UtcNow;saved.ModifiedUtc=saved.CreatedUtc;saved.Version=Guid.NewGuid();saved.OriginalName=null;
            db.Tramites.Add(saved);
        }
        else { if(current.Version!=input.Version) throw new DomainException("Otro usuario modificó el expediente. Recargue antes de guardar."); var created=current.CreatedUtc;var original=current.OriginalName; db.Entry(current).CurrentValues.SetValues(input); current.CreatedUtc=created;current.OriginalName=original; current.Deleted=false; }
        await using var tx=await db.Database.BeginTransactionAsync();
        await db.SaveChangesAsync();input.Id=saved.Id;input.Version=saved.Version; Audit(actor,current==null?"Creación":"Modificación","Tramite",saved.Id,saved.Folio); await db.SaveChangesAsync(); await tx.CommitAsync(); return saved.Id;
    }
    public async Task Toggle(string entity,int id,bool restore,Actor actor)
    {
        Access.RequireWrite(actor.Role);
        Record? item=entity switch {"Tramite"=>await db.Tramites.IgnoreQueryFilters().SingleOrDefaultAsync(x=>x.Id==id),"Oficio"=>await db.Oficios.IgnoreQueryFilters().SingleOrDefaultAsync(x=>x.Id==id),"FollowUp"=>await db.FollowUps.IgnoreQueryFilters().SingleOrDefaultAsync(x=>x.Id==id),"Reentry"=>await db.Reentries.IgnoreQueryFilters().SingleOrDefaultAsync(x=>x.Id==id),_=>null};
        if(item==null) throw new DomainException("Registro no encontrado.");
        item.Deleted=!restore; Audit(actor,restore?"Restauración":"Eliminación lógica",entity,id,(item as Tramite)?.Folio); await db.SaveChangesAsync();
    }
    public async Task SaveFollowUp(FollowUp f,Actor a)
    {
        Access.RequireWrite(a.Role);
        if(f.Date==default || string.IsNullOrWhiteSpace(f.Activity) || !await db.Tramites.AnyAsync(x=>x.Id==f.TramiteId)) throw new DomainException("Capture fecha, actividad y expediente válido.");
        var old=f.Id==0?null:await db.FollowUps.SingleOrDefaultAsync(x=>x.Id==f.Id && x.TramiteId==f.TramiteId)??throw new DomainException("Seguimiento no disponible.");
        await CheckCatalog(f.ResponsibleId,CatalogKind.Validador,old?.ResponsibleId);
        var saved=old??new FollowUp{TramiteId=f.TramiteId,Date=f.Date,Activity=f.Activity,Observations=f.Observations,ResponsibleId=f.ResponsibleId,UserId=a.Id};
        if(old==null) db.FollowUps.Add(saved);
        else { old.Activity=f.Activity; old.Observations=f.Observations; old.Date=f.Date; old.ResponsibleId=f.ResponsibleId; }
        await using var tx=await db.Database.BeginTransactionAsync(); await db.SaveChangesAsync();f.Id=saved.Id; Audit(a,old==null?"Creación":"Modificación","FollowUp",saved.Id); await db.SaveChangesAsync(); await tx.CommitAsync();
    }
    public async Task SaveReentry(Reentry r,Actor a)
    {
        Access.RequireWrite(a.Role); if(r.Date==default || string.IsNullOrWhiteSpace(r.Reason) || !await db.Tramites.AnyAsync(x=>x.Id==r.TramiteId)) throw new DomainException("Capture fecha, motivo y expediente válido.");
        if(r.Id!=0) throw new DomainException("No se permite reemplazar un reingreso desde esta operación.");
        var saved=new Reentry{TramiteId=r.TramiteId,Date=r.Date,Reason=r.Reason,Observations=r.Observations};
        db.Reentries.Add(saved); await using var tx=await db.Database.BeginTransactionAsync(); await db.SaveChangesAsync();r.Id=saved.Id; Audit(a,"Creación","Reentry",saved.Id); await db.SaveChangesAsync(); await tx.CommitAsync();
    }
    public async Task SaveOficio(Oficio input,Actor a)
    {
        Access.RequireWrite(a.Role);
        if(string.IsNullOrWhiteSpace(input.Number) || input.Year<1900 || input.Year>9999) throw new DomainException("Número y ejercicio válidos son obligatorios.");
        if(await db.Oficios.IgnoreQueryFilters().AnyAsync(x=>x.Number==input.Number && x.Year==input.Year && x.Id!=input.Id)) throw new DomainException("Número de oficio ya utilizado en ese ejercicio.");
        var saved=input.Id==0?new Oficio{Number=input.Number,Year=input.Year,Date=input.Date,Observations=input.Observations}:input;
        if(input.Id==0) db.Oficios.Add(saved);
        else { var old=await db.Oficios.SingleAsync(x=>x.Id==input.Id); if(old.Version!=input.Version) throw new DomainException("El oficio cambió. Recargue."); old.Number=input.Number; old.Year=input.Year; old.Date=input.Date; old.Observations=input.Observations; }
        await using var tx=await db.Database.BeginTransactionAsync(); await db.SaveChangesAsync();input.Id=saved.Id;input.Version=saved.Version; Audit(a,"Guardar","Oficio",input.Id); await db.SaveChangesAsync(); await tx.CommitAsync();
    }
    public async Task Link(int oficio,int[] ids,bool unlink,Actor a)
    {
        Access.RequireWrite(a.Role); if(!await db.Oficios.AnyAsync(x=>x.Id==oficio)) throw new DomainException("Oficio no disponible.");
        if(ids.Length==0 || await db.Tramites.CountAsync(x=>ids.Contains(x.Id))!=ids.Distinct().Count()) throw new DomainException("Seleccione expedientes activos.");
        foreach(var id in ids.Distinct()) { var l=await db.Links.FindAsync(id,oficio); if(unlink) { if(l!=null) db.Links.Remove(l); } else { if(l!=null) throw new DomainException("El vínculo ya existe."); db.Links.Add(new(){TramiteId=id,OficioId=oficio}); } }
        Audit(a,unlink?"Desvincular":"Vincular","Oficio",oficio); await db.SaveChangesAsync();
    }
    public async Task Bulk(int[] ids,int? status,int? validator,Actor a)
    {
        Access.RequireWrite(a.Role); if(status.HasValue) await CheckCatalog(status,CatalogKind.Estatus); if(validator.HasValue) await CheckCatalog(validator,CatalogKind.Validador);
        var items=await db.Tramites.Where(x=>ids.Contains(x.Id)).ToListAsync();
        foreach(var t in items) { if(status.HasValue)t.StatusId=status.Value; if(validator.HasValue)t.ValidatorId=validator; Audit(a,"Modificación múltiple","Tramite",t.Id,t.Folio); } await db.SaveChangesAsync();
    }
}
