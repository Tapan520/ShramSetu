using Microsoft.AspNetCore.Identity;

namespace ShramSetu.Services;

public interface IJwtTokenService
{
    string GenerateToken(IdentityUser user, IList<string> roles);
}
