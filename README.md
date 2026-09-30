# Game API - ASP.NET

API REST desarrollada en ASP.NET Core 8.0 para gestionar usuarios y partidas de un juego (Tic-Tac-Toe) usando MongoDB.

## Características

- Registro y gestión de usuarios (nombre, email, password)
- Gestión de partidas de juego con estado JSON
- Autenticación con contraseñas hasheadas (BCrypt)
- Validación de datos
- Documentación interactiva con Swagger
- Arquitectura REST completa

## Pre-requisitos

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download) (para ejecución local)
- [MongoDB](https://www.mongodb.com/try/download/community) (local o en cloud)
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

## Estructura del Proyecto

```text
GameAPI/
├── Controllers/
│   ├── UsersController.cs        # Endpoints de usuarios
│   └── GameplaysController.cs    # Endpoints de partidas
├── Models/
│   ├── Gameplay.cs               # Modelo de partida
│   ├── GameplayDto.cs            # Dto de partida
│   ├── GameplayResponseDto.cs    # Response Dto de partida
│   ├── LoginDto.cs               # Modelo de Login
│   ├── LoginResponseDto.cs       # Response Dto de login
│   ├── MongoDBSettings.cs        # Configuración de MongoDB
│   ├── User.cs                   # Modelo de usuario
│   ├── UserDto.cs                # Dto de usuario
│   └── UserResponseDto.cs        # Response Dto de usuario
├── Services/
│   ├── UserService.cs            # Lógica de negocio de usuarios
│   └── GameplayService.cs        # Lógica de negocio de partidas
├── Program.cs                    # Configuración de la aplicación
├── appsettings.json              # Configuración
├── Dockerfile                    # Imagen de la API
├── docker-compose.yml            # Orquestación de API + MongoDB
└── GameAPI.csproj                # Archivo de proyecto
```

## Seguridad

- Las contraseñas se hashean con BCrypt antes de almacenarse
- Validación de email único
- Validación de formato JSON para posiciones de un juego
- Validación de existencia de usuarios antes de crear partidas

## Tecnologías

- ASP.NET Core 8.0
- MongoDB.Driver 2.24.0
- BCrypt.Net-Next 4.0.3
- Swagger/OpenAPI
- Docker / Docker Compose

## Notas

- El campo `currentPositions` acepta cualquier JSON válido para máxima flexibilidad
- Las fechas se almacenan en formato UTC
- Los IDs de MongoDB se generan automáticamente
- El email se convierte a minúsculas automáticamente