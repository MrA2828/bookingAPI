using System.Text;
using AeroHub_Mock.Entities;
using AeroHub_Mock.Repositories;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;

namespace AeroHub_Mock.Features.Auth.Commands;

// A command carries DATA only. Services (repositories, LinkGenerator, the email sender) are injected into the handler.
public class SendConfirmationEmailCommand : IRequest
{
    public required User User { get; set; }
    public required HttpContext Context { get; set; }   // needed to build an absolute URL (scheme + host)
    public required string Email { get; set; }
    public bool IsChange { get; set; } = false;
}

public class SendConfirmationEmailHandler(
    IUnitOfWork uow,
    IEmailSender<User> emailSender,
    LinkGenerator linkGenerator) : IRequestHandler<SendConfirmationEmailCommand>
{
    // The confirmEmail action must be annotated with
    // [HttpGet("confirmEmail", Name = SendConfirmationEmailHandler.ConfirmEmailRouteName)]
    // for the link generator to find it.
    public const string ConfirmEmailRouteName = "ConfirmEmail";

    public async Task Handle(SendConfirmationEmailCommand request, CancellationToken cancellationToken)
    {
        var code = request.IsChange
            ? await uow.Users.GenerateChangeEmailTokenAsync(request.User, request.Email)
            : await uow.Users.GenerateEmailConfirmationTokenAsync(request.User);
        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

        var routeValues = new RouteValueDictionary
        {
            ["userId"] = request.User.Id,
            ["code"] = code,
        };

        if (request.IsChange)
        {
            // This is validated by the /confirmEmail endpoint on change.
            routeValues.Add("changedEmail", request.Email);
        }

        var confirmEmailUrl = linkGenerator.GetUriByName(request.Context, ConfirmEmailRouteName, routeValues)
            ?? throw new NotSupportedException($"Could not find endpoint named '{ConfirmEmailRouteName}'.");

        await emailSender.SendConfirmationLinkAsync(request.User, request.Email, confirmEmailUrl);
    }
}
