namespace SkillHub.Domain.Entities;

public class Instructor
{
    public Guid Id { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Specialty { get; private set; } = string.Empty;

    public ICollection<Workshop> Workshops { get; private set; } = new List<Workshop>();

    private Instructor() { }

    public Instructor(Guid id, string userId, string fullName, string email, string specialty)
    {
        Id = id;
        UserId = userId;
        FullName = fullName;
        Email = email;
        Specialty = specialty;
    }
}
