using GameAPI.Models;
using GameAPI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace GameAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GameplaysController : ControllerBase
{
    private readonly GameplayService _gameplayService;
    private readonly UserService _userService;
    public GameplaysController(GameplayService gameplayService, UserService userService)
    {
        _gameplayService = gameplayService;
        _userService = userService;
    }

    [HttpGet]
    public async Task<ActionResult<List<GameplayResponseDto>>> Get()
    {
        var partidas = await _gameplayService.GetAsync();
        var response = partidas.Select(p => new GameplayResponseDto
        {
            Id = p.Id!,
            CurrentPositions = p.CurrentPositions,
            HostPlayer = p.HostPlayer,
            GuestPlayer = p.GuestPlayer,
            PlayerTurn = p.PlayerTurn,
            MatchResult = p.MatchResult,
            CreatedDate = p.CreatedDate,
            UpdatedDate = p.UpdatedDate
        }).ToList();
        return Ok(response);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<GameplayResponseDto>> Get(string id)
    {
        var gameplay = await _gameplayService.GetAsync(id);
        if (gameplay == null)
        {
            return NotFound(new { message = "Gameplay not found" });
        }
        var response = new GameplayResponseDto
        {
            Id = gameplay.Id!,
            CurrentPositions = gameplay.CurrentPositions,
            HostPlayer = gameplay.HostPlayer,
            GuestPlayer = gameplay.GuestPlayer,
            PlayerTurn= gameplay.PlayerTurn,
            MatchResult = gameplay.MatchResult,
            CreatedDate = gameplay.CreatedDate,
            UpdatedDate = gameplay.UpdatedDate
        };
        return Ok(response);
    }

    [Authorize]
    [HttpGet("my-gameplays")]
    public async Task<ActionResult<List<GameplayResponseDto>>> GetMyGameplays()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Invalid token" });
        }
        var gameplays = await _gameplayService.GetByPlayerAsync(userId);
        var response = gameplays.Select(gameplay => new GameplayResponseDto
        {
            Id = gameplay.Id!,
            CurrentPositions = gameplay.CurrentPositions,
            HostPlayer = gameplay.HostPlayer,
            GuestPlayer= gameplay.GuestPlayer,
            PlayerTurn = gameplay.PlayerTurn,
            MatchResult = gameplay.MatchResult,
            CreatedDate = gameplay.CreatedDate,
            UpdatedDate = gameplay.UpdatedDate
        }).ToList();
        return Ok(response);
    }

    [HttpGet("player/{playerId}")]
    public async Task<ActionResult<List<GameplayResponseDto>>> GetByPlayer(string playerId)
    {
        var gameplays = await _gameplayService.GetByPlayerAsync(playerId);
        var response = gameplays.Select(gameplay => new GameplayResponseDto
        {
            Id = gameplay.Id!,
            CurrentPositions = gameplay.CurrentPositions,
            HostPlayer= gameplay.HostPlayer,
            GuestPlayer = gameplay.GuestPlayer,
            PlayerTurn = gameplay.PlayerTurn,
            MatchResult = gameplay.MatchResult,
            CreatedDate = gameplay.CreatedDate,
            UpdatedDate = gameplay.UpdatedDate
        }).ToList();
        return Ok(response);
    }
    
    [Authorize]
    [HttpPost]
    public async Task<ActionResult<GameplayResponseDto>> Post([FromBody] GameplayDto newGameplayDto)
    {
        var userIDToken = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIDToken))
        {
            return Unauthorized(new { message = "Invalid token"});
        }
        if (newGameplayDto.HostPlayer != userIDToken)
        {
            return Forbid();
        }        
        if (string.IsNullOrWhiteSpace(newGameplayDto.HostPlayer) ||
            string.IsNullOrWhiteSpace(newGameplayDto.PlayerTurn))
        {
            return BadRequest(new { message = "Hostplayer and turnplayer is mandatory" });
        }
        if (!string.IsNullOrWhiteSpace(newGameplayDto.CurrentPositions))
        {
            if (!IsValidJson(newGameplayDto.CurrentPositions))
            {
                return BadRequest(new { message = "Current positions format is not a valid JSON" });
            }
        }
        var host = await _userService.GetAsync(newGameplayDto.HostPlayer);
        if (host == null)
        {
            return BadRequest(new { message = "Host player not exists" });
        }        
        if (!string.IsNullOrWhiteSpace(newGameplayDto.GuestPlayer))
        {
            var guest = await _userService.GetAsync(newGameplayDto.GuestPlayer);
            if (guest == null)
            {
                return BadRequest(new { message = "Guest player not exist" });
            }
        }
        var newGameplay = new Gameplay
        {
            CurrentPositions = newGameplayDto.CurrentPositions,
            HostPlayer = newGameplayDto.HostPlayer,
            GuestPlayer = newGameplayDto.GuestPlayer,
            PlayerTurn = newGameplayDto.PlayerTurn,
            MatchResult = newGameplayDto.MatchResult,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };

        await _gameplayService.CreateAsync(newGameplay);
        var response = new GameplayResponseDto
        {
            Id = newGameplay.Id!,
            CurrentPositions = newGameplay.CurrentPositions,
            HostPlayer = newGameplay.HostPlayer,
            GuestPlayer = newGameplay.GuestPlayer,
            PlayerTurn = newGameplay.PlayerTurn,
            MatchResult = newGameplay.MatchResult,
            CreatedDate = newGameplay.CreatedDate,
            UpdatedDate = newGameplay.UpdatedDate
        };
        return CreatedAtAction(nameof(Get), new { id = newGameplay.Id }, response);
    }
    
    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> Put(string id, [FromBody] GameplayDto gameplayDto)
    {
        var userIdToken = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdToken))
        {
            return Unauthorized(new { message = "Invalid token" });
        }
        var gameplay = await _gameplayService.GetAsync(id);
        if (gameplay == null)
        {
            return NotFound(new { message = "Gameplay not found" });
        }
        if (gameplay.HostPlayer != userIdToken &&
            gameplay.GuestPlayer != userIdToken)
        {
            return Forbid(); // 403 Forbidden
        }        
        if (!string.IsNullOrWhiteSpace(gameplayDto.CurrentPositions))
        {
            if (!IsValidJson(gameplayDto.CurrentPositions))
            {
                return BadRequest(new { message = "Current positions format is not a valid JSON format." });
            }
            gameplay.CurrentPositions = gameplayDto.CurrentPositions;
        }        
        if (!string.IsNullOrWhiteSpace(gameplayDto.GuestPlayer))
        {
            var guest = await _userService.GetAsync(gameplayDto.GuestPlayer);
            if (guest == null)
            {
                return BadRequest(new { message = "Player guest not exist" });
            }
            gameplay.GuestPlayer = gameplayDto.GuestPlayer;
        }
        if (!string.IsNullOrWhiteSpace(gameplayDto.PlayerTurn))
        {
            gameplay.PlayerTurn = gameplayDto.PlayerTurn;
        }

        if (!string.IsNullOrWhiteSpace(gameplayDto.MatchResult))
        {
            gameplay.MatchResult = gameplayDto.MatchResult;
        }

        await _gameplayService.UpdateAsync(id, gameplay);
        return NoContent();
    }
    
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var userIdToken = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdToken))
        {
            return Unauthorized(new { message = "Invallid token" });
        }
        var gameplay = await _gameplayService.GetAsync(id);
        if (gameplay == null)
        {
            return NotFound(new { message = "Gameplay not found" });
        }        
        if (gameplay.HostPlayer != userIdToken)
        {
            return Forbid();
        }
        await _gameplayService.RemoveAsync(id);
        return NoContent();
    }

    private bool IsValidJson(string jsonString)
    {
        try
        {
            JsonDocument.Parse(jsonString);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
