using SkyWatch.Repositories;
using MediatR;

namespace SkyWatch.Features.Auth.Commands;

// Returns false when no user has this email.
public record ResendConfirmationEmailCommand(string Email, HttpContext Context) : IRequest<bool>;

public class ResendConfirmationEmailHandler(IUnitOfWork uow, ISender sender) : IRequestHandler<ResendConfirmationEmailCommand, bool>
{
    public async Task<bool> Handle(ResendConfirmationEmailCommand request, CancellationToken cancellationToken)
    {
        if (await uow.Users.FindByEmailAsync(request.Email) is not { } user)
            return false;

        await sender.Send(new SendConfirmationEmailCommand { User = user, Context = request.Context, Email = request.Email }, cancellationToken);
        return true;
    }
}
