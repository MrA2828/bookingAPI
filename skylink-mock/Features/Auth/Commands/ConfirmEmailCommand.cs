using System.Text;
using SkyLinkMock.Repositories;
using MediatR;
using Microsoft.AspNetCore.WebUtilities;

namespace SkyLinkMock.Features.Auth.Commands;

public enum ConfirmEmailStatus { Success, NotFound, BadRequest }

public record ConfirmEmailResult(ConfirmEmailStatus Status, string Message);

public record ConfirmEmailCommand(string UserId, string Code, string? ChangedEmail) : IRequest<ConfirmEmailResult>;

public class ConfirmEmailHandler(IUnitOfWork uow) : IRequestHandler<ConfirmEmailCommand, ConfirmEmailResult>
{
    public async Task<ConfirmEmailResult> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        if (await uow.Users.FindByIdAsync(request.UserId) is not { } user)
            return new(ConfirmEmailStatus.NotFound, "No user found in the database with this Id");

        string code;
        try
        {
            code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Code));
        }
        catch (FormatException)
        {
            return new(ConfirmEmailStatus.BadRequest, "could not Decode code, check if copied correct code");
        }

        var result = string.IsNullOrEmpty(request.ChangedEmail)
            ? await uow.Users.ConfirmEmailAsync(user, code)
            : await uow.Users.ChangeEmailAsync(user, request.ChangedEmail, code);

        return result.Succeeded
            ? new(ConfirmEmailStatus.Success, "Thank you for confirming your email.")
            : new(ConfirmEmailStatus.BadRequest, "Invalid or expired confirmation code.");
    }
}
