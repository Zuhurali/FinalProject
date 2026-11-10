namespace FinalProject.MVC.Commerce;

/// <summary>FAQ length rules aligned with SQL CHECK constraints on FaqItems.</summary>
public static class FaqContentRules
{
    public const int QuestionMinLength = 3;
    public const int QuestionMaxLength = 300;
    public const int AnswerMinLength = 3;
    public const int AnswerMaxLength = 4000;

    public static string? ValidateQuestion(string? question)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            return "Question is required.";
        }

        var length = question.Trim().Length;
        if (length < QuestionMinLength)
        {
            return $"Question must be at least {QuestionMinLength} characters.";
        }

        if (length > QuestionMaxLength)
        {
            return $"Question must be {QuestionMaxLength} characters or fewer.";
        }

        return null;
    }

    public static string? ValidateAnswer(string? answer)
    {
        if (string.IsNullOrWhiteSpace(answer))
        {
            return "Answer is required.";
        }

        var length = answer.Trim().Length;
        if (length < AnswerMinLength)
        {
            return $"Answer must be at least {AnswerMinLength} characters.";
        }

        if (length > AnswerMaxLength)
        {
            return $"Answer must be {AnswerMaxLength} characters or fewer.";
        }

        return null;
    }
}
