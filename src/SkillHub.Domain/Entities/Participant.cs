namespace SkillHub.Domain.Entities;

public class Participant
{
    public Guid Id { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;

    public ICollection<Enrollment> Enrollments { get; private set; } = new List<Enrollment>();

    private Participant() { }

    public Participant(Guid id, string userId, string fullName, string email)
    {
        Id = id;
        UserId = userId;
        FullName = fullName;
        Email = email;
    }
}
