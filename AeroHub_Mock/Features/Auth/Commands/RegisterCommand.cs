using AeroHub_Mock.Entities;
using AeroHub_Mock.Repositories;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace AeroHub_Mock.Features.Auth.Commands;

public record RegisterCommand(string UserName, string Email, string Password, HttpContext Context) : IRequest<IdentityResult>;

public class RegisterHandler(IUnitOfWork uow, ISender sender) : IRequestHandler<RegisterCommand, IdentityResult>
{
    public async Task<IdentityResult> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var user = new User
        {
            UserName = request.UserName,
            Email = request.Email,
            CreatedAt = DateTime.UtcNow,
        };

        var result = await uow.Users.AddAsync(user, request.Password);
        if (!result.Succeeded)
            return result;

        // New accounts are customers by default.
        await uow.Users.AddToRoleAsync(user, Roles.Customer);
        await uow.SaveChangesAsync(cancellationToken);

        // Login requires a confirmed email, so send the confirmation link straight away.
        await sender.Send(new SendConfirmationEmailCommand { User = user, Context = request.Context, Email = request.Email }, cancellationToken);
        return result;
    }
}
