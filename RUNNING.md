# Running Sqeez

This guide explains how to run Sqeez locally for development and how the production-oriented Docker setup is intended to work.

## Requirements

Install:

- .NET 10 SDK
- Node.js 22
- Yarn
- Docker or another PostgreSQL 18-compatible local database
- EF Core CLI tool, if you want to run migrations from the terminal

Install the EF Core CLI if needed:

```powershell
dotnet tool install --global dotnet-ef
```

## Local Development

The recommended local setup is:

- PostgreSQL and Mailpit in Docker.
- Backend from source using the HTTP launch profile.
- Frontend from source using Vite.

### 1. Start Local Services

Start PostgreSQL and Mailpit using docker-compose:

```powershell
docker compose -f src/docker-compose.dev.yml up -d
```

This starts:
- **PostgreSQL** on `localhost:5432`
- **Mailpit SMTP** on `localhost:1025`
- **Mailpit Web UI** on `http://localhost:8025`

### 2. Configure Backend Environment

Create `src/backend/Sqeez.Api/.env` from `src/backend/Sqeez.Api/.env.example`.

For local development, the important values are:

```dotenv
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=SqeezDb;Username=postgres;Password=TodoSecurePassword
TokenKey=This_Is_My_Super_Secret_Key_That_Must_Be_At_Least_64_Characters_Long_123456789!
FrontendUrl=http://localhost:3000

SUPER_USER_EMAIL=test@example.com
SUPER_USER_DEFAULT_PASSWORD=YourSuperSecretPassword123!
SUPER_USER_FIRST_NAME=System
SUPER_USER_LAST_NAME=Administrator

SmtpSettings__Server=localhost
SmtpSettings__Port=1025
SmtpSettings__SenderName=Sqeez App
SmtpSettings__SenderEmail=noreply@sqeez.org
SmtpSettings__Username=
SmtpSettings__Password=
SmtpSettings__UseStartTls=false
```

### 3. Local Registration & Email Verification
When you register a user locally, the backend sends a verification email. 
1. Open the Mailpit Web UI at **`http://localhost:8025`**.
2. Find the verification email in your inbox.
3. Click the verification link/button inside the email to verify and activate the account.

### 4. Apply Database Migrations

```powershell
cd src/backend/Sqeez.Api
dotnet restore
dotnet ef database update
```

### 5. Required Bootstrap Seed

The backend includes a seed mode for required bootstrap data. It is idempotent and can be run after every deployment. It ensures:

- the singleton system configuration row exists
- the configured super admin account exists

```powershell
cd src/backend/Sqeez.Api
dotnet run -- seed
```

For local demo data on an empty database, use:

```powershell
dotnet run -- seed-demo
```

Demo seed data includes an admin, teachers, students, classes, subjects, enrollments, sample quizzes, sample media, and badges.

### 6. Run Backend

Use the HTTP launch profile:

```powershell
cd src/backend/Sqeez.Api
dotnet run --launch-profile http
```

Backend URLs:

- `http://localhost:5000`

Development API docs:

- OpenAPI: `http://localhost:5000/openapi/v1.json`
- Scalar: `http://localhost:5000/scalar/v1`

### 7. Configure Frontend Environment

Create `src/frontend/sqeez/.env` from `src/frontend/sqeez/.env.example`.

Recommended local values:

```dotenv
VITE_API_BASE_URL=http://localhost:5000
VITE_PORT=3000
```

### 8. Run Frontend

```powershell
cd src/frontend/sqeez
yarn install
yarn dev
```

Open:

```text
http://localhost:3000
```

## Login Notes

Public registration is controlled by system configuration. If public registration is disabled, create users through CSV import or the admin interface after logging in with the seeded super admin.

The seeded super admin email and password come from:

```dotenv
SUPER_USER_EMAIL
SUPER_USER_DEFAULT_PASSWORD
SUPER_USER_FIRST_NAME
SUPER_USER_LAST_NAME
SUPER_USER_DEPARTMENT
SUPER_USER_PHONE_NUMBER
```

Only the email and password are required. The other super admin profile fields are optional.

## Frontend API Client Generation

The frontend uses generated React Query hooks from the OpenAPI schema.

Generate the client:

```powershell
cd src/frontend/sqeez
yarn api:gen
```

The generator reads:

```text
src/frontend/sqeez/src/api/api.yaml
```

and writes generated files under:

```text
src/frontend/sqeez/src/api/generated
```

## Tests

Backend tests:

```powershell
cd src/backend/Sqeez.Api
dotnet test Sqeez.Api.Tests/Sqeez.Api.Tests.csproj
```

PostgreSQL integration tests require Docker and the `SQEEZ_RUN_POSTGRES_TESTS` environment variable:

```powershell
cd src/backend/Sqeez.Api
$env:SQEEZ_RUN_POSTGRES_TESTS="true"
dotnet test Sqeez.Api.Tests/Sqeez.Api.Tests.csproj --filter FullyQualifiedName~Postgres
```

Frontend tests:

```powershell
cd src/frontend/sqeez
yarn test --run
```

Frontend coverage:

```powershell
cd src/frontend/sqeez
yarn test:coverage
```

Frontend production build:

```powershell
cd src/frontend/sqeez
yarn build
```

Backend production build:

```powershell
cd src/backend/Sqeez.Api
dotnet publish -c Release
```

## Docker Compose

The compose file at `src/docker-compose.yml` is production-oriented. It expects prebuilt backend and frontend images from GitHub Container Registry:

