using System.Security.Claims;
using SkyLinkMock.Repositories;
using MediatR;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace SkyLinkMock.Features.Auth.Commands;

public record RefreshCommand(string RefreshToken) : IRequest<ClaimsPrincipal?>;

// Returns null when the refresh token is invalid, expired or the security stamp changed.
public class RefreshHandler(
    IUnitOfWork uow,
    IOptionsMonitor<BearerTokenOptions> bearerOptions,
    TimeProvider clock) : IRequestHandler<RefreshCommand, ClaimsPrincipal?>
{
    public async Task<ClaimsPrincipal?> Handle(RefreshCommand request, CancellationToken cancellationToken)
    {
        var protector = bearerOptions.Get(IdentityConstants.BearerScheme).RefreshTokenProtector;
        var ticket = protector.Unprotect(request.RefreshToken);

        if (ticket?.Properties?.ExpiresUtc is not { } expires
            || clock.GetUtcNow() >= expires
            || await uow.Users.ValidateSecurityStampAsync(ticket.Principal) is not { } user)
        {
            return null;
        }

        return await uow.Users.CreatePrincipalAsync(user);
    }
}
