using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SkillHub.Application.Common;
using SkillHub.Application.DTOs.Auth;
using SkillHub.Application.Interfaces;
using SkillHub.Domain.Entities;
using SkillHub.Domain.Exceptions;
using SkillHub.Domain.Interfaces;
using SkillHub.Infrastructure.Identity;

namespace SkillHub.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IParticipantRepository _participantRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        IParticipantRepository participantRepository,
        IUnitOfWork unitOfWork,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _participantRepository = participantRepository;
        _unitOfWork = unitOfWork;
        _configuration = configuration;
    }

    public async Task<AuthResponseDto> RegisterParticipantAsync(RegisterRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new DomainException("Email is required.");

        if (string.IsNullOrWhiteSpace(request.FullName))
            throw new DomainException("Full name is required.");

        if (string.IsNullOrWhiteSpace(request.Password))
            throw new DomainException("Password is required.");

        var existingUser = await _userManager.FindByEmailAsync(request.Email.Trim());
        if (existingUser is not null)
            throw new DomainException($"A user with email '{request.Email}' already exists.");

        var user = new ApplicationUser
        {
            UserName = request.Email.Trim().ToLowerInvariant(),
            Email = request.Email.Trim().ToLowerInvariant(),
            FullName = request.FullName.Trim()
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new DomainException($"Registration failed: {errors}");
        }

        // Everyone registering via this endpoint is ALWAYS a Participant
        await _userManager.AddToRoleAsync(user, AppRoles.Participant);

        var participant = new Participant(Guid.NewGuid(), user.Id, user.FullName, user.Email);
        await _participantRepository.AddAsync(participant, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return GenerateJwtToken(user, AppRoles.Participant);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            throw new DomainException("Email and password are required.");

        var user = await _userManager.FindByEmailAsync(request.Email.Trim().ToLowerInvariant());
        if (user is null || !await _userManager.CheckPasswordAsync(user, request.Password))
            throw new DomainException("Invalid email or password.");

        var roles = await _userManager.GetRolesAsync(user);
        var primaryRole = roles.Contains(AppRoles.Employee) ? AppRoles.Employee : AppRoles.Participant;

        return GenerateJwtToken(user, primaryRole);
    }

    private AuthResponseDto GenerateJwtToken(ApplicationUser user, string role)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var secretKey = jwtSettings["SecretKey"] ?? "SkillHub_Super_Secret_Key_For_Jwt_Authentication_2026!";
        var issuer = jwtSettings["Issuer"] ?? "SkillHubApi";
        var audience = jwtSettings["Audience"] ?? "SkillHubClients";
        var expirationHours = int.TryParse(jwtSettings["ExpirationHours"], out var h) ? h : 8;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expires = DateTime.UtcNow.AddHours(expirationHours);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, role)
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        return new AuthResponseDto
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            Role = role,
            ExpiresAt = expires
        };
    }
}
