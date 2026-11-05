using System.ComponentModel.DataAnnotations;
using FinalProject.MVC.Commerce;

namespace FinalProject.MVC.Models;

public class FaqItem : BaseEntity
{
    public FaqItem()
    {
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    [Required, MinLength(FaqContentRules.QuestionMinLength), MaxLength(FaqContentRules.QuestionMaxLength)]
    public string Question { get; set; } = string.Empty;

    [Required, MinLength(FaqContentRules.AnswerMinLength), MaxLength(FaqContentRules.AnswerMaxLength)]
    public string Answer { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public bool IsPublished { get; set; } = true;

    public bool ShowOnFaqPage { get; set; } = true;

    public bool ShowOnShop { get; set; }
}
