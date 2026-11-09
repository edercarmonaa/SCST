using Microsoft.Extensions.Logging;
using SCST.Infrastructure;
namespace SCST.Tests;
public class LoggingTests
{
    [Fact]public void TechnicalLogsExcludeArbitraryStateAndExceptionContents()
    {
        var directory=Path.Combine(Path.GetTempPath(),"SCST-log-test-"+Guid.NewGuid());
        using var provider=new FileLoggerProvider(directory);
        var marker="SYNTHETIC_PRIVATE_VALUE_DO_NOT_LOG";
        provider.CreateLogger("Microsoft.Database.Test").LogWarning(new EventId(42),new Exception(marker),"Authorization: {Value}",marker);
        var content=File.ReadAllText(Directory.GetFiles(directory,"*.log").Single());
        Assert.DoesNotContain(marker,content);Assert.DoesNotContain("Authorization",content);Assert.Contains("\"EventId\":42",content);Assert.Contains("ExceptionType",content);
    }
}
