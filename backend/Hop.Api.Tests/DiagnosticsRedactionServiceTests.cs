using Hop.Api.Services;
using Xunit;

namespace Hop.Api.Tests;

public class DiagnosticsRedactionServiceTests
{
    private readonly DiagnosticsRedactionService _service = new();

    [Fact]
    public void Redact_MasksJwtBearerAndPasswordValues()
    {
        var input = "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.payload.signature Password=VerySecret123";

        var result = _service.Redact(input);

        Assert.DoesNotContain("eyJhbGci", result);
        Assert.DoesNotContain("VerySecret123", result);
        Assert.Contains("[REDACTED", result);
        Assert.Contains("Password=[REDACTED]", result);
    }

    [Fact]
    public void Redact_MasksLineUserIdEmailPhoneAndConnectionStringPassword()
    {
        var input = "User Ud030fccdc5547226c8b0fb2016fb51eb email admin@example.com phone 0812345678 Host=db;Password=db-secret;";

        var result = _service.Redact(input);

        Assert.DoesNotContain("Ud030fccdc5547226c8b0fb2016fb51eb", result);
        Assert.DoesNotContain("admin@example.com", result);
        Assert.DoesNotContain("0812345678", result);
        Assert.DoesNotContain("db-secret", result);
        Assert.Contains("Ud030...51eb", result);
        Assert.Contains("a***@example.com", result);
        Assert.Contains("[PHONE]", result);
        Assert.Contains("Password=[REDACTED]", result);
    }
}
