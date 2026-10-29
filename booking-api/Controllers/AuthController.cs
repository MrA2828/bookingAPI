using BookingApi.Features.Auth.Commands;
using BookingApi.Features.Auth.Dtos;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;   // built-in LoginRequest/RefreshRequest/... DTOs
using Microsoft.AspNetCore.Mvc;

namespace BookingApi.Controllers;

[ApiController]
[Route("auth")]
public class AuthController(ISender sender) : ControllerBase
{
    [HttpPost("register")]
    [EndpointSummary("Creates a new user account and sends a confirmation email")]
    [EndpointDescription(@"**When to use:** First step for a new user. Call it once from the sign-up screen.

**How to use:** Send `userName`, `email` and `password` in the JSON body. The password must satisfy the Identity password rules (length, digit, upper/lower case, symbol).

**What happens:** The account is created with an unconfirmed email and a confirmation link is generated.
> **Dev note:** No real email is sent. The confirmation link is written to the **debug console / application log** as a `[DEV EMAIL] Confirm email for ...` warning. Copy the link from there and open it in a browser (or call `GET /auth/confirmEmail`).

**Responses:** `200` created; `400` validation problem (duplicate email/username, weak password).")]
    public async Task<IActionResult> Register([FromBody] DTORegisterRequest request)
    {
        var result = await sender.Send(new RegisterCommand(request.UserName, request.Email, request.Password, HttpContext));
        if (!result.Succeeded)
            return IdentityValidationProblem(result);

        return Ok();
    }

    [HttpPost("login")]
    [EndpointSummary("Logs in and returns a bearer token")]
    [EndpointDescription(@"**When to use:** After the email has been confirmed, to sign the user in.

**How to use:** Send `email` and `password` in the JSON body.

**What happens:** Returns `tokenType`, `accessToken`, `expiresIn` (seconds) and `refreshToken`. Send the access token on protected endpoints as `Authorization: Bearer <accessToken>`.

**Responses:** `200` token issued; `401` wrong credentials, or the email is not confirmed yet (use `/auth/resendConfirmationEmail`).")]
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
    [EndpointDescription(@"**When to use:** When the access token has expired (or is about to), to stay signed in without asking for the password again.

**How to use:** Send the `refreshToken` received from `/auth/login` in the JSON body.

**What happens:** Returns a fresh token response (new access token and new refresh token). Replace the stored tokens with the new ones.

**Responses:** `200` new tokens; `401` the refresh token is invalid or expired, so the user must log in again.")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        var principal = await sender.Send(new RefreshCommand(request.RefreshToken));
        return principal is null
            ? Challenge(IdentityConstants.BearerScheme)
            : SignIn(principal, IdentityConstants.BearerScheme);
    }

    [HttpGet("confirmEmail", Name = SendConfirmationEmailHandler.ConfirmEmailRouteName)]
    [EndpointSummary("Confirms a user's email address")]
    [EndpointDescription(@"**When to use:** After `/auth/register` or `/auth/resendConfirmationEmail`. Normally the user just clicks the link they were sent, so you rarely call this by hand.

**How to use:** The link already contains the query parameters `userId` and `code` (and `changedEmail` only when confirming an email change). Open it as-is.
> **Dev note:** The link is printed in the **debug console / application log** as `[DEV EMAIL] Confirm email for ...`. Copy it from there.

**Responses:** `200` email confirmed; `400` invalid or expired code; `404` user not found.")]
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
    [EndpointSummary("Sends the confirmation email again")]
    [EndpointDescription(@"**When to use:** The user never received the confirmation link, or it expired.

**How to use:** Send the account `email` in the JSON body.

**What happens:** A new confirmation link is generated.
> **Dev note:** It is written to the **debug console / application log** as `[DEV EMAIL] Confirm email for ...`.

**Responses:** `200` link re-sent; `404` no user with this email.")]
    public async Task<IActionResult> ResendConfirmationEmail([FromBody] ResendConfirmationEmailRequest request)
    {
        var found = await sender.Send(new ResendConfirmationEmailCommand(request.Email, HttpContext));
        return found ? Ok() : NotFound("No user found in the database with this Email");
    }

    [HttpPost("forgotPassword")]
    [EndpointSummary("Starts the password reset flow")]
    [EndpointDescription(@"**When to use:** The user forgot their password. This is step 1 of 2 (step 2 is `/auth/resetPassword`).

**How to use:** Send the account `email` in the JSON body.

**What happens:** A reset code is generated for the account if it exists and its email is confirmed. The response is always `200` so callers cannot discover which emails are registered.
> **Dev note:** The reset code is written to the **debug console / application log** as `[DEV EMAIL] Password reset code for ...`. Copy it from there.

**Responses:** `200` always.")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        await sender.Send(new ForgotPasswordCommand(request.Email));
        return Ok();
    }

    [HttpPost("resetPassword")]
    [EndpointSummary("Sets a new password using a reset code")]
    [EndpointDescription(@"**When to use:** Step 2 of the reset flow, after `/auth/forgotPassword`.

**How to use:** Send `email`, `resetCode` (the code from the debug console / application log) and `newPassword` in the JSON body. The new password must satisfy the Identity password rules.

**Responses:** `200` password changed, so the user can log in with it; `400` invalid or expired code, or the password is too weak.")]
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
