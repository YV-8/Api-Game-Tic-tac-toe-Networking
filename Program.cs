using GameAPI.Models;
using GameAPI.Services;
using GameAPI.WebSockets;
using System.Net.WebSockets;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.IdentityModel.JsonWebTokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<MongoDBSettings>(
    builder.Configuration.GetSection("MongoDBSettings"));

builder.Services.AddHostedService<DatabaseInitializer>();

builder.Services.AddSingleton<UserService>();
builder.Services.AddSingleton<GameplayService>();
builder.Services.AddSingleton<WebSocketConnectionManager>();

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"]
    ?? throw new InvalidOperationException("SecretKey JWT not configured");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero // By default is 5min
    };
});
builder.Services.AddAuthorization();
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Game API",
        Version = "v1",
        Description = "API REST for user and gameplay basic management"
    });

    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Attach 'Bearer' and a space before token, g.e.: 'Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...'"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
        });
});

var app = builder.Build();

// HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseHttpsRedirection();
}

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

var webSocketOptions = new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromMinutes(2)
};
app.UseWebSockets(webSocketOptions);

// ============================================================
// WebSocket endpoint: /ws?token=<JWT>
// ============================================================
app.Map("/ws", async context =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }

    var token = context.Request.Query["token"].ToString();
    if (string.IsNullOrWhiteSpace(token))
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return;
    }

    var config = context.RequestServices.GetRequiredService<IConfiguration>();
    var jwtSection = config.GetSection("JwtSettings");
    var secretKey = jwtSection["SecretKey"]!;

    string? userId;
    try
    {
        var handler = new JsonWebTokenHandler();

        var validationResult = await handler.ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ClockSkew = TimeSpan.Zero
        });

        if (!validationResult.IsValid)
        {
            Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}] [WS_AUTH_FAILED] Token inválido: {validationResult.Exception?.Message}");
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        userId = validationResult.ClaimsIdentity.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? validationResult.ClaimsIdentity.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}] [WS_AUTH_FAILED] Error validando token: {ex.Message}");
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return;
    }

    if (string.IsNullOrEmpty(userId))
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return;
    }

    using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
    var wsManager = context.RequestServices.GetRequiredService<WebSocketConnectionManager>();
    var socketId = wsManager.AddSocket(webSocket, userId);

    Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}] [WS_CONNECT] Usuario {userId} conectado (socket {socketId})");

    await SendToUser(userId, new { type = "welcome", userId }, wsManager);

    var userService = context.RequestServices.GetRequiredService<UserService>();
    await BroadcastOnlineUsers(wsManager, userService);

    // try/catch para manejar desconexiones abruptas
    try
    {
        var buffer = new byte[1024 * 4];
        var receiveResult = await webSocket.ReceiveAsync(
            new ArraySegment<byte>(buffer), CancellationToken.None);

        while (!receiveResult.CloseStatus.HasValue)
        {
            var message = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);
            Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}] [WS_MSG] De {userId}: {message}");

            await HandleWebSocketMessage(userId, message, wsManager, userService);

            receiveResult = await webSocket.ReceiveAsync(
                new ArraySegment<byte>(buffer), CancellationToken.None);
        }
    }
    catch (WebSocketException ex)
    {
        // El cliente cerró abruptamente
        Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}] [WS_DISCONNECT] Conexión de {userId} cerrada abruptamente ({ex.WebSocketErrorCode})");
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}] [WS_DISCONNECT] Operación cancelada para {userId}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}] [WS_ERROR] Error inesperado para {userId}: {ex.Message}");
    }
    finally
    {
        await wsManager.RemoveSocket(socketId);
        Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}] [WS_DISCONNECT] Usuario {userId} desconectado");
        await BroadcastOnlineUsers(wsManager, userService);
    }
});

// ============================================================
// Helpers locales de WebSocket
// ============================================================
static async Task SendToUser(string userId, object payload, WebSocketConnectionManager wsManager)
{
    var socket = wsManager.GetSocketByUserId(userId);
    if (socket == null || socket.State != WebSocketState.Open) return;

    var json = JsonSerializer.Serialize(payload);
    var bytes = Encoding.UTF8.GetBytes(json);
    try
    {
        await socket.SendAsync(
            new ArraySegment<byte>(bytes),
            WebSocketMessageType.Text,
            true,
            CancellationToken.None);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[WS] Error enviando a {userId}: {ex.Message}");
    }
}

static async Task BroadcastOnlineUsers(
    WebSocketConnectionManager wsManager,
    UserService userService)
{
    var onlineIds = wsManager.GetOnlineUserIds();

    var allUsers = await userService.GetAsync();
    var usersDict = allUsers
        .Where(u => u.Id != null)
        .ToDictionary(u => u.Id!, u => u.Name);

    var payload = new
    {
        type = "online_users",
        users = onlineIds.Select(id => new
        {
            id,
            name = usersDict.TryGetValue(id, out var name) ? name : id
        }).ToList()
    };

    var json = JsonSerializer.Serialize(payload);
    var bytes = Encoding.UTF8.GetBytes(json);

    foreach (var (_, socket) in wsManager.GetAll())
    {
        if (socket.State != WebSocketState.Open) continue;
        try
        {
            await socket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);
        }
        catch { /* ignore */ }
    }
}

