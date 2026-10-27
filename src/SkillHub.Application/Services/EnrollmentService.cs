using SkillHub.Application.DTOs.Enrollments;
using SkillHub.Application.Interfaces;
using SkillHub.Domain.Entities;
using SkillHub.Domain.Enums;
using SkillHub.Domain.Exceptions;
using SkillHub.Domain.Interfaces;

namespace SkillHub.Application.Services;

public class EnrollmentService : IEnrollmentService
{
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IWorkshopRepository _workshopRepository;
    private readonly IParticipantRepository _participantRepository;
    private readonly IExcelReader _excelReader;
    private readonly IUnitOfWork _unitOfWork;

    public EnrollmentService(
        IEnrollmentRepository enrollmentRepository,
        IWorkshopRepository workshopRepository,
        IParticipantRepository participantRepository,
        IExcelReader excelReader,
        IUnitOfWork unitOfWork)
    {
        _enrollmentRepository = enrollmentRepository;
        _workshopRepository = workshopRepository;
        _participantRepository = participantRepository;
        _excelReader = excelReader;
        _unitOfWork = unitOfWork;
    }

    public async Task<EnrollmentResponseDto> EnrollParticipantAsync(Guid workshopId, string currentUserId, CancellationToken cancellationToken = default)
    {
        var participant = await _participantRepository.GetByUserIdAsync(currentUserId, cancellationToken);
        if (participant is null)
            throw new NotFoundException("Participant profile not found for the authenticated user.");

        var workshop = await _workshopRepository.GetByIdAsync(workshopId, cancellationToken);
        if (workshop is null)
            throw new NotFoundException($"Workshop with id '{workshopId}' was not found.");

        await ValidateEnrollmentRulesAsync(participant.Id, workshop, cancellationToken);

        var enrollment = new Enrollment(Guid.NewGuid(), workshop.Id, participant.Id, DateTime.UtcNow);
        await _enrollmentRepository.AddAsync(enrollment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var created = await _enrollmentRepository.GetByIdAsync(enrollment.Id, cancellationToken);
        return MapToResponseDto(created ?? enrollment);
    }

    public async Task<IReadOnlyList<EnrollmentResponseDto>> GetMyEnrollmentsAsync(string currentUserId, CancellationToken cancellationToken = default)
    {
        var participant = await _participantRepository.GetByUserIdAsync(currentUserId, cancellationToken);
        if (participant is null)
            throw new NotFoundException("Participant profile not found for the authenticated user.");

        var enrollments = await _enrollmentRepository.GetByParticipantIdAsync(participant.Id, cancellationToken);
        return enrollments.Select(MapToResponseDto).ToList();
    }

    public async Task<IReadOnlyList<EnrollmentResponseDto>> GetWorkshopEnrollmentsAsync(Guid workshopId, CancellationToken cancellationToken = default)
    {
        var workshop = await _workshopRepository.GetByIdAsync(workshopId, cancellationToken);
        if (workshop is null)
            throw new NotFoundException($"Workshop with id '{workshopId}' was not found.");

        var enrollments = await _enrollmentRepository.GetByWorkshopIdAsync(workshopId, cancellationToken);
        return enrollments.Select(MapToResponseDto).ToList();
    }

    public async Task CancelEnrollmentAsync(Guid enrollmentId, string currentUserId, string currentUserEmail, bool isEmployee, CancellationToken cancellationToken = default)
    {
        var enrollment = await _enrollmentRepository.GetByIdAsync(enrollmentId, cancellationToken);
        if (enrollment is null)
            throw new NotFoundException($"Enrollment with id '{enrollmentId}' was not found.");

        if (!isEmployee)
        {
            var participant = await _participantRepository.GetByUserIdAsync(currentUserId, cancellationToken);
            if (participant is null || enrollment.ParticipantId != participant.Id)
                throw new ForbiddenAccessException("You are not authorized to cancel this enrollment.");
        }

        var workshopStart = enrollment.Workshop?.StartDateTime ?? DateTime.MinValue;
        enrollment.Cancel(currentUserEmail, isEmployee, DateTime.UtcNow, workshopStart);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<BulkImportResultDto> ImportEnrollmentsFromExcelAsync(Stream fileStream, CancellationToken cancellationToken = default)
    {
        var rows = _excelReader.ReadEnrollmentRows(fileStream);
        var result = new BulkImportResultDto();

        foreach (var row in rows)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(row.ParticipantEmail))
                    throw new DomainException("Participant email cannot be empty.");

                if (string.IsNullOrWhiteSpace(row.WorkshopCode))
                    throw new DomainException("Workshop code cannot be empty.");

                var participant = await _participantRepository.GetByEmailAsync(row.ParticipantEmail.Trim(), cancellationToken);
                if (participant is null)
                    throw new NotFoundException($"Participant '{row.ParticipantEmail}' does not exist.");

                var workshop = await _workshopRepository.GetByCodeAsync(row.WorkshopCode.Trim().ToUpperInvariant(), cancellationToken);
                if (workshop is null)
                    throw new NotFoundException($"Workshop with code '{row.WorkshopCode}' does not exist.");

                await ValidateEnrollmentRulesAsync(participant.Id, workshop, cancellationToken);

                var enrollment = new Enrollment(Guid.NewGuid(), workshop.Id, participant.Id, DateTime.UtcNow);
                await _enrollmentRepository.AddAsync(enrollment, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                result.EnrolledCount++;
            }
            catch (Exception ex)
            {
                result.RejectedRows.Add(new RejectedRowDto
                {
                    RowNumber = row.RowNumber,
                    Reason = ex.Message
                });
            }
        }

        return result;
    }

