using System.Text;
using BookingApi.Repositories;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;

namespace BookingApi.Features.Auth.Commands;

public record ResetPasswordCommand(string Email, string ResetCode, string NewPassword) : IRequest<IdentityResult>;

public class ResetPasswordHandler(IUnitOfWork uow) : IRequestHandler<ResetPasswordCommand, IdentityResult>
{
    public async Task<IdentityResult> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await uow.Users.FindByEmailAsync(request.Email);
        if (user is null || !await uow.Users.IsEmailConfirmedAsync(user))
            return IdentityResult.Failed(uow.Users.InvalidToken());

        try
        {
            var code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.ResetCode));
            return await uow.Users.ResetPasswordAsync(user, code, request.NewPassword);
        }
        catch (FormatException)
        {
            return IdentityResult.Failed(uow.Users.InvalidToken());
        }
    }
}
