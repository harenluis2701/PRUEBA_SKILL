using SkillHub.Domain.Entities;

namespace SkillHub.Domain.Interfaces;

public interface IWorkshopRepository
{
    Task<Workshop?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Workshop?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Workshop>> GetUpcomingWorkshopsAsync(DateTime currentUtc, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Workshop>> GetTopWorkshopsByActiveEnrollmentsAsync(int limit = 10, CancellationToken cancellationToken = default);
    Task AddAsync(Workshop workshop, CancellationToken cancellationToken = default);
}
