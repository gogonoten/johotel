using System.ComponentModel.DataAnnotations;

namespace DomainModels
{
    public class UpdateProfileDto
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(2)]
        public string Username { get; set; } = string.Empty;

        [Required, MinLength(8)]
        public string PhoneNumber { get; set; } = string.Empty;
    }
}
