# Docker Setup for Vector Platform

This directory contains Docker configuration files for local development.

## Files

- `Dockerfile.backend` - Backend .NET 8.0 API container
- `Dockerfile.frontend` - Frontend React application container
- `docker-compose.yml` - Complete stack with PostgreSQL, Redis, Backend, and Frontend
- `nginx.conf` - Nginx configuration for frontend

## Prerequisites

- Docker Desktop installed and running
- Docker Compose (included with Docker Desktop)

## Usage

### Start All Services

```bash
cd docker
docker-compose up -d
```

This will start:
- PostgreSQL on port 5432
- Redis on port 6379
- Backend API on port 5000
- Frontend on port 3000

### View Logs

```bash
docker-compose logs -f
```

### View Specific Service Logs

```bash
docker-compose logs -f backend
docker-compose logs -f frontend
```

### Stop All Services

```bash
docker-compose down
```

### Stop and Remove Volumes

```bash
docker-compose down -v
```

### Rebuild Containers

```bash
docker-compose build --no-cache
docker-compose up -d
```

## Environment Variables

Create a `.env` file in the `docker` directory (optional):

```env
JWT_SECRET=your-super-secret-key-change-in-production
JWT_ISSUER=Vector
JWT_AUDIENCE=Vector
```

## Database Access

Connect to PostgreSQL:
```bash
docker exec -it vector-postgres psql -U postgres -d vector_db
```

Connect to Redis:
```bash
docker exec -it vector-redis redis-cli
```

## Troubleshooting

### Port Already in Use

If ports 5432, 6379, 5000, or 3000 are already in use, modify the port mappings in `docker-compose.yml`.

### Database Connection Issues

Ensure the backend container waits for PostgreSQL to be healthy. The `depends_on` configuration handles this.

### Frontend Can't Connect to Backend

Update the `VITE_API_URL` in your frontend `.env` file to point to `http://localhost:5000/api`.

## Development Workflow

1. Start services: `docker-compose up -d`
2. Make code changes
3. Rebuild affected services: `docker-compose build backend` (or `frontend`)
4. Restart service: `docker-compose restart backend` (or `frontend`)

For hot-reload during development, it's recommended to run the services locally instead of in Docker.

