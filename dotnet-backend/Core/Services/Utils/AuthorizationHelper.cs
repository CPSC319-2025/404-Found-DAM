using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System;

public static class AuthorizationHelper
{
    public static int? GetUserIdFromToken(HttpRequest request)
    {
        if (!request.Headers.TryGetValue("Authorization", out StringValues authorizationHeader))
        {
            return null; // No Authorization header
        }

        var token = authorizationHeader.ToString().Replace("Bearer ", "");

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadToken(token) as JwtSecurityToken;
            if (jwtToken == null)
            {
                return null; // Invalid token
            }

            var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "userId");
            if (userIdClaim == null)
            {
                return null; // userId not found in token
            }

            if (int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }

            return null; // If parsing fails, return null
        }
        catch (Exception)
        {
            return null; // Error decoding token
        }
    }
}
