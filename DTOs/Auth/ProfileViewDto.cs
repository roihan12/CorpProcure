using System.ComponentModel.DataAnnotations;

namespace CorpProcure.DTOs.Auth
{
    public class ProfileViewDto
    {


        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Full Name")]
        public string? FullName { get; set; }

        [Display(Name = "Department")]
        public string? Department { get; set; }

        [Display(Name = "Position")]
        public string? Position { get; set; }

        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

    }
}
