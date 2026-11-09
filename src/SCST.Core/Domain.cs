using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace SCST.Core;

public enum Role { Administrador, Capturista, Consulta }
public enum CatalogKind { Estatus, TipoTramite, NivelEducativo, MedioIngreso, Validador }
public enum ValidationResult { PendienteDeValidar, Validado, ConInconsistencias }
public enum Traffic { Verde, Amarillo, Rojo }
public class DomainException(string message) : Exception(message);
public abstract class Record
{
    public int Id { get; set; }
    public bool Deleted { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedUtc { get; set; } = DateTime.UtcNow;
    public Guid Version { get; set; } = Guid.NewGuid();
}
public class Catalog : Record
{
    public CatalogKind Kind { get; set; }
    [Required] public string Name { get; set; } = "";
    public bool Active { get; set; } = true;
    public bool Pending { get; set; }
}
public class Tramite : Record
{
    [Required] public string Folio { get; set; } = "";
    [Required] public string Names { get; set; } = "";
    [Required] public string FirstSurname { get; set; } = "";
    public string? SecondSurname { get; set; }
    public string? OriginalName { get; set; }
    [Required] public DateOnly Reception { get; set; }
    public int Year { get; set; }
    [Range(1,int.MaxValue)] public int TypeId { get; set; }
    public Catalog Type { get; set; } = null!;
    [Range(1,int.MaxValue)] public int LevelId { get; set; }
    public Catalog Level { get; set; } = null!;
    public int StatusId { get; set; }
    public Catalog Status { get; set; } = null!;
    public int? MediumId { get; set; }
    public Catalog? Medium { get; set; }
    public int? ValidatorId { get; set; }
    public Catalog? Validator { get; set; }
    public string? Cct { get; set; }
    public string? TlFolio { get; set; }
    public string? SchoolCycle { get; set; }
    public string? CertificateFolio { get; set; }
    public string? Documentation { get; set; }
    public string? Incident { get; set; }
    public string? Observations { get; set; }
    public DateOnly? Assignment { get; set; }
    public DateOnly? ValidatorResponse { get; set; }
    public DateOnly? SourceCapture { get; set; }
    public DateOnly? Issue { get; set; }
    public DateOnly? WindowResponse { get; set; }
    public DateOnly? Conclusion { get; set; }
    public DateOnly? ValidationDate { get; set; }
    public ValidationResult? Validation { get; set; }
    public List<FollowUp> FollowUps { get; set; } = [];
    public List<Reentry> Reentries { get; set; } = [];
    public List<TramiteOficio> Letters { get; set; } = [];
    public string FullName => string.Join(" ", new[]{Names,FirstSurname,SecondSurname}.Where(x=>!string.IsNullOrWhiteSpace(x)));
    public void Normalize()
    {
        if (string.IsNullOrWhiteSpace(Folio) || string.IsNullOrWhiteSpace(Names) || string.IsNullOrWhiteSpace(FirstSurname) || Reception == default || TypeId <= 0 || LevelId <= 0)
            throw new DomainException("Capture folio, nombre, primer apellido, recepción, tipo y nivel educativo.");
        Names = Names.Trim().ToUpper(CultureInfo.GetCultureInfo("es-MX"));
        FirstSurname = FirstSurname.Trim().ToUpper(CultureInfo.GetCultureInfo("es-MX"));
        SecondSurname = SecondSurname?.Trim().ToUpper(CultureInfo.GetCultureInfo("es-MX"));
        Year = Reception.Year;
    }
    public Traffic? Signal(DateOnly today, bool pending) => !pending ? null : (today.DayNumber - Reception.DayNumber) switch { <=15 => Traffic.Verde, <=30 => Traffic.Amarillo, _ => Traffic.Rojo };
    public bool DateWarning(DateOnly today) => new DateOnly?[]{Reception,Assignment,ValidatorResponse,SourceCapture,Issue,WindowResponse,Conclusion,ValidationDate}.Any(d=>d>today) || new[]{Assignment,ValidatorResponse,Issue,WindowResponse,Conclusion,ValidationDate}.Any(d=>d<Reception);
}
public class FollowUp : Record
{
    public int TramiteId { get; set; }
    public Tramite Tramite { get; set; } = null!;
    [Required] public string Activity { get; set; } = "";
    public string? Observations { get; set; }
    public DateOnly Date { get; set; }
    public int? ResponsibleId { get; set; }
    public Catalog? Responsible { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
}
public class Reentry : Record
{
    public int TramiteId { get; set; }
    public Tramite Tramite { get; set; } = null!;
    public DateOnly Date { get; set; }
    [Required] public string Reason { get; set; } = "";
    public string? Observations { get; set; }
}
public class Oficio : Record
{
    [Required] public string Number { get; set; } = "";
    public int Year { get; set; }
    public DateOnly? Date { get; set; }
    public string? Observations { get; set; }
    public List<TramiteOficio> Tramites { get; set; } = [];
}
public class TramiteOficio
{
    public int TramiteId { get; set; }
    public Tramite Tramite { get; set; } = null!;
    public int OficioId { get; set; }
    public Oficio Oficio { get; set; } = null!;
}
public class User : Record
{
    public string Name { get; set; } = "";
    public string Username { get; set; } = "";
    public string NormalizedUsername { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public Role Role { get; set; }
    public bool Active { get; set; } = true;
    public bool MustChangePassword { get; set; } = true;
    public int FailedAttempts { get; set; }
    public DateTime? LockedUntilUtc { get; set; }
    public string SecurityStamp { get; set; } = Guid.NewGuid().ToString();
    public string Columns { get; set; } = "Folio,Nombre,Recepción,Tipo,Nivel,Estatus,Validador,Semáforo";
}
public class Settings
{
    public int Id { get; set; } = 1;
    public string Dependency { get; set; } = "";
    public string Unit { get; set; } = "";
    public string Area { get; set; } = "";
    public int Year { get; set; } = 2026;
    public bool Complete { get; set; }
    public DateTime? LastBackupUtc { get; set; }
}
public class Audit
{
    public long Id { get; set; }
    public int? UserId { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public string Action { get; set; } = "";
    public string Entity { get; set; } = "";
    public string EntityId { get; set; } = "";
    public string? Folio { get; set; }
}
public static class Access
{
    public static bool CanWrite(Role role) => role is Role.Administrador or Role.Capturista;
    public static void RequireWrite(Role role) { if(!CanWrite(role)) throw new DomainException("No tiene permiso para modificar registros."); }
    public static void RequireAdmin(Role role) { if(role!=Role.Administrador) throw new DomainException("Acceso exclusivo del administrador."); }
}
