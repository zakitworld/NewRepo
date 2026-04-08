using EventHub.Api.Data;
using EventHub.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace EventHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ApiDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthController> _logger;

    public AuthController(ApiDbContext db, IConfiguration config,
                          ILogger<AuthController> logger)
    {
        _db     = db;
        _config = config;
        _logger = logger;
    }

    // ── POST /api/auth/register ───────────────────────────────────────────
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        if (await _db.Users.AnyAsync(u => u.Email == req.Email))
            return Conflict("Email already registered");

        var user = new ApiUser
        {
            Id           = Guid.NewGuid().ToString(),
            Email        = req.Email.Trim().ToLowerInvariant(),
            FullName     = req.FullName.Trim(),
            PasswordHash = HashPassword(req.Password),
            Role         = "User",
            CreatedAt    = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        _logger.LogInformation("New user registered: {Email}", user.Email);
        return Ok(new AuthResponse(IssueToken(user), user.Id, user.FullName, user.Email, user.Role));
    }

    // ── POST /api/auth/login ──────────────────────────────────────────────
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var user = await _db.Users.FirstOrDefaultAsync(
            u => u.Email == req.Email.Trim().ToLowerInvariant());

        if (user == null || !VerifyPassword(req.Password, user.PasswordHash))
            return Unauthorized("Invalid email or password");

        _logger.LogInformation("User logged in: {Email}", user.Email);
        return Ok(new AuthResponse(IssueToken(user), user.Id, user.FullName, user.Email, user.Role));
    }

    // ── POST /api/auth/forgot-password ────────────────────────────────────
    /// <summary>
    /// Issues a one-time 6-digit reset code and emails it.
    /// In production wire this to your email provider; here we just return the
    /// code so mobile dev can test without an SMTP server.
    /// </summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest req)
    {
        var user = await _db.Users.FirstOrDefaultAsync(
            u => u.Email == req.Email.Trim().ToLowerInvariant());

        // Always return 200 to avoid user-enumeration
        if (user == null)
            return Ok(new { message = "If that email exists, a reset code has been sent." });

        var bytes = new byte[4];
        RandomNumberGenerator.Fill(bytes);
        var code = (Math.Abs(BitConverter.ToInt32(bytes, 0)) % 900000 + 100000).ToString();

        // Store hashed code with 15-minute expiry as a pseudo-column via PasswordHash prefix
        // Format: RESET|<code-hash>|<expiry-ticks>|<original-hash>
        var expiry      = DateTime.UtcNow.AddMinutes(15).Ticks;
        var codeHash    = HashPassword(code);
        user.PasswordHash = $"RESET|{codeHash}|{expiry}|{user.PasswordHash}";
        await _db.SaveChangesAsync();

        _logger.LogInformation("Password reset code issued for {Email} (dev: {Code})", user.Email, code);

        // TODO: send code via email/SMS in production
        return Ok(new { message = "If that email exists, a reset code has been sent.", devCode = code });
    }

    // ── POST /api/auth/reset-password ─────────────────────────────────────
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req)
    {
        var user = await _db.Users.FirstOrDefaultAsync(
            u => u.Email == req.Email.Trim().ToLowerInvariant());

        if (user == null || !user.PasswordHash.StartsWith("RESET|"))
            return BadRequest("Invalid or expired reset code");

        var parts = user.PasswordHash.Split('|');
        if (parts.Length != 4)
            return BadRequest("Invalid or expired reset code");

        var codeHash     = parts[1];
        var expiry       = new DateTime(long.Parse(parts[2]), DateTimeKind.Utc);
        var originalHash = parts[3];

        if (DateTime.UtcNow > expiry)
        {
            user.PasswordHash = originalHash;
            await _db.SaveChangesAsync();
            return BadRequest("Reset code has expired");
        }

        if (!VerifyPassword(req.Code, codeHash))
            return BadRequest("Invalid reset code");

        user.PasswordHash = HashPassword(req.NewPassword);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Password reset successfully for {Email}", user.Email);
        return Ok(new { message = "Password updated successfully" });
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private string IssueToken(ApiUser user)
    {
        var key     = _config["Jwt:Key"] ?? "CHANGE_THIS_32_CHAR_SECRET_IN_PROD!";
        var issuer  = _config["Jwt:Issuer"]   ?? "EventHub";
        var audience= _config["Jwt:Audience"] ?? "EventHubApp";

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Name,  user.FullName),
            new Claim(ClaimTypes.Role,               user.Role),
            new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString())
        };

        var creds   = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer:             issuer,
            audience:           audience,
            claims:             claims,
            expires:            DateTime.UtcNow.AddDays(30),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string HashPassword(string password)
    {
        var salt = new byte[16];
        RandomNumberGenerator.Fill(salt);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password, salt, 100_000, HashAlgorithmName.SHA256, 32);
        var full = new byte[48];
        Buffer.BlockCopy(salt, 0, full, 0,  16);
        Buffer.BlockCopy(hash, 0, full, 16, 32);
        return Convert.ToBase64String(full);
    }

    private static bool VerifyPassword(string password, string storedHash)
    {
        try
        {
            var full = Convert.FromBase64String(storedHash);
            if (full.Length != 48) return false;
            var salt = full[..16];
            var hash = full[16..];
            var derived = Rfc2898DeriveBytes.Pbkdf2(
                password, salt, 100_000, HashAlgorithmName.SHA256, 32);
            return CryptographicOperations.FixedTimeEquals(derived, hash);
        }
        catch { return false; }
    }
}

// ── Request / response records ─────────────────────────────────────────────
public record RegisterRequest(string Email, string Password, string FullName);
public record LoginRequest(string Email, string Password);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Email, string Code, string NewPassword);
public record AuthResponse(string Token, string UserId, string FullName, string Email, string Role);
