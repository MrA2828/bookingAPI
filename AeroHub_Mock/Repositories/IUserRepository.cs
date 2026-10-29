using System.Security.Claims;
using AeroHub_Mock.Entities;
using Microsoft.AspNetCore.Identity;

namespace AeroHub_Mock.Repositories;

public interface IUserRepository
{
    Task<User?> FindByEmailAsync(string email);
    Task<User?> FindByIdAsync(string id);
    Task<IdentityResult> AddAsync(User user, string password);
    Task AddToRoleAsync(User user, string role);

    Task<bool> CheckPasswordAsync(User user, string password);
    Task<bool> IsEmailConfirmedAsync(User user);
    Task<SignInResult> CheckPasswordSignInAsync(User user, string password);
    Task<ClaimsPrincipal> CreatePrincipalAsync(User user);
    Task<User?> ValidateSecurityStampAsync(ClaimsPrincipal principal);

    Task<string> GenerateEmailConfirmationTokenAsync(User user);
    Task<string> GenerateChangeEmailTokenAsync(User user, string newEmail);
    Task<IdentityResult> ConfirmEmailAsync(User user, string code);
    Task<IdentityResult> ChangeEmailAsync(User user, string newEmail, string code);

    Task<string> GeneratePasswordResetTokenAsync(User user);
    Task<IdentityResult> ResetPasswordAsync(User user, string code, string newPassword);
    IdentityError InvalidToken();
}
