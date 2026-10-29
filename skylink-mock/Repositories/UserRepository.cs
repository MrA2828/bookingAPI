using System.Security.Claims;
using SkyLinkMock.Entities;
using Microsoft.AspNetCore.Identity;

namespace SkyLinkMock.Repositories;

// Thin wrapper over Identity's UserManager / SignInManager so handlers never touch them directly.
public class UserRepository(UserManager<User> userManager, SignInManager<User> signInManager) : IUserRepository
{
    public Task<User?> FindByEmailAsync(string email) => userManager.FindByEmailAsync(email);
    public Task<User?> FindByIdAsync(string id) => userManager.FindByIdAsync(id);
    public Task<IdentityResult> AddAsync(User user, string password) => userManager.CreateAsync(user, password);
    public Task AddToRoleAsync(User user, string role) => userManager.AddToRoleAsync(user, role);

    public Task<bool> CheckPasswordAsync(User user, string password) => userManager.CheckPasswordAsync(user, password);
    public Task<bool> IsEmailConfirmedAsync(User user) => userManager.IsEmailConfirmedAsync(user);

    public Task<SignInResult> CheckPasswordSignInAsync(User user, string password) =>
        signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);

    public Task<ClaimsPrincipal> CreatePrincipalAsync(User user) => signInManager.CreateUserPrincipalAsync(user);
    public Task<User?> ValidateSecurityStampAsync(ClaimsPrincipal principal) => signInManager.ValidateSecurityStampAsync(principal);

    public Task<string> GenerateEmailConfirmationTokenAsync(User user) => userManager.GenerateEmailConfirmationTokenAsync(user);
    public Task<string> GenerateChangeEmailTokenAsync(User user, string newEmail) => userManager.GenerateChangeEmailTokenAsync(user, newEmail);
    public Task<IdentityResult> ConfirmEmailAsync(User user, string code) => userManager.ConfirmEmailAsync(user, code);
    public Task<IdentityResult> ChangeEmailAsync(User user, string newEmail, string code) => userManager.ChangeEmailAsync(user, newEmail, code);

    public Task<string> GeneratePasswordResetTokenAsync(User user) => userManager.GeneratePasswordResetTokenAsync(user);
    public Task<IdentityResult> ResetPasswordAsync(User user, string code, string newPassword) => userManager.ResetPasswordAsync(user, code, newPassword);
    public IdentityError InvalidToken() => userManager.ErrorDescriber.InvalidToken();
}