- `ghcr.io/${GHCR_OWNER}/sqeez-backend:latest`
- `ghcr.io/${GHCR_OWNER}/sqeez-frontend:latest`

It runs:

- PostgreSQL.
- Backend API.
- Frontend Nginx container.

For a production server, the repository does not need to be cloned. The runtime directory only needs:

```text
+-- docker-compose.yml
+-- .env
```

The current CD workflow uses `/root/Sqeez` as that runtime directory. It copies the latest `docker-compose.yml` from the repository during deployment, while `.env` remains a server-local secret file.

For manual setup, copy `src/docker-compose.yml` to the server runtime directory and create `.env` next to it. The repository includes `src/.env.example` as a starting point. Example:

```dotenv
GHCR_OWNER=your-github-owner

POSTGRES_USER=postgres
POSTGRES_PASSWORD=TodoSecurePassword
POSTGRES_DB=SqeezDb

ConnectionStrings__DefaultConnection=Host=sqeez-postgres;Port=5432;Database=SqeezDb;Username=postgres;Password=TodoSecurePassword
TokenKey=This_Is_My_Super_Secret_Key_That_Must_Be_At_Least_64_Characters_Long_123456789!
FrontendUrl=https://your-domain.example

NGINX_SERVER_NAME=your-domain.example
NGINX_SSL_CERTIFICATE=/etc/letsencrypt/live/your-domain.example/fullchain.pem
NGINX_SSL_CERTIFICATE_KEY=/etc/letsencrypt/live/your-domain.example/privkey.pem
NGINX_CERTIFICATE_HOST_PATH=/etc/letsencrypt
NGINX_BACKEND_URL=http://backend:8080
NGINX_CLIENT_MAX_BODY_SIZE=100m

SUPER_USER_EMAIL=admin@example.com
SUPER_USER_DEFAULT_PASSWORD=ChangeThisPassword123!
SUPER_USER_FIRST_NAME=System
SUPER_USER_LAST_NAME=Administrator
SUPER_USER_DEPARTMENT=Administration
SUPER_USER_PHONE_NUMBER=

SmtpSettings__Server=smtp.example.com
SmtpSettings__Port=587
SmtpSettings__SenderName=Sqeez App
SmtpSettings__SenderEmail=noreply@example.com
SmtpSettings__Username=your_smtp_username
SmtpSettings__Password=your_smtp_password
```

Then run:

```powershell
cd /root/Sqeez
docker compose up -d
```

The frontend Nginx config is generated from `src/frontend/sqeez/nginx.conf.template` when the container starts. The Docker image stays generic; domain and certificate values come from `.env`.

Default compose values still target `sqeez.org`:

```dotenv
NGINX_SERVER_NAME=sqeez.org
NGINX_SSL_CERTIFICATE=/etc/letsencrypt/live/sqeez.org/fullchain.pem
NGINX_SSL_CERTIFICATE_KEY=/etc/letsencrypt/live/sqeez.org/privkey.pem
NGINX_CERTIFICATE_HOST_PATH=/etc/letsencrypt
```

For another domain, change those values in the VPS `.env` and make sure the referenced certificate files exist on the host. `NGINX_CERTIFICATE_HOST_PATH` is mounted into the frontend container at `/etc/letsencrypt`.

## Fresh VPS Bootstrap Script

For a brand-new Ubuntu/Debian VPS, use `scripts/setup-vps.sh` as a one-time bootstrap helper. It installs required packages, installs or reuses Docker Engine with the Docker Compose plugin, creates the Let's Encrypt certificate, prepares `/root/Sqeez`, downloads the compose file, writes `.env`, starts PostgreSQL, waits until it is ready, generates and applies migrations, runs the required bootstrap seed, and starts the full stack.

Before running it, edit the configuration block at the top of the script:

```bash
DOMAIN="sqeez.yourdomain.com"
LETSENCRYPT_EMAIL="your-email@example.com"
GHCR_OWNER="Denkomasta"
GITHUB_REPO="https://github.com/Denkomasta/Sqeez.git"
DB_PASS="TodoSecurePassword"
JWT_SECRET="This_Is_My_Super_Secret_Key_That_Must_Be_At_Least_64_Characters_Long_123456789!"
SUPER_USER_EMAIL="admin@example.com"
SUPER_USER_DEFAULT_PASSWORD="ChangeThisPassword123!"
SUPER_USER_FIRST_NAME="System"
SUPER_USER_LAST_NAME="Administrator"
```

Then copy it to the VPS and run:

```bash
chmod +x setup-vps.sh
sudo ./setup-vps.sh
```

The script uses `pg_isready` instead of a fixed sleep when waiting for PostgreSQL. If GHCR packages are private, fill `GHCR_USERNAME` and `GHCR_TOKEN` in the script before running it.

## Deployment Workflow

The GitHub Actions deployment workflow:

1. Runs after successful CI on `main`.
2. Builds backend and frontend Docker images.
3. Pushes images to GitHub Container Registry.
4. Generates an idempotent EF migration script from the same commit being deployed.
5. Ensures `/root/Sqeez` exists on the server.
6. Copies `docker-compose.yml` and the temporary migration script to `/root/Sqeez`.
7. Reads database credentials from the server's `.env`.
8. Pulls the latest container images.
9. Starts PostgreSQL.
10. Applies migrations with `psql`.
11. Removes the temporary migration script.
12. Runs the required bootstrap seed to ensure system config and the super admin exist.
13. Starts the full stack with Docker Compose.

Manual bootstrap seeding is also available through the `Manual Database Seed` workflow.
