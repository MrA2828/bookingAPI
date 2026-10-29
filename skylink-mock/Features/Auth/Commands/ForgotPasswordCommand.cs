using System.Text;
using System.Text.Encodings.Web;
using SkyLinkMock.Entities;
using SkyLinkMock.Repositories;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;

namespace SkyLinkMock.Features.Auth.Commands;

public record ForgotPasswordCommand(string Email) : IRequest;

// Always completes silently so callers can't probe which emails are registered.
public class ForgotPasswordHandler(IUnitOfWork uow, IEmailSender<User> emailSender) : IRequestHandler<ForgotPasswordCommand>
{
    public async Task Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await uow.Users.FindByEmailAsync(request.Email);
        if (user is not null && await uow.Users.IsEmailConfirmedAsync(user))
        {
            var code = await uow.Users.GeneratePasswordResetTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            await emailSender.SendPasswordResetCodeAsync(user, request.Email, HtmlEncoder.Default.Encode(code));
        }
    }
}
