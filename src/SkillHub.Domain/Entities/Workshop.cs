using SkillHub.Domain.Enums;
using SkillHub.Domain.Exceptions;

namespace SkillHub.Domain.Entities;

public class Workshop
{
    public Guid Id { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Guid InstructorId { get; private set; }
    public Instructor? Instructor { get; private set; }
    public DateTime StartDateTime { get; private set; }
    public DateTime EndDateTime { get; private set; }
    public int MaxCapacity { get; private set; }

    public ICollection<Enrollment> Enrollments { get; private set; } = new List<Enrollment>();

    private Workshop() { }

    public Workshop(
        Guid id,
        string code,
        string title,
        string description,
        Guid instructorId,
        DateTime startDateTime,
        DateTime endDateTime,
        int maxCapacity,
        bool bypassFutureDateCheck = false)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("Workshop code is required.");

        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Workshop title is required.");

        if (maxCapacity <= 0)
            throw new DomainException("Max capacity must be greater than zero.");

        if (endDateTime <= startDateTime)
            throw new DomainException("End date time must be later than start date time.");

        if (!bypassFutureDateCheck && startDateTime <= DateTime.UtcNow)
            throw new DomainException("Workshop start date time must be in the future.");

        Id = id;
        Code = code.Trim().ToUpperInvariant();
        Title = title.Trim();
        Description = description.Trim();
        InstructorId = instructorId;
        StartDateTime = startDateTime;
        EndDateTime = endDateTime;
        MaxCapacity = maxCapacity;
    }

    public int GetAvailableSpots()
    {
        var activeEnrollments = Enrollments.Count(e => e.Status == EnrollmentStatus.Active);
        var available = MaxCapacity - activeEnrollments;
        return available < 0 ? 0 : available;
    }

    public bool HasStarted(DateTime currentUtc)
    {
        return currentUtc >= StartDateTime;
    }
}
