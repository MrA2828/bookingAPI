using System.Security.Claims;
using SkyLinkMock.Repositories;
using MediatR;

namespace SkyLinkMock.Features.Auth.Commands;

public record LoginCommand(string Email, string Password) : IRequest<LoginResult>;

// Principal is set on success; the controller turns it into the bearer token response.
public record LoginResult(ClaimsPrincipal? Principal, string? Error = null);

public class LoginHandler(IUnitOfWork uow) : IRequestHandler<LoginCommand, LoginResult>
{
    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await uow.Users.FindByEmailAsync(request.Email);
#if DEBUG
        if (user is null)
            return new LoginResult(null, "DEBUG: no account found for this email");
        if (!await uow.Users.CheckPasswordAsync(user, request.Password))
            return new LoginResult(null, "DEBUG: password does not match");
        if (!await uow.Users.IsEmailConfirmedAsync(user))
            return new LoginResult(null, "DEBUG: Email not confirmed");
#else
        if (user is null)
            return new LoginResult(null, "Invalid email or password");
#endif
        var result = await uow.Users.CheckPasswordSignInAsync(user, request.Password);
        if (!result.Succeeded)
            return new LoginResult(null, result.ToString());

        return new LoginResult(await uow.Users.CreatePrincipalAsync(user));
    }
}
