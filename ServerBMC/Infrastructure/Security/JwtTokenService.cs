using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using ServerBMC.Domain.Entities;

namespace ServerBMC.Infrastructure.Security;

public class JwtOptions
{
    public string Issuer { get; set; } = null!;
    public string Audience { get; set; } = null!;
    public string Key { get; set; } = null!;
    public int AccessTokenLifetimeMinutes { get; set; } = 480;
}

public interface IJwtTokenService
{
    string CreateAccessToken(User user, IEnumerable<Role> roles);
}

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _opt;
    public JwtTokenService(JwtOptions opt) { _opt = opt; }

    public string CreateAccessToken(User user, IEnumerable<Role> roles)
    {
        var roleList = roles.ToList();
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        claims.AddRange(roleList.Select(r => new Claim(ClaimTypes.Role, r.Code)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _opt.Issuer,
            audience: _opt.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(_opt.AccessTokenLifetimeMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal principal)
    {
        var raw = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
                  ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.Parse(raw!);
    }

    public static string? GetEmail(this ClaimsPrincipal principal)
        => principal.FindFirstValue(JwtRegisteredClaimNames.Email)
           ?? principal.FindFirstValue(ClaimTypes.Email);
}