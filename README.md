# GithubIntegrator

Dos APIs en .NET 9 que integran la API de GitHub:
- **Gh.MinimalApi** (Minimal APIs)
- **Gh.ControllersApi** (Controllers)

Incluyen: JWT, Rate Limiting, Redis (caché distrib.), Swagger, HttpClientFactory + Resilience (oficial .NET), y Docker/Compose.

## Requisitos
- Docker y Docker Compose
- (Opcional) .NET 9 SDK si vas a correr sin contenedores

## Ejecutar con Docker Compose
```bash
docker compose build
docker compose up
```
- Minimal: http://localhost:5080
- Controllers: http://localhost:6080

## Obtener Token
POST `/auth/token` (en ambos proyectos)
Body:
```json
{ "Username": "bernardo" }
```

## Usar Endpoints Protegidos
- `GET /api/v1/github/search?q=aspnet&page=1&pageSize=10`
- `GET /api/v1/github/users/{username}`
- `GET /api/v1/github/users/{username}/repos?page=1&pageSize=10`

Añade encabezado `Authorization: Bearer <token>`.

## Variables de entorno (compose)
- `Jwt__Key` **(cámbiala)**
- `GitHub__Token` (opcional, PAT para más cuota)
- `Redis__ConnectionString` (apunta al servicio `redis` del compose)

## Notas
- Los Dockerfiles son multi-stage.
- Redis expone 6379.
- Rate Limiting por defecto: 100 req / 5 mins.
