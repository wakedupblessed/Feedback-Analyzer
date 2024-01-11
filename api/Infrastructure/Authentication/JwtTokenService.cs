﻿using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FeedbackAnalyzer.Application.Abstraction;
using FeedbackAnalyzer.Application.Features.Token;
using FeedbackAnalyzer.Application.Shared;
using FeedbackAnalyzer.Application.Shared.EntityErrors;
using Identity;
using Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Authentication;

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _jwtSettings;
    private readonly UserManager<ApplicationUser> _userManager;

    public JwtTokenService(IOptions<JwtSettings> jwtSettings, UserManager<ApplicationUser> userManager)
    {
        _jwtSettings = jwtSettings.Value;
        _userManager = userManager;
    }

    public async Task<Result<TokenDto>> GenerateTokenPairAsync(ApplicationUser user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();

        var claims = await GenerateUserClaims(user);

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(6),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_jwtSettings.SecretKey)),
                SecurityAlgorithms.HmacSha256Signature),
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience
        };

        var securityToken = tokenHandler.CreateToken(descriptor);

        var accessToken = tokenHandler.WriteToken(securityToken);

        var refreshToken = GenerateRefreshToken();

        return new TokenDto(accessToken, refreshToken);
    }

    public async Task<Result<TokenDto>> RefreshTokenAsync(RefreshTokenDto tokenModel)
    {
        var principal = GetPrincipalFromExpiredToken(tokenModel.AccessToken);

        if (principal?.FindFirstValue(ClaimTypes.Email) is null)
        {
            return Result<TokenDto>.Failure(IdentityUserErrors.NotValidToken());
        }
        
        var user = await _userManager.FindByEmailAsync(principal.FindFirstValue(ClaimTypes.Email)!);

        if (user is null)
        {
            return Result<TokenDto>.Failure(IdentityUserErrors.NotFound(principal.FindFirstValue(ClaimTypes.Email)!));
        }
        
        if (user.RefreshToken != tokenModel.RefreshToken ||
            user.RefreshTokenExpiryTime <= DateTime.Now)
        {
            return Result<TokenDto>.Failure(IdentityUserErrors.NotValidToken());
        }
        
        return await GenerateTokenPairAsync(user);
    }

    #region Private Methods

    private static string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];

        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);

        return Convert.ToBase64String(randomNumber);
    }

    private async Task<List<Claim>> GenerateUserClaims(ApplicationUser user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.FullName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new("uid", user.Id)
        };

        var roles = await _userManager.GetRolesAsync(user);

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        return claims;
    }

    private ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey)),
            ValidateLifetime = true
        };

        var tokenHandler = new JwtSecurityTokenHandler();

        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

        return CheckSecurityToken(securityToken) ? null : principal;
    }

    private static bool CheckSecurityToken(SecurityToken securityToken) =>
        securityToken is not JwtSecurityToken jwtSecurityToken ||
        !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256,
            StringComparison.InvariantCultureIgnoreCase);

    #endregion
}