static async Task HandleWebSocketMessage(
    string userId,
    string rawMessage,
    WebSocketConnectionManager wsManager,
    UserService userService)
{
    try
    {
        using var doc = JsonDocument.Parse(rawMessage);
        var root = doc.RootElement;

        if (!root.TryGetProperty("type", out var typeProp))
        {
            await SendToUser(userId, new { type = "error", message = "Missing 'type'" }, wsManager);
            return;
        }

        var type = typeProp.GetString();

        switch (type)
        {
            case "ping":
                await SendToUser(userId, new { type = "pong", timestamp = DateTime.UtcNow }, wsManager);
                break;

            // Sprint 2: Mensaje list_users (y request_online_users para retrocompatibilidad)
            case "list_users":
            case "request_online_users":
                var onlineIds = wsManager.GetOnlineUserIds();
                var allUsers = await userService.GetAsync();
                var dict = allUsers.Where(u => u.Id != null).ToDictionary(u => u.Id!, u => u.Name);
                await SendToUser(userId, new
                {
                    type = "online_users",
                    users = onlineIds.Select(id => new
                    {
                        id,
                        name = dict.TryGetValue(id, out var n) ? n : id
                    }).ToList()
                }, wsManager);
                break;

            case "invite_player":
                if (root.TryGetProperty("targetUserId", out var targetProp))
                {
                    var targetUserId = targetProp.GetString();
                    if (!string.IsNullOrEmpty(targetUserId))
                    {
                        var hostUser = await userService.GetAsync(userId);
                        var hostName = hostUser?.Name ?? userId;
                        var matchId = root.TryGetProperty("matchId", out var matchProp) ? matchProp.GetString() : null;

                        Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}] [WS_INVITE] Usuario {userId} ({hostName}) invitó a {targetUserId} a jugar");
                        await SendToUser(targetUserId, new
                        {
                            type = "game_invitation",
                            hostId = userId,
                            hostName,
                            matchId
                        }, wsManager);
                    }
                }
                break;

            case "accept_invite":
                if (root.TryGetProperty("hostId", out var hostProp))
                {
                    var hostId = hostProp.GetString();
                    var matchId = root.TryGetProperty("matchId", out var mProp) ? mProp.GetString() : null;
                    if (!string.IsNullOrEmpty(hostId))
                    {
                        var guestUser = await userService.GetAsync(userId);
                        var guestName = guestUser?.Name ?? userId;
                        Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}] [WS_INVITE_ACCEPTED] Usuario {userId} ({guestName}) aceptó la partida de {hostId}");
                        await SendToUser(hostId, new
                        {
                            type = "invite_accepted",
                            guestId = userId,
                            guestName,
                            matchId
                        }, wsManager);
                    }
                }
                break;

            case "reject_invite":
                if (root.TryGetProperty("hostId", out var rHostProp))
                {
                    var hostId = rHostProp.GetString();
                    if (!string.IsNullOrEmpty(hostId))
                    {
                        Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}] [WS_INVITE_REJECTED] Usuario {userId} rechazó la invitación de {hostId}");
                        await SendToUser(hostId, new
                        {
                            type = "invite_rejected",
                            fromUserId = userId
                        }, wsManager);
                    }
                }
                break;

            case "cancel_invite":
                if (root.TryGetProperty("targetUserId", out var cancelProp))
                {
                    var targetUserId = cancelProp.GetString();
                    if (!string.IsNullOrEmpty(targetUserId))
                    {
                        Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}] [WS_INVITE_CANCELLED] Usuario {userId} canceló la invitación a {targetUserId}");
                        await SendToUser(targetUserId, new
                        {
                            type = "invite_cancelled",
                            fromUserId = userId
                        }, wsManager);
                    }
                }
                break;

            case "game_move":
                if (root.TryGetProperty("targetUserId", out var moveTargetProp) &&
                    root.TryGetProperty("position", out var posProp))
                {
                    var targetUserId = moveTargetProp.GetString();
                    var position = posProp.GetInt32();
                    var orderPlay = root.TryGetProperty("orderPlay", out var ordProp) ? ordProp.GetInt32() : 0;
                    if (!string.IsNullOrEmpty(targetUserId))
                    {
                        Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}] [WS_MOVE] Jugada de {userId} en pos {position} (orderPlay: {orderPlay}) para {targetUserId}");
                        await SendToUser(targetUserId, new
                        {
                            type = "opponent_move",
                            fromUserId = userId,
                            position,
                            orderPlay
                        }, wsManager);
                    }
                }
                break;

            case "reset_game":
                if (root.TryGetProperty("targetUserId", out var resetTargetProp))
                {
                    var targetUserId = resetTargetProp.GetString();
                    if (!string.IsNullOrEmpty(targetUserId))
                    {
                        Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}] [WS_RESET] Partida reiniciada por {userId} para {targetUserId}");
                        await SendToUser(targetUserId, new
                        {
                            type = "game_reset",
                            fromUserId = userId
                        }, wsManager);
                    }
                }
                break;

            default:
                Console.WriteLine($"[WS] Tipo desconocido: {type}");
                await SendToUser(userId, new
                {
                    type = "error",
                    message = $"Unknown message type: {type}"
                }, wsManager);
                break;
        }
    }
    catch (JsonException)
    {
        await SendToUser(userId, new { type = "error", message = "Invalid JSON" }, wsManager);
    }
}

app.MapControllers();
// TODO: enable https/ssl
app.Run();
