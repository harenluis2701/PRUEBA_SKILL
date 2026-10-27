using SkillHub.Application.DTOs.Auth;
using SkillHub.Application.DTOs.Enrollments;
using SkillHub.Application.DTOs.Reports;
using SkillHub.Application.DTOs.Workshops;

namespace SkillHub.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterParticipantAsync(RegisterRequestDto request, CancellationToken cancellationToken = default);
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
}

public interface IWorkshopService
{
    Task<IReadOnlyList<WorkshopResponseDto>> GetUpcomingWorkshopsAsync(CancellationToken cancellationToken = default);
    Task<WorkshopResponseDto> GetWorkshopByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<WorkshopResponseDto> CreateWorkshopAsync(CreateWorkshopRequestDto request, CancellationToken cancellationToken = default);
}

public interface IEnrollmentService
{
    Task<EnrollmentResponseDto> EnrollParticipantAsync(Guid workshopId, string currentUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EnrollmentResponseDto>> GetMyEnrollmentsAsync(string currentUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EnrollmentResponseDto>> GetWorkshopEnrollmentsAsync(Guid workshopId, CancellationToken cancellationToken = default);
    Task CancelEnrollmentAsync(Guid enrollmentId, string currentUserId, string currentUserEmail, bool isEmployee, CancellationToken cancellationToken = default);
    Task<BulkImportResultDto> ImportEnrollmentsFromExcelAsync(Stream fileStream, CancellationToken cancellationToken = default);
}

public interface IReportService
{
    Task<IReadOnlyList<TopWorkshopReportDto>> GetTopWorkshopsAsync(CancellationToken cancellationToken = default);
    Task<byte[]> GenerateCertificatePdfAsync(Guid enrollmentId, string currentUserId, bool isEmployee, CancellationToken cancellationToken = default);
}

public interface IExcelReader
{
    IReadOnlyList<ExcelEnrollmentRowDto> ReadEnrollmentRows(Stream excelStream);
}

public interface IPdfGenerator
{
    byte[] GenerateCertificate(CertificateDataDto data);
}
