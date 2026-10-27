# SkillHub — Workshop & Enrollment Management Platform

## Project Description
**SkillHub** is a training center offering short, in-person workshops across various disciplines including photography, cooking, public speaking, and graphic design.

This solution provides a robust **RESTful API** built with **.NET 10** following **Clean Architecture**, alongside a responsive presentation client for participants built with **Vanilla JavaScript & Bootstrap 5.3**. The system automates workshop registrations, enforces business rules at the domain and application layers (capacity caps, schedule conflict detection, 24-hour cancellation rules, and duplicate check), supports bulk registration via Excel spreadsheets, and issues official PDF enrollment certificates.

---

## Architecture Overview
The backend strictly adheres to **Clean Architecture** with inward-pointing dependencies:

```
src/
├── SkillHub.Domain/         # Core business logic: Entities (Workshop, Enrollment, Instructor, Participant),
│                            # Enums (EnrollmentStatus), Domain Exceptions and Repository Interfaces.
├── SkillHub.Application/    # Orchestration layer: Use cases, DTOs, Application Services (WorkshopService,
│                            # EnrollmentService, ReportService), Interfaces for external infrastructure.
├── SkillHub.Infrastructure/ # Data access & technical adapters: EF Core with PostgreSQL, ASP.NET Core Identity,
│                            # Repositories, QuestPDF certificate generator, ClosedXML Excel bulk reader, Seeders.
├── SkillHub.Api/            # Presentation layer: REST Controllers, JWT Authentication, Global Exception Middleware,
│                            # Swagger UI documentation.
client/                      # Participant web application built with Vanilla JS, HTML5, and Bootstrap 5.3.
```

---

## Technologies Used
- **.NET 10 (C# 14)**
- **ASP.NET Core Web API**
- **Entity Framework Core 10** (Code-First with Migrations)
- **PostgreSQL 16** (hosted in Docker)
- **Npgsql.EntityFrameworkCore.PostgreSQL**
- **ASP.NET Core Identity** (User authentication and role authorization)
- **JWT (JSON Web Tokens)** for stateless endpoint authorization
- **QuestPDF** for automated enrollment certificate generation
- **ClosedXML** for bulk enrollment spreadsheet ingestion
- **Swashbuckle / Swagger UI** for interactive API testing
- **Docker & Docker Compose** for database containerization
- **Bootstrap 5.3 & Vanilla JS** for the responsive client application

---

## Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/)
- [Docker & Docker Compose](https://www.docker.com/)
- Web browser (Chrome, Firefox, Edge)

---

## Setup and Execution Guide

### 1. Start the PostgreSQL Database (Docker)
In the project root, start the customized PostgreSQL container:
```bash
docker compose up -d
```
> The container is configured with:
> - Container name: `skillhub-postgres-haren`
> - Port: `5434:5432`
> - Database: `skillhub_db`
> - User: `haren_admin`
> - Password: `skillharen123`

### 2. Apply Migrations (Database Initialization)
The database schema will automatically migrate on startup, but you can also update it via CLI:
```bash
dotnet ef database update --project src/SkillHub.Infrastructure --startup-project src/SkillHub.Api
```

### 3. Run the Backend API
Start the API on port `5050`:
```bash
dotnet run --project src/SkillHub.Api/SkillHub.Api.csproj --urls "http://localhost:5050"
```
The API automatically seeds initial instructors, participants, and upcoming workshops upon boot.
- **Swagger Documentation:** [http://localhost:5050/swagger](http://localhost:5050/swagger)

### 4. Run the Participant Frontend
In a separate terminal, serve the `client/` directory (e.g. using Python or any static server):
```bash
python3 -m http.server 3000 --directory client
```
- **Web Application URL:** [http://localhost:3000](http://localhost:3000)

---

## Seeded Employee & Instructor Credentials

You can test employee-restricted operations directly in Swagger using these pre-seeded accounts:

| Role | Full Name | Email | Password | Specialty |
|---|---|---|---|---|
| **Employee** | Carlos Mendoza | `carlos.mendoza@skillhub.com` | `SkillHub2026*` | Fotografía Profesional |
| **Employee** | Laura Gomez | `laura.gomez@skillhub.com` | `SkillHub2026*` | Cocina Internacional |
| **Employee** | Andres Restrepo | `andres.restrepo@skillhub.com` | `SkillHub2026*` | Oratoria y Comunicación |
| **Employee** | Mariana Rios | `mariana.rios@skillhub.com` | `SkillHub2026*` | Diseño Gráfico y UI |

### Pre-seeded Participant Accounts:
| Role | Full Name | Email | Password |
|---|---|---|---|
| **Participant** | Juan Perez | `juan.perez@example.com` | `SkillHub2026*` |
| **Participant** | Ana Martinez | `ana.martinez@example.com` | `SkillHub2026*` |

---

## API Endpoints Reference

| Method | Endpoint | Authorization | Description |
|---|---|---|---|
| `POST` | `/api/v1/auth/register` | Public | Register a new participant (always role Participant) |
| `POST` | `/api/v1/auth/login` | Public | Authenticate user and receive JWT Bearer token |
| `GET` | `/api/v1/workshops` | Public | List upcoming workshops with real-time calculated available spots |
| `GET` | `/api/v1/workshops/{id}` | Public | Detailed view of a workshop |
| `POST` | `/api/v1/workshops` | Employee | Create a new workshop |
| `GET` | `/api/v1/workshops/{id}/enrollments` | Employee | List all enrolled participants for a given workshop |
| `POST` | `/api/v1/enrollments` | Participant | Enroll authenticated participant in a workshop |
| `GET` | `/api/v1/enrollments/mine` | Participant | List authenticated participant's active & cancelled enrollments |
| `PATCH` | `/api/v1/enrollments/{id}/cancel` | Owner or Employee | Cancel enrollment (enforces 24-hour limit for participants) |
| `POST` | `/api/v1/enrollments/import` | Employee | Bulk enroll participants from an Excel spreadsheet (.xlsx) |
| `GET` | `/api/v1/enrollments/{id}/certificate` | Owner or Employee | Download official enrollment certificate in PDF format |
| `GET` | `/api/v1/reports/top-workshops` | Employee | Ranking of workshops sorted by active enrollment count |

---

## Database Script Export
The complete database dump containing schema definitions, tables, constraints, indexes, and seeded data is exported at the repository root:
- [`skillhub_dump.sql`](./skillhub_dump.sql)

---

## Author
- **Full Name:** Haren
- **Clan:** Cohorte 5
- **Project:** SkillHub Performance Assessment (.NET)
