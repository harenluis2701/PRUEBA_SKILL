using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SkillHub.Application.Common;
using SkillHub.Domain.Entities;
using SkillHub.Infrastructure.Identity;

namespace SkillHub.Infrastructure.Persistence.Seed;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        // 1. Ensure Roles exist
        string[] roles = [AppRoles.Employee, AppRoles.Participant];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // 2. Default Seed Employees / Instructors
        var instructorsData = new (string FullName, string Email, string Specialty, string Password)[]
        {
            ("Carlos Mendoza", "carlos.mendoza@skillhub.com", "Fotografía Profesional", "SkillHub2026*"),
            ("Laura Gomez", "laura.gomez@skillhub.com", "Cocina Internacional", "SkillHub2026*"),
            ("Andres Restrepo", "andres.restrepo@skillhub.com", "Oratoria y Comunicación", "SkillHub2026*"),
            ("Mariana Rios", "mariana.rios@skillhub.com", "Diseño Gráfico y UI", "SkillHub2026*")
        };

        var instructorEntities = new Dictionary<string, Instructor>();

        foreach (var data in instructorsData)
        {
            var user = await userManager.FindByEmailAsync(data.Email);
            if (user is null)
            {
                user = new ApplicationUser
                {
                    UserName = data.Email,
                    Email = data.Email,
                    FullName = data.FullName,
                    EmailConfirmed = true
                };

                var createResult = await userManager.CreateAsync(user, data.Password);
                if (createResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, AppRoles.Employee);
                }
            }

            var instructor = await context.Instructors.FirstOrDefaultAsync(i => i.Email == data.Email);
            if (instructor is null)
            {
                instructor = new Instructor(Guid.NewGuid(), user.Id, data.FullName, data.Email, data.Specialty);
                await context.Instructors.AddAsync(instructor);
            }

            instructorEntities[data.Email] = instructor;
        }

        await context.SaveChangesAsync();

        // 3. Default Seed Participants
        var participantsData = new (string FullName, string Email, string Password)[]
        {
            ("Juan Perez", "juan.perez@example.com", "SkillHub2026*"),
            ("Ana Martinez", "ana.martinez@example.com", "SkillHub2026*"),
            ("Santiago Lopez", "santiago.lopez@example.com", "SkillHub2026*"),
            ("Valentina Castro", "valentina.castro@example.com", "SkillHub2026*")
        };

        foreach (var pData in participantsData)
        {
            var pUser = await userManager.FindByEmailAsync(pData.Email);
            if (pUser is null)
            {
                pUser = new ApplicationUser
                {
                    UserName = pData.Email,
                    Email = pData.Email,
                    FullName = pData.FullName,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(pUser, pData.Password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(pUser, AppRoles.Participant);
                }
            }

            var participant = await context.Participants.FirstOrDefaultAsync(p => p.Email == pData.Email);
            if (participant is null)
            {
                participant = new Participant(Guid.NewGuid(), pUser.Id, pData.FullName, pData.Email);
                await context.Participants.AddAsync(participant);
            }
        }

        await context.SaveChangesAsync();

        // 4. Default Seed Future Workshops
        if (!await context.Workshops.AnyAsync())
        {
            var now = DateTime.UtcNow;

            var workshopsToSeed = new List<Workshop>
            {
                new Workshop(
                    Guid.NewGuid(),
                    "FOT-001",
                    "Fotografía con Smartphones",
                    "Aprende a componer, iluminar y editar fotos profesionales usando solo tu teléfono móvil.",
                    instructorEntities["carlos.mendoza@skillhub.com"].Id,
                    now.AddDays(3).Date.AddHours(9),
                    now.AddDays(3).Date.AddHours(12),
                    15),

                new Workshop(
                    Guid.NewGuid(),
                    "COC-001",
                    "Taller de Pastas Artesanales",
                    "Elaboración de masa fresca, técnicas de amasado y salsas clásicas italianas.",
                    instructorEntities["laura.gomez@skillhub.com"].Id,
                    now.AddDays(5).Date.AddHours(14),
                    now.AddDays(5).Date.AddHours(18),
                    10),

                new Workshop(
                    Guid.NewGuid(),
                    "ORA-001",
                    "Dominio Escénico y Oratoria",
                    "Técnicas de respiración, modulación de voz y cómo hablar en público sin miedo.",
                    instructorEntities["andres.restrepo@skillhub.com"].Id,
                    now.AddDays(7).Date.AddHours(10),
                    now.AddDays(7).Date.AddHours(13),
                    20),

                new Workshop(
                    Guid.NewGuid(),
                    "DIS-001",
                    "Fundamentos de Diseño Visual",
                    "Jerarquía tipográfica, teoría del color y principios de maquetación moderna.",
                    instructorEntities["mariana.rios@skillhub.com"].Id,
                    now.AddDays(10).Date.AddHours(15),
                    now.AddDays(10).Date.AddHours(18),
                    12)
            };

            await context.Workshops.AddRangeAsync(workshopsToSeed);
            await context.SaveChangesAsync();
        }
    }
}
