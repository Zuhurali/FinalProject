using System.ComponentModel.DataAnnotations;
using FinalProject.MVC.Commerce;

namespace FinalProject.MVC.Models;

public sealed class FaqItemEditViewModel
{
    public int Id { get; set; }

    [Required]
    [MinLength(FaqContentRules.QuestionMinLength, ErrorMessage = "Question must be at least {1} characters.")]
    [MaxLength(FaqContentRules.QuestionMaxLength, ErrorMessage = "Question must be {1} characters or fewer.")]
    [Display(Name = "Question")]
    public string Question { get; set; } = string.Empty;

    [Required]
    [MinLength(FaqContentRules.AnswerMinLength, ErrorMessage = "Answer must be at least {1} characters.")]
    [MaxLength(FaqContentRules.AnswerMaxLength, ErrorMessage = "Answer must be {1} characters or fewer.")]
    [Display(Name = "Answer")]
    public string Answer { get; set; } = string.Empty;

    [Display(Name = "Sort order")]
    [Range(0, 100000)]
    public int SortOrder { get; set; }

    [Display(Name = "Published")]
    public bool IsPublished { get; set; } = true;
}
