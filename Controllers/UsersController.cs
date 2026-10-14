using GameAPI.Models;
using GameAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GameAPI.WebSockets;
using Microsoft.AspNetCore.Authorization;

namespace GameAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly UserService _userService;
    private readonly IConfiguration _configuration;
    private const int BcryptWorkFactor = 11;
    private static readonly string DummyTimingHash = BCrypt.Net.BCrypt.HashPassword("DefensaTimingAttackDummy123!", BcryptWorkFactor);

    public UsersController(UserService userService, IConfiguration configuration)
    {
        _userService = userService;
        _configuration = configuration;
    }
    
    [Authorize]
    [HttpGet]
    public async Task<ActionResult<List<UserResponseDto>>> Get()
    {
        var users = await _userService.GetAsync();
        var response = users.Select(u => new UserResponseDto
        {
            Id = u.Id!,
            Name = u.Name,
            Email = u.Email,
            RegisterDate = u.RegisterDate
        }).ToList();        
        return Ok(response);
    }

    [Authorize]
    [HttpGet("{id}")]
    public async Task<ActionResult<UserResponseDto>> Get(string id)
    {
        var user = await _userService.GetAsync(id);
        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        var response = new UserResponseDto
        {
            Id = user.Id!,
            Name = user.Name,
            Email = user.Email,
            RegisterDate = user.RegisterDate
        };
        return Ok(response);
    }
    
    [AllowAnonymous]
    [HttpPost]
    public async Task<ActionResult<UserResponseDto>> Post([FromBody] UserDto newUserDto)
    {
        if (string.IsNullOrWhiteSpace(newUserDto.Name) ||
            string.IsNullOrWhiteSpace(newUserDto.Email) ||
            string.IsNullOrWhiteSpace(newUserDto.Password))
        {
            return BadRequest(new { message = "All fields are mandatory" });
        }
        
        if (newUserDto.Password.Length < 6 || newUserDto.Password.Length > 72)
        {
            return BadRequest(new { message = "Password must be between 6 and 72 characters" });
        }

        if (!IsValidEmail(newUserDto.Email))
        {
            return BadRequest(new { message = "Email format is invalid. Must contain '@' and a valid domain (e.g. .com, .net)" });
        }
        var existingUser = await _userService.GetByEmailAsync(newUserDto.Email);
        if (existingUser != null)
        {
            return Conflict(new { message = "Email is currently registered" });
        }

        string passwordHash = BCrypt.Net.BCrypt.HashPassword(newUserDto.Password, BcryptWorkFactor);
        var newUser = new User
        {
            Name = newUserDto.Name,
            Email = newUserDto.Email.ToLower(),
            Password = passwordHash,
            RegisterDate = DateTime.UtcNow
        };
        await _userService.CreateAsync(newUser);
        Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}] [USER_REGISTER] Usuario registrado con éxito. Email: {newUser.Email}, Id: {newUser.Id}");

        var response = new UserResponseDto
        {
            Id = newUser.Id!,
            Name = newUser.Name,
            Email = newUser.Email,
            RegisterDate = newUser.RegisterDate
        };
        return CreatedAtAction(nameof(Get), new { id = newUser.Id }, response);
    }
    
    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> Put(string id, [FromBody] UserDto userDto)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(currentUserId) || currentUserId != id)
        {
            return Forbid();
        }

        var user = await _userService.GetAsync(id);
        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }        
        if (!string.IsNullOrWhiteSpace(userDto.Name))
        {
            user.Name = userDto.Name;
        }
        if (!string.IsNullOrWhiteSpace(userDto.Email))
        {
            if (!IsValidEmail(userDto.Email))
            {
                return BadRequest(new { message = "Email format is invalid" });
            }
            user.Email = userDto.Email.ToLower();
        }
        if (!string.IsNullOrWhiteSpace(userDto.Password))
        {
            if (userDto.Password.Length < 6 || userDto.Password.Length > 72)
            {
                return BadRequest(new { message = "Password must be between 6 and 72 characters" });
            }
            user.Password = BCrypt.Net.BCrypt.HashPassword(userDto.Password, BcryptWorkFactor);
        }
        await _userService.UpdateAsync(id, user);
        return NoContent();
    }
    
    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(currentUserId) || currentUserId != id)
        {
            return Forbid();
        }

        var user = await _userService.GetAsync(id);
        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }
        await _userService.RemoveAsync(id);
        return NoContent();
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult> Login([FromBody] LoginDto loginDto)
    {
        if (string.IsNullOrWhiteSpace(loginDto.Email) ||
            string.IsNullOrWhiteSpace(loginDto.Password))
        {
            return BadRequest(new { message = "Email and password are required" });
        }

        if (loginDto.Password.Length > 72)
        {
            return Unauthorized(new { message = "Invalid credentials" });
        }

        var user = await _userService.GetByEmailAsync(loginDto.Email);
        bool validPassword = false;

        if (user != null)
        {
            validPassword = BCrypt.Net.BCrypt.Verify(loginDto.Password, user.Password);
        }
        else
        {
            BCrypt.Net.BCrypt.Verify(loginDto.Password, DummyTimingHash);
        }

        if (user == null || !validPassword)
        {
            return Unauthorized(new { message = "Invalid credentials" });
        }

        var token = GenerarJwtToken(user);
        var response = new LoginResponseDto
        {
            Token = token,
            User = new UserResponseDto
            {
                Id = user.Id!,
                Name = user.Name,
                Email = user.Email,
                RegisterDate = user.RegisterDate
            }
        };
        return Ok(response);
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private string GenerarJwtToken(User user)
    {
        var jwtKey = _configuration["JwtSettings:SecretKey"]
                     ?? throw new InvalidOperationException("JWT SecretKey not configured");
        var jwtIssuer = _configuration["JwtSettings:Issuer"] ?? "GameAPI";
        var jwtAudience = _configuration["JwtSettings:Audience"] ?? "GameAPI";
        var jwtExpireMinutes = int.Parse(_configuration["JwtSettings:ExpireMinutes"] ?? "60");
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id!),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Name, user.Name),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(jwtExpireMinutes),
            IssuedAt = DateTime.UtcNow,
            NotBefore = DateTime.UtcNow,
            Issuer = jwtIssuer,
            Audience = jwtAudience,
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
    
    [Authorize]
    [HttpGet("online")]
    public async Task<ActionResult> GetOnlineUsers(
        [FromServices] WebSocketConnectionManager wsManager,
        [FromServices] UserService userService)
    {
        var onlineIds = wsManager.GetOnlineUserIds();
        var allUsers = await userService.GetAsync();
        var dict = allUsers.Where(u => u.Id != null).ToDictionary(u => u.Id!, u => u.Name);

        var result = onlineIds.Select(id => new
        {
            id,
            name = dict.TryGetValue(id, out var n) ? n : id
        }).ToList();

        return Ok(result);
    }
}
