namespace SkillHub.Application.DTOs.Enrollments;

public class CreateEnrollmentRequestDto
{
    public Guid WorkshopId { get; set; }
}

public class EnrollmentResponseDto
{
    public Guid Id { get; set; }
    public Guid WorkshopId { get; set; }
    public string WorkshopCode { get; set; } = string.Empty;
    public string WorkshopTitle { get; set; } = string.Empty;
    public string InstructorName { get; set; } = string.Empty;
    public DateTime WorkshopStart { get; set; }
    public DateTime WorkshopEnd { get; set; }
    public Guid ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string ParticipantEmail { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime EnrolledAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelledBy { get; set; }
}

public class BulkImportResultDto
{
    public int EnrolledCount { get; set; }
    public List<RejectedRowDto> RejectedRows { get; set; } = new();
}

public class RejectedRowDto
{
    public int RowNumber { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class ExcelEnrollmentRowDto
{
    public int RowNumber { get; set; }
    public string ParticipantEmail { get; set; } = string.Empty;
    public string WorkshopCode { get; set; } = string.Empty;
}
