using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using LastHour.Application.Common.Interfaces;
using LastHour.Domain.Entities;
using LastHour.Infrastructure.Data;
using LastHour.Infrastructure.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace LastHour.Infrastructure.Identity.Services;

public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ApplicationDbContext context,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
        _configuration = configuration;
    }

    public async Task<(bool Success, string? Error, AuthUserResult? User)> RegisterAsync(
        string email, string password, string fullName, string? phone, string role)
    {
        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser != null)
            return (false, "Email already registered", null);

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            PhoneNumber = phone,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            return (false, string.Join(", ", result.Errors.Select(e => e.Description)), null);

        if (!await _roleManager.RoleExistsAsync(role))
            await _roleManager.CreateAsync(new IdentityRole(role));

        await _userManager.AddToRoleAsync(user, role);

        var profile = new UserProfile
        {
            IdentityId = user.Id,
            FullName = fullName,
            Email = email,
            Phone = phone
        };
        _context.UserProfiles.Add(profile);
        await _context.SaveChangesAsync();

        return (true, null, new AuthUserResult(user.Id, fullName, email, phone, null));
    }

    public async Task<(bool Success, string? Error, AuthUserResult? User)> LoginAsync(
        string email, string password)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null || user.IsDeleted)
            return (false, "Invalid email or password", null);

        var isValid = await _userManager.CheckPasswordAsync(user, password);
        if (!isValid)
            return (false, "Invalid email or password", null);

        return (true, null, new AuthUserResult(user.Id, user.FullName, user.Email!, user.PhoneNumber, user.AvatarUrl));
    }

    public async Task<(bool Success, string? Error)> ChangePasswordAsync(
        string userId, string currentPassword, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return (false, "User not found");

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (!result.Succeeded)
            return (false, string.Join(", ", result.Errors.Select(e => e.Description)));

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ForgotPasswordAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
            return (false, "If the email exists, a reset link has been sent");
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ResetPasswordAsync(
        string email, string token, string newPassword)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null) return (false, "Invalid request");

        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded)
            return (false, string.Join(", ", result.Errors.Select(e => e.Description)));

        return (true, null);
    }

    public async Task<(bool Success, string? Error, string? AccessToken, string? RefreshToken)> GenerateTokensAsync(
        string userId, string email, string role)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Role, role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
        };

        var accessTokenExpiration = int.Parse(
            _configuration["Jwt:AccessTokenExpirationMinutes"] ?? "15");

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(accessTokenExpiration),
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        var refreshToken = GenerateRefreshToken();

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(accessToken);
        var jti = jwtToken.Claims.First(c => c.Type == "jti").Value;

        var refreshTokenEntity = new LastHour.Domain.Entities.RefreshToken
        {
            UserId = userId,
            Token = refreshToken,
            JwtId = jti,
            ExpiryDate = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };
        _context.RefreshTokens.Add(refreshTokenEntity);
        await _context.SaveChangesAsync();

        return (true, null, accessToken, refreshToken);
    }

    public async Task<(bool Success, string? Error, string? AccessToken, string? RefreshToken)> RefreshTokenAsync(
        string refreshToken, string jwtId)
    {
        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken && !rt.IsUsed && !rt.IsRevoked);

        if (storedToken == null)
            return (false, "Invalid refresh token", null, null);

        if (storedToken.JwtId != jwtId)
        {
            storedToken.IsRevoked = true;
            await _context.SaveChangesAsync();
            return (false, "Token mismatch - possible theft", null, null);
        }

        if (storedToken.ExpiryDate < DateTime.UtcNow)
        {
            storedToken.IsRevoked = true;
            await _context.SaveChangesAsync();
            return (false, "Refresh token expired", null, null);
        }

        var user = await _userManager.FindByIdAsync(storedToken.UserId);
        if (user == null) return (false, "User not found", null, null);

        storedToken.IsUsed = true;
        await _context.SaveChangesAsync();

        var roles = await _userManager.GetRolesAsync(user);
        return await GenerateTokensAsync(user.Id, user.Email!, roles.FirstOrDefault() ?? "Customer");
    }

    public async Task<(bool Success, string? Error)> RevokeRefreshTokenAsync(string userId)
    {
        var tokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsUsed && !rt.IsRevoked)
            .ToListAsync();

        foreach (var token in tokens) token.IsRevoked = true;
        await _context.SaveChangesAsync();
        return (true, null);
    }

    private string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}
