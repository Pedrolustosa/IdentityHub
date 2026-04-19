using IdentityHub.Application.DTOs;
using IdentityHub.Application.Services;
using IdentityHub.Domain.Entities;
using IdentityHub.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IdentityHub.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly TokenService _tokenService;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AppDbContext _context;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            TokenService tokenService,
            RoleManager<IdentityRole> roleManager,
            AppDbContext context)
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _roleManager = roleManager;
            _context = context;
        }

        // 🔹 REGISTER
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Email and password are required");

            var email = request.Email.Trim().ToLower();

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = request.FullName?.Trim(),
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            if (await _roleManager.RoleExistsAsync("User"))
                await _userManager.AddToRoleAsync(user, "User");

            return Ok("User created successfully");
        }

        // 🔹 LOGIN
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Email and password are required");

            var email = request.Email.Trim().ToLower();

            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
                return Unauthorized("Invalid credentials");

            if (!user.IsActive)
                return Unauthorized("User is inactive");

            var passwordValid = await _userManager
                .CheckPasswordAsync(user, request.Password);

            if (!passwordValid)
                return Unauthorized("Invalid credentials");

            var roles = await _userManager.GetRolesAsync(user);

            // 🔐 JWT Access Token
            var accessToken = await _tokenService.GenerateToken(
                user,
                roles,
                _userManager,
                _roleManager
            );

            // 🔄 Refresh Token
            var refreshToken = _tokenService.GenerateRefreshToken();

            _context.RefreshTokens.Add(new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = refreshToken,
                UserId = user.Id,
                Created = DateTime.UtcNow,
                Expires = DateTime.UtcNow.AddDays(7),
                IsRevoked = false
            });

            await _context.SaveChangesAsync();

            return Ok(new AuthResponse
            {
                Token = accessToken,
                RefreshToken = refreshToken
            });
        }

        // 🔄 REFRESH TOKEN
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(RefreshTokenRequest request)
        {
            var storedToken = await _context.RefreshTokens
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.Token == request.RefreshToken);

            if (storedToken == null)
                return Unauthorized("Invalid refresh token");

            if (storedToken.IsRevoked || storedToken.Expires < DateTime.UtcNow)
                return Unauthorized("Expired refresh token");

            var user = storedToken.User;

            var roles = await _userManager.GetRolesAsync(user);

            var newAccessToken = await _tokenService.GenerateToken(
                user,
                roles,
                _userManager,
                _roleManager
            );

            // 🔄 revoga token antigo
            storedToken.IsRevoked = true;

            // 🔄 gera novo refresh token
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            _context.RefreshTokens.Add(new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = newRefreshToken,
                UserId = user.Id,
                Created = DateTime.UtcNow,
                Expires = DateTime.UtcNow.AddDays(7),
                IsRevoked = false
            });

            await _context.SaveChangesAsync();

            return Ok(new AuthResponse
            {
                Token = newAccessToken,
                RefreshToken = newRefreshToken
            });
        }

        // 🔹 LOGOUT
        [HttpPost("logout")]
        public async Task<IActionResult> Logout(RefreshTokenRequest request)
        {
            var token = await _context.RefreshTokens
                .FirstOrDefaultAsync(x => x.Token == request.RefreshToken);

            if (token != null)
            {
                token.IsRevoked = true;
                await _context.SaveChangesAsync();
            }

            return Ok("Logged out successfully");
        }
    }
}