# Game API - ASP.NET

API REST desarrollada en ASP.NET Core 8.0 para gestionar usuarios y partidas de un juego (Tic-Tac-Toe) usando MongoDB.

## Características

- Registro y gestión de usuarios (nombre, email, password)
- Gestión de partidas de juego con estado JSON
- Autenticación con contraseñas hasheadas (BCrypt)
- Validación de datos
- Documentación interactiva con Swagger
- Arquitectura REST completa
- Esquema de base de datos versionado (validators `$jsonSchema` + índices)
- Migraciones idempotentes ejecutables contra MongoDB

## Pre-requisitos

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download) (para ejecución local)
- [MongoDB](https://www.mongodb.com/try/download/community) (local o en cloud)
- [MongoDB Shell (`mongosh`)](https://www.mongodb.com/try/download/shell) (para ejecutar migraciones)
- [Docker](https://docs.docker.com/get-docker/) y [Docker Compose](https://docs.docker.com/compose/install/) (para ejecución containerizada)

## Ejecución con Docker (recomendada)

El proyecto incluye un `Dockerfile` multi-stage y un `docker-compose.yml` que levantan la API y MongoDB en contenedores separados, listos para funcionar.

### 1. Levantar los servicios

Desde la raíz del proyecto (donde está `docker-compose.yml`), ejecuta:

```bash
docker compose up --build
```

Si tu versión de Docker es antigua y usas `docker-compose` (v1), el comando es:

```bash
docker-compose up --build
```

Esto:

- Construye la imagen de la API a partir del `Dockerfile`.
- Descarga la imagen oficial de MongoDB 7.0.
- Crea una red interna donde la API se conecta a MongoDB usando el hostname `mongodb`.
- Espera a que MongoDB esté saludable (healthcheck) antes de iniciar la API.
- Ejecuta automáticamente los scripts de `Scripts/migrations/` la primera vez que se inicializa el volumen (vía `/docker-entrypoint-initdb.d`).

### 2. Verificar el estado

En otra terminal:

```bash
docker compose ps
```

Deberías ver ambos contenedores como `Up` (y `healthy` para MongoDB):

```text
NAME           IMAGE         COMMAND                  SERVICE   STATUS
game-api       gameapi-api   "dotnet GameAPI.dll"     api       Up
game-mongodb   mongo:7.0     "docker-entrypoint.s…"   mongodb   Up (healthy)
```

### 3. Acceder a la aplicación

- API: `http://localhost:5000`
- Swagger UI: `http://localhost:5000/swagger`

Prueba un endpoint rápido:

```bash
curl http://localhost:5000/api/users
```

### 4. Ver logs

```bash
# Logs de la API
docker compose logs -f api

# Logs de MongoDB
docker compose logs -f mongodb

# Logs de ambos
docker compose logs -f
```

### 5. Detener los servicios

```bash
docker compose down
```

Esto detiene y elimina los contenedores, pero conserva los volúmenes (los datos de MongoDB no se pierden).

Si además quieres borrar los datos de MongoDB:

```bash
docker compose down -v
```

### Notas sobre la configuración Docker

- La conexión a MongoDB se configura mediante variables de entorno en `docker-compose.yml`:
    - `MongoDBSettings__ConnectionString=mongodb://mongodb:27017`
    - `MongoDBSettings__DatabaseName=GameDB`
- Se añadió `DOTNET_SYSTEM_NET_DISABLEIPV6=1` y `links: - mongodb` para evitar problemas de resolución DNS/IPv6 en algunos entornos Linux (error `Resource temporarily unavailable`).
- El healthcheck de MongoDB asegura que la API no intente conectarse hasta que la base de datos esté lista.
- Los datos de MongoDB se persisten en los volúmenes `mongodb_data` y `mongodb_config`.
- La carpeta `Scripts/migrations/` se monta como volumen de solo lectura en `/docker-entrypoint-initdb.d`, por lo que los scripts se ejecutan automáticamente en el primer arranque.

## Configuración Local (sin Docker)

Si prefieres ejecutar la API directamente con .NET:

1. Clonar o descargar el repositorio.

2. Configurar MongoDB. Editar el archivo `appsettings.json` con la cadena de conexión:

   ```json
   {
     "MongoDBSettings": {
       "ConnectionString": "mongodb://localhost:27017",
       "DatabaseName": "GameDB"
     }
   }
   ```

3. Restaurar paquetes:

   ```bash
   dotnet restore
   ```

4. Ejecutar la aplicación:

   ```bash
   dotnet run
   ```

Direcciones de la API:

- HTTPS: `https://localhost:58333`
- HTTP: `http://localhost:58334`
- Swagger UI: `https://localhost:58333/swagger`

La API incluye un `IHostedService` (`DatabaseInitializer`) que asegura el esquema (colecciones, validators e índices) al arrancar. No es obligatorio ejecutar la migración `.js` a mano para desarrollo local, pero sí se recomienda para entornos productivos o para migrar datos existentes.

## Esquema de Base de Datos

Base de datos: `GameDB`

### Colección `users`

| Campo           | Tipo     | Requerido | Notas                                  |
|-----------------|----------|-----------|----------------------------------------|
| `_id`           | ObjectId | ✅ (auto) | Generado por MongoDB                   |
| `username`      | string   | ✅        | 3–50 caracteres, índice único          |
| `email`         | string   | ✅        | Índice único, se almacena en minúsculas |
| `password_hash` | string   | ✅        | Hash BCrypt (mín. 20 caracteres)       |
| `createdDate`   | date     | ✅        | UTC                                    |

Índices:

- `ux_users_email` → `{ email: 1 }` (único)
- `ux_users_username` → `{ username: 1 }` (único)

### Colección `matches`

| Campo         | Tipo            | Requerido | Notas                                  |
|---------------|-----------------|-----------|----------------------------------------|
| `_id`         | ObjectId        | ✅ (auto) | Generado por MongoDB                   |
| `player1`     | ObjectId/string | ✅        | Jugador anfitrión (host)               |
| `player2`     | ObjectId/string | ✅        | Jugador invitado (guest)               |
| `board_state` | string          | ✅        | Estado del tablero serializado como JSON |
| `status`      | enum            | ✅        | `in_progress` \| `finished` \| `abandoned` |
| `playerTurn`  | ObjectId/string | ❌        | Jugador con el turno actual            |
| `matchResult` | ObjectId/null   | ❌        | Ganador de la partida                  |
| `createdDate` | date            | ✅        | UTC                                    |
| `updatedDate` | date            | ✅        | UTC                                    |

Índices:

- `ix_matches_player1` → `{ player1: 1 }`
- `ix_matches_player2` → `{ player2: 1 }`
- `ix_matches_status` → `{ status: 1 }`
- `ix_matches_players_status` → `{ player1: 1, player2: 1, status: 1 }`

### Validators

Ambas colecciones tienen un `$jsonSchema` aplicado con:

- `validationLevel: "moderate"` → valida inserts y updates sobre documentos existentes que ya cumplen; no bloquea docs viejos inválidos.
- `validationAction: "error"` → rechaza escrituras que no cumplan el esquema.

## Migraciones

Los scripts de migración están en `Scripts/migrations/` y se ejecutan contra MongoDB usando `mongosh`. Son **idempotentes**: se pueden ejecutar múltiples veces sin efectos adversos.

### ¿Qué hace la migración `001_init_schema.js`?

**Fase 1 — Migración de datos legacy** (si existían colecciones con nombres/campos antiguos):

- Renombra `Users` → `users` y `Gameplays` → `matches`.
- Renombra campos: `name` → `username`, `password` → `password_hash`, `registerDate` → `createdDate`.
- Renombra campos en `matches`: `currentPositions` → `board_state`, `hostPlayer` → `player1`, `guestPlayer` → `player2`.
- Deriva el campo `status` a partir de `matchResult`.

**Fase 2 — Aplica los validators** `$jsonSchema` a `users` y `matches`.

**Fase 3 — Crea los índices** necesarios (únicos y compuestos).

### Ejecutar la migración

**Opción A — Contra MongoDB local:**

```bash
mongosh "mongodb://localhost:27017/GameDB" Scripts/migrations/001_init_schema.js
```

**Opción B — Contra el contenedor Docker ya levantado:**

```bash
docker exec -i game-mongodb mongosh "mongodb://localhost:27017/GameDB" \
  < Scripts/migrations/001_init_schema.js
```

**Opción C — Automático en el primer arranque con Docker Compose:**

El `docker-compose.yml` monta `Scripts/migrations/` en `/docker-entrypoint-initdb.d`. La imagen oficial de MongoDB ejecuta todos los `*.js` de esa carpeta **solo la primera vez** que se inicializa el volumen `mongodb_data`. Si necesitas volver a ejecutarla:

```bash
# Borra el volumen de datos y vuelve a levantar
docker compose down -v
docker compose up --build
```

O bien, usa la Opción B sobre el contenedor ya existente.

### Verificar el resultado

```bash
docker exec -it game-mongodb mongosh "mongodb://localhost:27017/GameDB" --eval "
  print('Colecciones: ' + db.getCollectionNames());
  print('--- Índices users ---'); printjson(db.users.getIndexes());
  print('--- Índices matches ---'); printjson(db.matches.getIndexes());
"
```

Deberías ver `["matches","users"]` y los índices listados.

### Añadir una nueva migración

1. Crea un nuevo archivo con el siguiente número secuencial, p. ej. `Scripts/migrations/002_add_field.js`.
2. Usa el mismo patrón: `db.getSiblingDB("GameDB")`, idempotencia y `print()` para trazabilidad.
3. Ejecuta con cualquiera de las opciones A, B o C.

## Endpoints REST API

### Users

#### POST /api/users

Registrar un nuevo usuario

Request Body:

```json
{
  "name": "Jhon Doe",
  "email": "jdoe@mail.com",
  "password": "miPassword123"
}
```

Response (201 Created):

```json
{
  "id": "679930f8e9a1234567890abc",
  "name": "Jhon Doe",
  "email": "jdoe@mail.com",
  "createdDate": "2026-01-28T04:00:00Z"
}
```

#### GET /api/users

Obtener todos los usuarios

Response (200 OK):

```json
[
  {
    "id": "679930f8e9a1234567890abc",
    "name": "Jhon Doe",
    "email": "jdoe@mail.com",
    "createdDate": "2026-01-28T04:00:00Z"
  }
]
```

#### GET /api/users/{id}

Obtener un usuario por ID

Response (200 OK):

```json
{
  "id": "679930f8e9a1234567890abc",
  "name": "Jhon Doe",
  "email": "jdoe@mail.com",
  "createdDate": "2026-01-28T04:00:00Z"
}
```

#### PUT /api/users/{id}

Actualizar un usuario

Request Body:

```json
{
  "name": "Jhony Doe",
  "password": "miSecurePassword123"
}
```

Response (204 No Content)

#### DELETE /api/users/{id}

Eliminar un usuario

Response (204 No Content)

### Partidas

#### POST /api/gameplays

Crear una nueva partida

Request Body:

```json
{
  "currentPositions": "{\"hostPlayerMoves\": [], \"guestPlayerMoves\": [], \"playerTurnId\": \"679930f8e9a1234567890def\", \"winnerLine\": []}",
  "hostPlayer": "679930f8e9a1234567890abc",
  "guestPlayer": "679930f8e9a1234567890def",
  "playerTurn": "679930f8e9a1234567890abc",
  "matchResult": null
}
```

Response (201 Created):

```json
{
  "id": "679931f8e9a1234567890xyz",
  "currentPositions": "{\"hostPlayerMoves\": [], \"guestPlayerMoves\": [], \"playerTurnId\": \"679930f8e9a1234567890def\", \"winnerLine\": []}",
  "hostPlayer": "679930f8e9a1234567890abc",
  "guestPlayer": "679930f8e9a1234567890def",
  "playerTurn": "679930f8e9a1234567890abc",
  "matchResult": null,
  "createdDate": "2026-01-28T04:00:00Z",
  "updatedDate": "2026-01-28T04:00:00Z"
}
```

#### GET /api/gameplays

Obtener todas las partidas

#### GET /api/gameplays/{id}

Obtener una partida por ID

Response (200 OK):

```json
{
  "id": "679931f8e9a1234567890xyz",
  "currentPositions": "{\"hostPlayerMoves\": [2,1,9], \"guestPlayerMoves\": [7,5,3], \"playerTurnId\": \"679930f8e9a1234567890def\", \"winnerLine\": [1,4,7]}",
  "hostPlayer": "679930f8e9a1234567890abc",
  "guestPlayer": "679930f8e9a1234567890def",
  "playerTurn": "679930f8e9a1234567890abc",
  "matchResult": null,
  "createdDate": "2026-01-28T04:00:00Z",
  "updatedDate": "2026-01-28T04:00:00Z"
}
```

#### GET /api/gameplays/jugador/{jugadorId}

Obtener todas las partidas de un jugador

#### PUT /api/gameplays/{id}

Actualizar una partida

Request Body:

```json
{
  "currentPositions": "{\"hostPlayerMoves\": [2,1,9], \"guestPlayerMoves\": [7,5,3], \"playerTurnId\": \"679930f8e9a1234567890def\", \"winnerLine\": [1,4,7]}",
  "playerTurn": "679930f8e9a1234567890def",
  "resultado": "679930f8e9a1234567890def"
}
```

#### DELETE /api/gameplays/{id}

Eliminar una partida

## Pruebas con cURL

### Registrar usuario

```bash
curl -X POST http://localhost:5000/api/users \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Sandra Dee",
    "email": "sandra@mail.com",
    "password": "password123"
  }'
```

### Crear partida

```bash
curl -X POST http://localhost:5000/api/gameplays \
  -H "Content-Type: application/json" \
  -d '{
    "currentPositions": "{\"hostPlayerMoves\": [], \"guestPlayerMoves\": [], \"playerTurnId\": \"679930f8e9a1234567890def\", \"winnerLine\": []}",
    "hostPlayer": "ID_USUARIO_1",
    "guestPlayer": "ID_USUARIO_2",
    "playerTurn": "ID_USUARIO_1"
  }'
```

### Obtener todas las partidas

```bash
curl http://localhost:5000/api/gameplays
```

### Verificar el esquema en MongoDB

```bash
docker exec -it game-mongodb mongosh "mongodb://localhost:27017/GameDB" --eval "
  printjson(db.users.getIndexes());
  printjson(db.matches.getIndexes());
"
```

## Estructura del Proyecto

```text
GameAPI/
├── Controllers/
│   ├── UsersController.cs            # Endpoints de usuarios
│   └── GameplaysController.cs        # Endpoints de partidas
├── Models/
│   ├── Gameplay.cs                   # Modelo de partida
│   ├── GameplayDto.cs                # Dto de partida
│   ├── GameplayResponseDto.cs        # Response Dto de partida
│   ├── LoginDto.cs                   # Modelo de Login
│   ├── LoginResponseDto.cs           # Response Dto de login
│   ├── MongoDBSettings.cs            # Configuración de MongoDB
│   ├── User.cs                       # Modelo de usuario
│   ├── UserDto.cs                    # Dto de usuario
│   └── UserResponseDto.cs            # Response Dto de usuario
├── Services/
│   ├── UserService.cs                # Lógica de negocio de usuarios
│   ├── GameplayService.cs            # Lógica de negocio de partidas
│   └── DatabaseInitializer.cs        # Asegura esquema al arrancar la API
├── Scripts/
│   └── migrations/
│       └── 001_init_schema.js        # Migración inicial (users + matches)
├── WebSockets/
│   └── WebSocketConnectionManager.cs # Manejo de conexiones WS
├── Program.cs                        # Configuración de la aplicación
├── appsettings.json                  # Configuración
├── Dockerfile                        # Imagen de la API
├── docker-compose.yml                # Orquestación de API + MongoDB
└── GameAPI.csproj                    # Archivo de proyecto
```

## Seguridad

- Las contraseñas se hashean con BCrypt antes de almacenarse
- Validación de email único (índice `ux_users_email`)
- Validación de formato JSON para posiciones de un juego
- Validación de existencia de usuarios antes de crear partidas
- Esquema `$jsonSchema` aplicado a nivel de MongoDB (defensa en profundidad)

## Tecnologías

- ASP.NET Core 10.0
- MongoDB.Driver 2.24.0
- BCrypt.Net-Next 4.0.3
- Swagger/OpenAPI
- Docker / Docker Compose
- MongoDB Shell (`mongosh`) para migraciones

## Notas

- El campo `board_state` (antes `currentPositions`) acepta cualquier JSON válido para máxima flexibilidad
- Las fechas se almacenan en formato UTC
- Los IDs de MongoDB se generan automáticamente
- El email se convierte a minúsculas automáticamente
- Los nombres de los campos en la base de datos (`username`, `password_hash`, `board_state`, etc.) pueden diferir de los nombres expuestos en la API (`name`, `password`, `currentPositions`): esto se controla vía `[BsonElement]` en los modelos, por lo que el contrato JSON con el frontend no cambia
- Los validators están en modo `moderate` + `error`, por lo que solo afectan a nuevas escrituras que no cumplan el esquema
- Para entornos productivos se recomienda no montar `Scripts/migrations/` en `/docker-entrypoint-initdb.d` y ejecutar las migraciones explícitamente como un paso del pipeline de despliegue