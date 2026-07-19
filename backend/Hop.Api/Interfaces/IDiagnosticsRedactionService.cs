namespace Hop.Api.Interfaces;

public interface IDiagnosticsRedactionService
{
    string Redact(string? value);
}
