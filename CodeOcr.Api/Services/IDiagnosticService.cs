using CodeOcr.Api.Contracts;

namespace CodeOcr.Api.Services;

public interface IDiagnosticService
{
    DiagnosticResponse GetStatus();
}