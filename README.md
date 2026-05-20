# Sqeez

Sqeez is an open-source, self-hosted educational quiz platform for schools and learning institutions. It provides role-based administration, subject and class management, quiz authoring, student quiz attempts, local media storage, XP rewards, and badge achievements.

The current codebase contains a working ASP.NET Core backend, React frontend, PostgreSQL persistence, generated TypeScript API client, tests, Docker images, and GitHub Actions CI/CD.

## Features

- Role-based users: Student, Teacher, and Admin.
- User authentication with JWT access tokens, refresh-token sessions, and HTTP-only cookies.
- Email verification and password reset support.
- School classes, subjects, and enrollments.
- Admin tools for users, classes, subjects, badges, imports, and system settings.
- Teacher tools for assigned subjects, quiz management, quiz builder, attempts, and manual grading.
- Student quiz player with question transitions, answer recap, media display, and results.
- Quiz questions with text, media, choice answers, strict multiple choice, free-text answers, time limits, difficulty, and optional penalties.
- XP rewards based on improved quiz performance.
- Rule-based badge awarding.
- Local public and private file storage for avatars, badges, and quiz media.
- CSV import for classes, subjects, and students, plus quiz CSV import and export.
- Frontend localization with English and Czech locale files.

## Tech Stack

### Backend

- .NET 10
- ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL with Npgsql
- BCrypt password hashing
- MailKit email integration
- CsvHelper import processing
- Scalar/OpenAPI API reference

### Frontend

- React 19
- TypeScript
- Vite
- TanStack Router
- TanStack Query
- Orval-generated API hooks
- Axios
- Zustand
- Tailwind CSS
- i18next
- Vitest and Testing Library

### Infrastructure

- Docker
- Docker Compose
- Nginx frontend/reverse proxy container with runtime templating
- GitHub Container Registry
- GitHub Actions CI/CD

## Repository Structure

```text
.
+-- analysis/                         Historical analysis and UML artifacts
+-- src/
|   +-- backend/
|   |   +-- Sqeez.Api/                ASP.NET Core API, EF model, services, tests
|   |   |   +-- Samples/              CSV import/export examples
|   +-- frontend/
|   |   +-- sqeez/                    React/Vite frontend
|   +-- docker-compose.dev.yml        Local PostgreSQL and Mailpit services
|   +-- docker-compose.yml            Production-oriented compose file
+-- scripts/
|   +-- setup-vps.sh                  Fresh VPS bootstrap helper
+-- PROJECT_DESCRIPTION.md            Detailed implementation description
+-- RUNNING.md                        Local and Docker running guide
+-- LICENSE.md                        MIT license
+-- README.md
```

## Documentation

- [Project description](PROJECT_DESCRIPTION.md) explains the implemented system, roles, architecture, core workflows, and limitations.
- [Running guide](RUNNING.md) explains how to configure and run the project locally and with Docker.
- [CSV samples](src/backend/Sqeez.Api/Samples/README.md) describe the supported import/export formats and example files.
- `analysis/` contains earlier analysis artifacts. Some of those files are older than the implementation and should be treated as historical context.

## Running, Testing, and Development

Detailed instructions for local development, Docker services, environment variables, database migrations, seeding, test commands, production deployment, and API client generation are kept in [RUNNING.md](RUNNING.md).

The usual local setup runs PostgreSQL and Mailpit from `src/docker-compose.dev.yml`, the backend from `src/backend/Sqeez.Api`, and the frontend from `src/frontend/sqeez`.

## Test Accounts

The hosted demo/test instance can be populated with the following accounts for reviewers and testers. These accounts are not created automatically by the repository.

All listed accounts use the password `Heslo1122*`.

| Role | Email | Intended use |
| --- | --- | --- |
| Admin | `admin.demo@sqeez.org` | User, class, subject, badge, import, and system settings administration. |
| Teacher | `teacher.demo@sqeez.org` | Quiz management, attempt review, manual grading, and statistics. |
| Student | `student.demo@sqeez.org` | Subject browsing, quiz attempts, profile, badges, and leaderboards. |
| Student | `student2.demo@sqeez.org` | Secondary student account for comparing results and leaderboards. |

## License

Sqeez is released under the MIT License. See [LICENSE.md](LICENSE.md).
