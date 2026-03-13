# Vector Developer Guide

## Project Structure

```
Vector/
├── backend/Vector.Api/       # .NET 8 Web API
├── frontend/                 # React + TypeScript + Vite
├── docker/                   # Docker Compose for local dev
└── docs/                     # Documentation
```

## Local Development

### Prerequisites
- .NET 8 SDK
- Node.js 20+
- Docker Desktop (for Postgres, Redis, MinIO)

### Run with Docker
```bash
cd docker
docker compose up -d
```
- Frontend: http://localhost:3000
- Backend: http://localhost:5000
- Swagger: http://localhost:5000/swagger
- MinIO Console: http://localhost:9001

### Run Locally (without Docker)
1. Start Postgres and Redis
2. Set connection strings in `appsettings.Development.json`
3. `cd backend/Vector.Api && dotnet run`
4. `cd frontend && npm run dev`

---

## Code Execution

### Judge0 Integration
- Code execution uses Judge0 API (configured in `Judge0__BaseUrl`)
- Sandboxed execution with timeout (5s CPU) and memory limits (128MB)
- Supported: Python, JavaScript, Java, C++, C#, Go

### Adding a New Language
1. Add language ID to `CodeExecutionService` / Judge0 language list
2. Add code template in `frontend/src/utils/questionTemplates.ts`
3. Add wrapper logic in `CodeWrapperService` if custom structure conversion needed

### Test Case Format
- Stored as JSON: `{"param1": value, "param2": value}`
- Line-based input: one value per line for simple cases
- Linked list/tree questions require conversion layer (see Known Bugs)

---

## File Uploads

### S3-Compatible Storage
- MinIO for local Docker; Cloudflare R2 or AWS S3 for production
- Folders: `profile-pictures`, `coach-applications`, `question-videos`, `dashboard-videos`
- `S3Service` centralizes upload/delete logic

### Multipart Upload
- Use `FormData` in frontend; do not set `Content-Type` (browser adds boundary)
- Backend: `IFormFile` with `[FromForm]`

---

## Database Migrations

```bash
cd backend/Vector.Api
dotnet ef migrations add MigrationName
dotnet ef database update
```
Migrations run automatically on container startup.

---

## Testing

```bash
# Backend (364+ tests)
dotnet test backend/Vector.Api.Tests

# Frontend
cd frontend && npm test
```

---

## Deployment

- **CI/CD:** GitHub Actions (`.github/workflows/`)
- **QA:** Push to `develop` → deploys to Render QA
- **Prod:** Push to `main` → deploys to Render Production
- **Config:** `render.yaml` defines services
