using StroyControl.Core.Dtos;

namespace StroyControl.Core.Interfaces;

/// <summary>Анализ отклонений факта от сметы.</summary>
public interface IDeviationService
{
    Task<DeviationReport> BuildReportAsync(int? projectId = null, CancellationToken cancellationToken = default);
}
