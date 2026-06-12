# LicitApp — Backend (.NET 10 + PostgreSQL)

API REST para **LicitApp**, marketplace de licitaciones de materiales de construcción
(provincia del Chaco). Dos roles: **constructor** (publica solicitudes) y **corralón**
(publica ofertas). La autenticación la provee **Firebase Auth**; este backend sólo
**verifica el ID Token** y sirve los datos de negocio desde **PostgreSQL**.

## Stack

| Aspecto        | Tecnología                                                |
|----------------|-----------------------------------------------------------|
| Runtime        | .NET 10 (ASP.NET Core Web API, controllers)               |
| ORM            | EF Core 10 + `Npgsql.EntityFrameworkCore.PostgreSQL`      |
| Base de datos  | PostgreSQL 17 (Docker)                                    |
| Auth           | `JwtBearer` validando Firebase ID Tokens (JWKS de Google) |
| Docs           | Swagger / OpenAPI (en Development)                        |

## Requisitos

- **.NET SDK 10** — verificá con `dotnet --list-sdks`.
  Si no está instalado, sin sudo: `curl -fsSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 10.0`
  y agregá `~/.dotnet` (y `~/.dotnet/tools`) al `PATH`.
- **Docker + Docker Compose** (para PostgreSQL).
- Herramienta EF: `dotnet tool install --global dotnet-ef`

> **Nota sobre permisos de Docker:** si `docker` da *permission denied*, tu usuario no
> está en el grupo `docker`. Solución permanente: `sudo usermod -aG docker $USER` y volvé
> a iniciar sesión. Alternativa puntual: prefijá los comandos con `sudo`.

## Levantar de cero

```bash
# 1) Base de datos
docker compose up -d db
# esperá a que el healthcheck esté "healthy":
docker inspect --format '{{.State.Health.Status}}' licitapp-db

# 2) Aplicar migraciones (crea todas las tablas)
cd LicitApp.Api
dotnet ef database update      # o se aplican solas al arrancar (db.Database.Migrate())

# 3) Correr la API (fuera de Docker, en desarrollo)
dotnet run
# Swagger: http://localhost:<puerto>/swagger   (el puerto se imprime en consola)
# Health:  http://localhost:<puerto>/health
```

### Todo dockerizado (API + DB)

```bash
docker compose --profile api up -d --build
# API en http://localhost:8080  (health: http://localhost:8080/health)
```

## Configuración

`appsettings.json` trae valores de desarrollo listos para usar. Para sobreescribir sin
tocar el repo, usá `appsettings.Development.json` (gitignored), variables de entorno o
`.env` (ver `.env.example`).

| Clave                          | Valor por defecto                                                        |
|--------------------------------|--------------------------------------------------------------------------|
| `ConnectionStrings:Default`    | `Host=localhost;Port=5432;Database=licitapp;Username=licitapp;Password=licitapp_dev` |
| `Firebase:ProjectId`           | `licitapp-e1841`                                                         |
| `Cors:AllowedOrigins`          | `[]` (en Development se permite cualquier origen)                        |

Variables de entorno equivalentes: `ConnectionStrings__Default`, `Firebase__ProjectId`.

## Autenticación

Todos los endpoints (salvo `/health`) requieren `Authorization: Bearer <firebase_id_token>`.
El backend valida emisor (`https://securetoken.google.com/licitapp-e1841`), audiencia
(`licitapp-e1841`), firma (JWKS público de Google) y expiración. El **UID** del usuario
es el claim `sub`/`user_id` y es la PK de `users`.

El **registro/login** ocurre en Firebase (front). Tras loguearse, el front llama a
`POST /api/users/sync` con un token válido para crear/actualizar el perfil en PostgreSQL.

### Probar con un token real

```bash
TOKEN="<firebase_id_token>"
BASE="http://localhost:5080"   # ajustá al puerto que imprime dotnet run

# Sin token -> 401
curl -i $BASE/api/users/me

# Crear/actualizar perfil
curl -s -X POST $BASE/api/users/sync -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"role":"constructor","fullName":"Juan Pérez","phone":"3624000000","zone":"Resistencia, Chaco"}'

# Mi perfil (incluye stats)
curl -s $BASE/api/users/me -H "Authorization: Bearer $TOKEN"
```

## Modelo de dominio

`users` (PK = Firebase UID) · `solicitudes` (1→N `materiales`, 1→N `ofertas`) ·
`ofertas` · `notifications`. Enums guardados como **texto** (`varchar(32)`). Fechas en
**UTC** (`timestamptz`); un `JsonConverter` normaliza todo `DateTime` entrante a UTC.
`stats` se modela como *owned type* → columnas `stats_total_licitaciones`,
`stats_total_ofertas`, `stats_total_cierres`.

## Endpoints

### Usuarios
| Método | Ruta              | Descripción                                  |
|--------|-------------------|----------------------------------------------|
| POST   | `/api/users/sync` | Upsert del perfil del usuario autenticado.   |
| GET    | `/api/users/me`   | Perfil (incluye `stats`).                    |
| PUT    | `/api/users/me`   | Actualizar perfil (incluye `pushToken`).     |

### Solicitudes
| Método | Ruta                                  | Descripción                              |
|--------|---------------------------------------|------------------------------------------|
| POST   | `/api/solicitudes`                    | Crear solicitud + materiales (constructor). |
| GET    | `/api/solicitudes/mine`               | Mis solicitudes, `createdAt desc`.       |
| GET    | `/api/solicitudes/{id}`               | Detalle con `materiales`.                |
| GET    | `/api/solicitudes?zone=&status=OPEN`  | Feed del corralón, `deadline asc`.       |

