using AeroHub_Mock.Features.Auth.Commands;
using AeroHub_Mock.Features.Auth.Dtos;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;   // built-in LoginRequest/RefreshRequest/... DTOs
using Microsoft.AspNetCore.Mvc;

namespace AeroHub_Mock.Controllers;

[ApiController]
[Route("auth")]
public class AuthController(ISender sender) : ControllerBase
{
    [HttpPost("register")]
    [EndpointSummary("Creates a new user account and sends a confirmation email")]
    public async Task<IActionResult> Register([FromBody] DTORegisterRequest request)
    {
        var result = await sender.Send(new RegisterCommand(request.UserName, request.Email, request.Password, HttpContext));
        if (!result.Succeeded)
            return IdentityValidationProblem(result);

        return Ok();
    }

    [HttpPost("login")]
    [EndpointSummary("Logs in and returns a bearer token")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await sender.Send(new LoginCommand(request.Email, request.Password));
        if (result.Principal is null)
            return Problem(result.Error, statusCode: StatusCodes.Status401Unauthorized);

        // Writes the bearer token (access + refresh) as JSON instead of setting an auth cookie.
        return SignIn(result.Principal, IdentityConstants.BearerScheme);
    }

    [HttpPost("refresh")]
    [EndpointSummary("Exchanges a refresh token for a new bearer token")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        var principal = await sender.Send(new RefreshCommand(request.RefreshToken));
        return principal is null
            ? Challenge(IdentityConstants.BearerScheme)
            : SignIn(principal, IdentityConstants.BearerScheme);
    }

    [HttpGet("confirmEmail", Name = SendConfirmationEmailHandler.ConfirmEmailRouteName)]
    public async Task<IActionResult> ConfirmEmail(
        [FromQuery] string userId,
        [FromQuery] string code,
        [FromQuery] string? changedEmail)
    {
        var result = await sender.Send(new ConfirmEmailCommand(userId, code, changedEmail));
        return result.Status switch
        {
            ConfirmEmailStatus.NotFound => NotFound(result.Message),
            ConfirmEmailStatus.BadRequest => BadRequest(result.Message),
            _ => Content(result.Message),
        };
    }

    [HttpPost("resendConfirmationEmail")]
    public async Task<IActionResult> ResendConfirmationEmail([FromBody] ResendConfirmationEmailRequest request)
    {
        var found = await sender.Send(new ResendConfirmationEmailCommand(request.Email, HttpContext));
        return found ? Ok() : NotFound("No user found in the database with this Email");
    }

    [HttpPost("forgotPassword")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        await sender.Send(new ForgotPasswordCommand(request.Email));
        return Ok();
    }

    [HttpPost("resetPassword")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var result = await sender.Send(new ResetPasswordCommand(request.Email, request.ResetCode, request.NewPassword));
        return result.Succeeded ? Ok() : IdentityValidationProblem(result);
    }

    private ActionResult IdentityValidationProblem(IdentityResult result)
    {
        foreach (var error in result.Errors)
            ModelState.AddModelError(error.Code, error.Description);
        return ValidationProblem(ModelState);
    }
}
