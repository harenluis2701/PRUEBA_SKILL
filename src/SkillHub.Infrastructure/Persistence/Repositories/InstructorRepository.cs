using Microsoft.EntityFrameworkCore;
using SkillHub.Domain.Entities;
using SkillHub.Domain.Interfaces;

namespace SkillHub.Infrastructure.Persistence.Repositories;

public class InstructorRepository : IInstructorRepository
{
    private readonly ApplicationDbContext _context;

    public InstructorRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Instructor?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Instructors
            .Include(i => i.Workshops)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<Instructor?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToLower();
        return await _context.Instructors
            .Include(i => i.Workshops)
            .FirstOrDefaultAsync(i => i.Email.ToLower() == normalized, cancellationToken);
    }

    public async Task<Instructor?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.Instructors
            .Include(i => i.Workshops)
            .FirstOrDefaultAsync(i => i.UserId == userId, cancellationToken);
    }

    public async Task<IReadOnlyList<Instructor>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Instructors.ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Instructor instructor, CancellationToken cancellationToken = default)
    {
        await _context.Instructors.AddAsync(instructor, cancellationToken);
    }
}