### Ofertas
| Método | Ruta                                                | Descripción                       |
|--------|-----------------------------------------------------|-----------------------------------|
| POST   | `/api/solicitudes/{id}/ofertas`                     | Crear oferta (corralón).          |
| GET    | `/api/solicitudes/{id}/ofertas`                     | Ofertas `ACTIVE`, `totalPrice asc`. |
| GET    | `/api/solicitudes/{id}/ofertas/resumen`             | `{ count, bestPrice }`.           |
| GET    | `/api/ofertas/mine`                                 | Mis ofertas, `createdAt desc`.    |
| GET    | `/api/solicitudes/{id}/ofertas/{ofertaId}`          | Una oferta.                       |
| PUT    | `/api/solicitudes/{id}/ofertas/{ofertaId}`          | Editar oferta.                    |
| POST   | `/api/solicitudes/{id}/ofertas/{ofertaId}/withdraw` | Retirar (`WITHDRAWN`).            |
| POST   | `/api/solicitudes/{id}/accept`                      | Aceptar ganadora (body `ofertaId`). |

### Notificaciones / Health
| Método | Ruta                            | Descripción                |
|--------|---------------------------------|----------------------------|
| GET    | `/api/notifications`            | Del usuario autenticado.   |
| POST   | `/api/notifications/{id}/read`  | Marcar como leída.         |
| GET    | `/health`                       | Liveness (sin auth).       |

## Reglas de negocio (transaccionales)

Todas se ejecutan dentro de una transacción EF Core (`BeginTransactionAsync`):

1. **Crear solicitud** — crea solicitud `OPEN` + materiales; incrementa `stats.totalLicitaciones`.
2. **Crear oferta** — exige solicitud `OPEN` (sino `409`); recalcula `isBestPrice`
   (badge único para la oferta `ACTIVE` más barata); `isFastDelivery = deliveryHours <= 24`;
   incrementa `ofertasCount` y `stats.totalOfertas`.
3. **Aceptar oferta** — sólo el constructor dueño y con solicitud `OPEN`; ganadora `WON`,
   resto `LOST`, solicitud `CLOSED`, `winningOfferId` seteado; incrementa
   `stats.totalCierres` del constructor **y** del corralón ganador.
4. **Editar oferta** — sólo si la solicitud sigue `OPEN` y el corralón es dueño; recalcula badge.
5. **Retirar oferta** — `WITHDRAWN`; sólo el dueño; recalcula badge.
6. **Resumen** — `{ count, bestPrice }` sobre las `ACTIVE`.

### Notificaciones (generadas dentro de la transacción del evento)

Cada notificación se persiste de forma **atómica** junto con la operación que la dispara:

| Evento                | Tipo           | Destinatario(s)                              |
|-----------------------|----------------|----------------------------------------------|
| Crear solicitud       | `NEW_REQUEST`  | Todos los corralones de la `deliveryZone` (setea `corralonesNotifiedCount`). |
| Crear oferta          | `NEW_OFFER`    | El constructor dueño de la solicitud.        |
| Aceptar oferta        | `OFFER_WON`    | El corralón ganador.                         |
| Aceptar oferta        | `OFFER_LOST`   | Cada corralón con oferta no seleccionada.    |

**`DEADLINE_NEAR`** se genera con un **job en background** (`DeadlineNearService`,
`BackgroundService` con `PeriodicTimer`): cada `IntervalMinutes` barre las solicitudes
`OPEN` cuyo `deadline` cae dentro de `WindowHours` y todavía no tienen un `DEADLINE_NEAR`,
y notifica al constructor dueño. El barrido es **idempotente** (un `NOT EXISTS` evita
duplicados entre corridas). Configurable en `appsettings.json`:

```json
"Notifications": {
  "DeadlineNear": { "Enabled": true, "IntervalMinutes": 60, "WindowHours": 24 }
}
```

> El **envío** real (Expo Push) queda fuera de alcance: por ahora sólo se **persisten** las
> `Notification` y el front las consume vía `GET /api/notifications`.

> **Decisiones documentadas:**
> - `solicitudTitle` / `solicitudDeadline` (campos denormalizados que el front consume en
>   listas del corralón) **no se persisten**: se exponen en el DTO de Oferta vía *join* con
>   la Solicitud en `GET /api/ofertas/mine`.
> - El badge `isBestPrice` se recalcula de forma completa (única oferta `ACTIVE` más barata,
>   desempate por la más reciente) en cada create/edit/withdraw, lo que es equivalente a la
>   regla incremental del front y robusto ante ediciones y retiros.
> - `CorralonName` se toma de `BusinessName` (o `FullName` si no hay) del perfil.

## Errores

`401` sin token válido · `403` rol incorrecto / no es dueño · `404` recurso inexistente ·
`409` conflicto de estado (licitación cerrada/no activa) · `400` validación. Las respuestas
de error usan `ProblemDetails`.

## Estructura

```
LicitApp.Api/
├── Program.cs                # wiring: EF, JwtBearer, CORS, Swagger, migrate al arranque
├── Domain/                   # entidades + enums
├── Data/                     # AppDbContext + design-time factory
├── Dtos/                     # DTOs + mapeos
├── Services/                 # lógica de negocio + transacciones (+ AppException)
├── Controllers/
├── Auth/                     # claims helpers + middleware de errores
├── Json/                     # UtcDateTimeConverter
└── Migrations/               # generadas por EF (InitialCreate)
docker-compose.yml · Dockerfile · .env.example
```

## Fuera de alcance (puntos de extensión)

- **Tiempo real** (SignalR) — el front puede hacer polling/refetch por ahora.
- **Storage de adjuntos** — el front sigue subiendo a Firebase Storage y manda `attachmentUrl`.
- **Push notifications** — las `Notification` se persisten; el envío vía Expo queda para después.
