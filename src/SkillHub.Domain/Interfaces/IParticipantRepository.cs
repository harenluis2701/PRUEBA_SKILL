using SkillHub.Domain.Entities;

namespace SkillHub.Domain.Interfaces;

public interface IParticipantRepository
{
    Task<Participant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Participant?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<Participant?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Participant>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Participant participant, CancellationToken cancellationToken = default);
}
