using System.ComponentModel.DataAnnotations;
using FinalProject.MVC.Commerce;

namespace FinalProject.MVC.Models;

public sealed class TeamMemberEditViewModel
{
    public int Id { get; set; }

    [Required, MaxLength(120), Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(120), Display(Name = "Position")]
    public string Position { get; set; } = string.Empty;

    // The photo is supplied by the file input. Requiring this hidden path blocks
    // Create before the upload is saved, and the form only summarized model-level errors.
    [MaxLength(500)]
    public string ImagePath { get; set; } = string.Empty;

    [Required, MaxLength(2000), Display(Name = "Biography")]
    public string Biography { get; set; } = string.Empty;

    [Required, MaxLength(200), Display(Name = "Experience")]
    public string Experience { get; set; } = string.Empty;

    [Required, MaxLength(120), Display(Name = "Department")]
    public string Department { get; set; } = string.Empty;

    [Required, AzerbaijanPhone, Display(Name = "Phone")]
    public string Phone { get; set; } = string.Empty;

    [Required, MaxLength(200), EmailAddress, Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Sort order")]
    [Range(0, 100000)]
    public int SortOrder { get; set; }

    [Display(Name = "Published")]
    public bool IsPublished { get; set; } = true;
}
