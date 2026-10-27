using SkillHub.Application.DTOs.Reports;
using SkillHub.Application.Interfaces;
using SkillHub.Domain.Entities;
using SkillHub.Domain.Enums;
using SkillHub.Domain.Exceptions;
using SkillHub.Domain.Interfaces;

namespace SkillHub.Application.Services;

public class ReportService : IReportService
{
    private readonly IWorkshopRepository _workshopRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IParticipantRepository _participantRepository;
    private readonly IPdfGenerator _pdfGenerator;

    public ReportService(
        IWorkshopRepository workshopRepository,
        IEnrollmentRepository enrollmentRepository,
        IParticipantRepository participantRepository,
        IPdfGenerator pdfGenerator)
    {
        _workshopRepository = workshopRepository;
        _enrollmentRepository = enrollmentRepository;
        _participantRepository = participantRepository;
        _pdfGenerator = pdfGenerator;
    }

    public async Task<IReadOnlyList<TopWorkshopReportDto>> GetTopWorkshopsAsync(CancellationToken cancellationToken = default)
    {
        var workshops = await _workshopRepository.GetTopWorkshopsByActiveEnrollmentsAsync(100, cancellationToken);
        return workshops.Select(w => new TopWorkshopReportDto
        {
            WorkshopId = w.Id,
            Code = w.Code,
            Title = w.Title,
            InstructorName = w.Instructor?.FullName ?? string.Empty,
            MaxCapacity = w.MaxCapacity,
            ActiveEnrollmentsCount = w.Enrollments.Count(e => e.Status == EnrollmentStatus.Active)
        }).ToList();
    }

    public async Task<byte[]> GenerateCertificatePdfAsync(Guid enrollmentId, string currentUserId, bool isEmployee, CancellationToken cancellationToken = default)
    {
        var enrollment = await _enrollmentRepository.GetByIdAsync(enrollmentId, cancellationToken);
        if (enrollment is null)
            throw new NotFoundException($"Enrollment with id '{enrollmentId}' was not found.");

        if (!isEmployee)
        {
            var participant = await _participantRepository.GetByUserIdAsync(currentUserId, cancellationToken);
            if (participant is null || enrollment.ParticipantId != participant.Id)
                throw new ForbiddenAccessException("You are only allowed to download certificates for your own enrollments.");
        }

        if (enrollment.Status != EnrollmentStatus.Active)
            throw new DomainException("Cannot generate a certificate for a cancelled enrollment.");

        var data = new CertificateDataDto
        {
            EnrollmentId = enrollment.Id,
            ParticipantName = enrollment.Participant?.FullName ?? "Participant",
            ParticipantEmail = enrollment.Participant?.Email ?? string.Empty,
            WorkshopCode = enrollment.Workshop?.Code ?? string.Empty,
            WorkshopTitle = enrollment.Workshop?.Title ?? string.Empty,
            InstructorName = enrollment.Workshop?.Instructor?.FullName ?? string.Empty,
            WorkshopStart = enrollment.Workshop?.StartDateTime ?? DateTime.MinValue,
            WorkshopEnd = enrollment.Workshop?.EndDateTime ?? DateTime.MinValue,
            EnrolledAt = enrollment.EnrolledAt,
            GeneratedAt = DateTime.UtcNow
        };

        return _pdfGenerator.GenerateCertificate(data);
    }
}