    private async Task ValidateEnrollmentRulesAsync(Guid participantId, Workshop workshop, CancellationToken cancellationToken)
    {
        // Rule 4: Only future workshops
        if (workshop.StartDateTime <= DateTime.UtcNow)
            throw new DomainException($"Cannot enroll in workshop '{workshop.Code}' because it has already started or finished.");

        // Rule 2 & 6: No duplicate active enrollments
        var alreadyActive = await _enrollmentRepository.HasActiveEnrollmentAsync(participantId, workshop.Id, cancellationToken);
        if (alreadyActive)
            throw new DomainException($"Participant already has an active enrollment in workshop '{workshop.Code}'.");

        // Rule 1: Capacity check
        if (workshop.GetAvailableSpots() <= 0)
            throw new DomainException($"No spots available for workshop '{workshop.Code}'. Maximum capacity is {workshop.MaxCapacity}.");

        // Rule 3: Schedule conflict check with any current active enrollment
        var activeEnrollments = await _enrollmentRepository.GetActiveByParticipantIdAsync(participantId, cancellationToken);
        foreach (var active in activeEnrollments)
        {
            if (active.Workshop is null) continue;

            bool overlaps = workshop.StartDateTime < active.Workshop.EndDateTime
                         && workshop.EndDateTime > active.Workshop.StartDateTime;

            if (overlaps)
            {
                throw new DomainException(
                    $"Schedule conflict: workshop '{workshop.Code}' overlaps with workshop '{active.Workshop.Code}' ({active.Workshop.StartDateTime:g} - {active.Workshop.EndDateTime:g}).");
            }
        }
    }

    private static EnrollmentResponseDto MapToResponseDto(Enrollment e)
    {
        return new EnrollmentResponseDto
        {
            Id = e.Id,
            WorkshopId = e.WorkshopId,
            WorkshopCode = e.Workshop?.Code ?? string.Empty,
            WorkshopTitle = e.Workshop?.Title ?? string.Empty,
            InstructorName = e.Workshop?.Instructor?.FullName ?? string.Empty,
            WorkshopStart = e.Workshop?.StartDateTime ?? DateTime.MinValue,
            WorkshopEnd = e.Workshop?.EndDateTime ?? DateTime.MinValue,
            ParticipantId = e.ParticipantId,
            ParticipantName = e.Participant?.FullName ?? string.Empty,
            ParticipantEmail = e.Participant?.Email ?? string.Empty,
            Status = e.Status.ToString(),
            EnrolledAt = e.EnrolledAt,
            CancelledAt = e.CancelledAt,
            CancelledBy = e.CancelledBy
        };
    }
}
