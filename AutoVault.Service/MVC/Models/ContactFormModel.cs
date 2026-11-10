using System.ComponentModel.DataAnnotations;

namespace FinalProject.MVC.Models;

public class ContactFormModel
{
    [Display(Name = "Name")]
    [Required(ErrorMessage = "Enter your name.")]
    [StringLength(80, MinimumLength = 2, ErrorMessage = "Enter your name.")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Email")]
    [Required(ErrorMessage = "Enter a valid email address.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(200, ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Phone")]
    [Required(ErrorMessage = "Enter a valid phone number.")]
    [StringLength(25, ErrorMessage = "Enter a valid phone number.")]
    public string Phone { get; set; } = string.Empty;

    /// <summary>Country selected in the international phone field. Not stored separately.</summary>
    public string? PhoneCountry { get; set; }

    [Display(Name = "Car")]
    [Required(ErrorMessage = "Tell us which part or car you are interested in.")]
    [StringLength(120, MinimumLength = 2, ErrorMessage = "Tell us which part or car you are interested in.")]
    public string Car { get; set; } = string.Empty;

    [Display(Name = "Message")]
    [Required(ErrorMessage = "Enter a message.")]
    [StringLength(1000, MinimumLength = 10, ErrorMessage = "Write at least 10 characters in your message.")]
    public string Message { get; set; } = string.Empty;
}
