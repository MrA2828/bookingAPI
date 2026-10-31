using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace SkyWatch.Features.Aircraft.Dtos;

// Route of GET /aircraft/{hex}.
public sealed class GetAircraftRequestDto
{
    [FromRoute(Name = "hex")]
    [RegularExpression("^[0-9a-fA-F]{6}$", ErrorMessage = "hex must be six hexadecimal characters")]
    public string Hex { get; init; } = "";
}
