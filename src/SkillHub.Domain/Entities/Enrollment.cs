using SkillHub.Domain.Enums;
using SkillHub.Domain.Exceptions;

namespace SkillHub.Domain.Entities;

public class Enrollment
{
    public Guid Id { get; private set; }
    public Guid WorkshopId { get; private set; }
    public Workshop? Workshop { get; private set; }
    public Guid ParticipantId { get; private set; }
    public Participant? Participant { get; private set; }
    public EnrollmentStatus Status { get; private set; }
    public DateTime EnrolledAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public string? CancelledBy { get; private set; }

    private Enrollment() { }

    public Enrollment(Guid id, Guid workshopId, Guid participantId, DateTime enrolledAt)
    {
        Id = id;
        WorkshopId = workshopId;
        ParticipantId = participantId;
        Status = EnrollmentStatus.Active;
        EnrolledAt = enrolledAt;
    }

    public void Cancel(string cancelledByEmail, bool isEmployee, DateTime currentUtc, DateTime workshopStartUtc)
    {
        if (Status == EnrollmentStatus.Cancelled)
            throw new DomainException("Enrollment is already cancelled.");

        if (currentUtc >= workshopStartUtc)
            throw new DomainException("Cannot cancel an enrollment once the workshop has started.");

        if (!isEmployee)
        {
            var hoursUntilStart = (workshopStartUtc - currentUtc).TotalHours;
            if (hoursUntilStart < 24)
                throw new DomainException("Participants can only cancel their enrollment up to 24 hours before the workshop starts.");
        }

        Status = EnrollmentStatus.Cancelled;
        CancelledAt = currentUtc;
        CancelledBy = cancelledByEmail;
    }
}
