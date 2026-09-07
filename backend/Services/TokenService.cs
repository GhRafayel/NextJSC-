using Microsoft.Extensions.Options;
using Backend.Options;
using Backend.Models;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Cryptography;
using Backend.Dtos;

namespace Backend.Services;

public class TokenService(IOptions<JwtOptions> options)
{
    private readonly JwtOptions _jwt = options.Value;

    public string CreateAccessToken(User user)
    {
        List<Claim> claims =
        [
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Role, user.Role.ToString()),
        ];
        
        SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(_jwt.Key));
        SigningCredentials creds = new(key, SecurityAlgorithms.HmacSha256);

        JwtSecurityToken token = new(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwt.AccessTokenMinutes),
            signingCredentials: creds
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public  (string Token, DateTime ExpiresAt) CreateRefreshToken ()
    {
        byte[]      bytes = RandomNumberGenerator.GetBytes(32);
        string      token = Convert.ToBase64String(bytes);
        DateTime    expiresAt = DateTime.UtcNow.AddDays(_jwt.RefreshTokenDays);

        return (token, expiresAt);
    }
}