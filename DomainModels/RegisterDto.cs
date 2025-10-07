using System.ComponentModel.DataAnnotations;

namespace DomainModels;

public class RegisterDto
{
    [Required, EmailAddress]
    public required string Email { get; set; }

    [Required, MinLength(2)]
    public required string Username { get; set; }

    [Required, MinLength(6)]
    public required string Password { get; set; }

    [Required, MinLength(8)]
    public required string PhoneNumber { get; set; }
}
