using Microsoft.AspNetCore.Identity;

namespace SkyLinkMock.Entities;

// The ASP.NET Identity login user as well as the domain user.
// IdentityUser supplies Id (string/GUID), UserName, Email, PasswordHash, etc.
// The role lives in the roles tables (UserManager.AddToRoleAsync / IsInRoleAsync), not a column here.
public class User : IdentityUser
{
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}
