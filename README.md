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

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download)
- [MongoDB](https://www.mongodb.com/try/download/community) (local o en cloud)

## Configuraion

1. Clonar o descargar el repositorio

2. Configurar MongoDB

Editar el archivo appsettings.json con la cadena de conexión a Mongodb:

```json
{
  "MongoDBSettings": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "GameDB"
  }
}
```

3. Restaurar paquetes

```bash
dotnet restore
```

4. Ejecutar la aplicación

```bash
dotnet run
```

Direccion de la API:

- HTTPS: `https://localhost:58333`
- HTTP: `http://localhost:58334`
- Swagger UI: `https://localhost:58333/swagger`

## Endpoints REST API

### Users

#### POST /api/Users
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
Actualizar un usuario:
Request Body:
```json
{
  "name": "Jhony Doe",  
  "password": "miSecurePassword123"
}
```

Response (204 OK)

#### DELETE /api/users/{id}
Eliminar un usuario

Response (204 OK)

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
curl http://localhost:5000/api/partidas
```

## Estructura del Proyecto

```
GameAPI/
├── Controllers/
│   ├── UsersController.cs    # Endpoints de usuarios
│   └── GameplaysController.cs    # Endpoints de partidas
├── Models/
│   ├── Gameplay.cs              # Modelo de partida
│   ├── GameplayDto.cs           # Dto de partida
│   ├── GameplayResponseDto.cs   # Response Dto de partida
│   ├── Login.cs                 # Modelo de Login
│   ├── LoginResponsesDto.cs     # Response Dto de login
│   ├── MongoDBSettings.cs       # Configuración de MongoDB
│   ├── User.cs                  # Modelo de usuario
│   ├── UserDto.cs               # Dto de usuario
│   └── UserResponseDto.cs       # Response Dto de usuario
├── Services/
│   ├── UserService.cs           # Lógica de negocio de usuarios
│   └── GamePlayervice.cs        # Lógica de negocio de partidas
├── Program.cs                   # Configuración de la aplicación
├── appsettings.json             # Configuración
└── GameAPI.csproj               # Archivo de proyecto
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

## Notas

- El campo `currentPositions` acepta cualquier JSON válido para máxima flexibilidad
- Las fechas se almacenan en formato UTC
- Los IDs de MongoDB se generan automáticamente
- El email se convierte a minúsculas automáticamente
