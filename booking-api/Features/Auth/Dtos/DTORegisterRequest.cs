namespace SkyWatch.Features.Auth.Dtos;

/// <summary>The request type for the "/auth/register" endpoint.</summary>
public sealed class DTORegisterRequest
{
    public required string UserName { get; set; }
    public required string Email { get; init; }
    public required string Password { get; init; }
}
