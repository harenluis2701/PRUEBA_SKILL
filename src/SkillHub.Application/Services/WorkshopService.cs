using SkillHub.Application.DTOs.Workshops;
using SkillHub.Application.Interfaces;
using SkillHub.Domain.Entities;
using SkillHub.Domain.Exceptions;
using SkillHub.Domain.Interfaces;

namespace SkillHub.Application.Services;

public class WorkshopService : IWorkshopService
{
    private readonly IWorkshopRepository _workshopRepository;
    private readonly IInstructorRepository _instructorRepository;
    private readonly IUnitOfWork _unitOfWork;

    public WorkshopService(
        IWorkshopRepository workshopRepository,
        IInstructorRepository instructorRepository,
        IUnitOfWork unitOfWork)
    {
        _workshopRepository = workshopRepository;
        _instructorRepository = instructorRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<WorkshopResponseDto>> GetUpcomingWorkshopsAsync(CancellationToken cancellationToken = default)
    {
        var workshops = await _workshopRepository.GetUpcomingWorkshopsAsync(DateTime.UtcNow, cancellationToken);
        return workshops.Select(MapToResponseDto).ToList();
    }

    public async Task<WorkshopResponseDto> GetWorkshopByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var workshop = await _workshopRepository.GetByIdAsync(id, cancellationToken);
        if (workshop is null)
            throw new NotFoundException($"Workshop with id '{id}' was not found.");

        return MapToResponseDto(workshop);
    }

    public async Task<WorkshopResponseDto> CreateWorkshopAsync(CreateWorkshopRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            throw new DomainException("Workshop code is required.");

        var normalizedCode = request.Code.Trim().ToUpperInvariant();
        if (await _workshopRepository.ExistsByCodeAsync(normalizedCode, cancellationToken))
            throw new DomainException($"A workshop with code '{normalizedCode}' already exists.");

        if (string.IsNullOrWhiteSpace(request.InstructorEmail))
            throw new DomainException("Instructor email is required.");

        var instructor = await _instructorRepository.GetByEmailAsync(request.InstructorEmail.Trim(), cancellationToken);
        if (instructor is null)
            throw new NotFoundException($"Instructor with email '{request.InstructorEmail}' does not exist in the system.");

        var workshop = new Workshop(
            Guid.NewGuid(),
            normalizedCode,
            request.Title,
            request.Description,
            instructor.Id,
            request.StartDateTime,
            request.EndDateTime,
            request.MaxCapacity);

        await _workshopRepository.AddAsync(workshop, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Fetch back to ensure navigation properties are populated for the response
        var created = await _workshopRepository.GetByIdAsync(workshop.Id, cancellationToken);
        return MapToResponseDto(created ?? workshop);
    }

    private static WorkshopResponseDto MapToResponseDto(Workshop w)
    {
        return new WorkshopResponseDto
        {
            Id = w.Id,
            Code = w.Code,
            Title = w.Title,
            Description = w.Description,
            StartDateTime = w.StartDateTime,
            EndDateTime = w.EndDateTime,
            MaxCapacity = w.MaxCapacity,
            AvailableSpots = w.GetAvailableSpots(),
            Instructor = w.Instructor is not null
                ? new InstructorSummaryDto
                {
                    Id = w.Instructor.Id,
                    FullName = w.Instructor.FullName,
                    Email = w.Instructor.Email,
                    Specialty = w.Instructor.Specialty
                }
                : new InstructorSummaryDto()
        };
    }
}
