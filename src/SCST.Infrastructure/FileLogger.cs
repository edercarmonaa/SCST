using Microsoft.Extensions.Logging;

namespace SCST.Infrastructure;
public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly string directory;private readonly object gate=new();
    public FileLoggerProvider(string directory){this.directory=directory;Directory.CreateDirectory(directory);foreach(var f in Directory.GetFiles(directory,"*.log").Where(f=>File.GetLastWriteTimeUtc(f)<DateTime.UtcNow.AddDays(-90)))File.Delete(f);}
    public ILogger CreateLogger(string categoryName)=>new FileLogger(this,categoryName);
    public void Dispose(){}
    private sealed class FileLogger(FileLoggerProvider owner,string category):ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState:notnull=>null;
        public bool IsEnabled(LogLevel level)=>level>=LogLevel.Warning || category=="SCST.Web";
        public void Log<TState>(LogLevel level,EventId eventId,TState state,Exception? exception,Func<TState,Exception?,string> formatter)
        {
            if(!IsEnabled(level))return;
            // No serializar mensajes arbitrarios, parámetros SQL, estado ni excepciones completas.
            var line=System.Text.Json.JsonSerializer.Serialize(new{TimestampUtc=DateTime.UtcNow,Level=level.ToString(),Category=category,EventId=eventId.Id,ExceptionType=exception?.GetType().Name});
            lock(owner.gate)File.AppendAllText(Path.Combine(owner.directory,DateTime.Now.ToString("yyyy-MM-dd")+".log"),line+Environment.NewLine);
        }
    }
}
