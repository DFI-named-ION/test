using FMVideoManagerApi.Data.DTO;
using FMVideoManagerApi.Data.Repositories.UserRepository;
using FMVideoManagerApi.Services;
using FMVideoManagerApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FMVideoManagerApi.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public sealed class AuthController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly JwtTokenService _tokenService;

        public AuthController(IUserRepository userRepository, JwtTokenService tokenService)
        {
            _userRepository = userRepository;
            _tokenService = tokenService;
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Login))
                return BadRequest("Login is required.");

            if (string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Password is required.");

            if (string.IsNullOrWhiteSpace(request.Alias))
                return BadRequest("Alias is required.");

            AppUser? existingUser = await _userRepository.FindByLoginAsync(request.Login);

            if (existingUser != null)
                return Conflict("User with this login already exists.");

            var user = new AppUser
            {
                Login = request.Login.Trim(),
                Alias = request.Alias.Trim(),
                PasswordHash = CryptographyService.HashPassword(request.Password),
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();

            string accessToken = _tokenService.CreateAccessToken(user);

            return Ok(new AuthResponse
            {
                UserId = user.Id,
                Login = user.Login,
                Alias = user.Alias,
                AccessToken = accessToken
            });
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Login))
                return BadRequest("Login is required.");

            if (string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Password is required.");

            AppUser? user = await _userRepository.FindByLoginAsync(request.Login.Trim());

            if (user == null)
                return Unauthorized("Invalid login or password.");

            bool passwordValid = CryptographyService.VerifyPasswordHash(request.Password, user.PasswordHash);

            if (!passwordValid)
                return Unauthorized("Invalid login or password.");

            string accessToken = _tokenService.CreateAccessToken(user);

            return Ok(new AuthResponse
            {
                UserId = user.Id,
                Login = user.Login,
                Alias = user.Alias,
                AccessToken = accessToken
            });
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<ActionResult<AuthResponse>> Me()
        {
            string? userIdText = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (!long.TryParse(userIdText, out long userId))
                return Unauthorized();

            AppUser? user = await _userRepository.FindByIdAsync(userId);

            if (user == null)
                return Unauthorized();

            string accessToken = _tokenService.CreateAccessToken(user);

            return Ok(new AuthResponse
            {
                UserId = user.Id,
                Login = user.Login,
                Alias = user.Alias,
                AccessToken = accessToken
            });
        }
    }
}