namespace SkillHub.Application.DTOs.Reports;

public class TopWorkshopReportDto
{
    public Guid WorkshopId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string InstructorName { get; set; } = string.Empty;
    public int MaxCapacity { get; set; }
    public int ActiveEnrollmentsCount { get; set; }
}

public class CertificateDataDto
{
    public Guid EnrollmentId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string ParticipantEmail { get; set; } = string.Empty;
    public string WorkshopCode { get; set; } = string.Empty;
    public string WorkshopTitle { get; set; } = string.Empty;
    public string InstructorName { get; set; } = string.Empty;
    public DateTime WorkshopStart { get; set; }
    public DateTime WorkshopEnd { get; set; }
    public DateTime EnrolledAt { get; set; }
    public DateTime GeneratedAt { get; set; }
}
