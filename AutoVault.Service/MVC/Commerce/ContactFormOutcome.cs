using Microsoft.AspNetCore.Http;

namespace FinalProject.MVC.Commerce;

public static class ContactFormOutcome
{
    public const string SentSuccess =
        "Thank you. Your message was sent. Our team will get back to you shortly.";

    public const string SavedEmailUnavailable =
        "Your inquiry was saved, but email delivery is currently unavailable. We still have your request and will follow up.";

    public const string SavedEmailFailed =
        "Your inquiry was saved, but we could not email the store just now. We still have your request and will follow up.";

    public const string SaveFailed =
        "We could not save your inquiry. Please try again, or email barisking333@gmail.com directly.";

    public readonly record struct Result(int StatusCode, bool Success, bool EmailSent, string Message);

    public static Result AfterSuccessfulSave(bool emailed, bool emailUnavailable)
    {
        if (emailed)
        {
            return new Result(StatusCodes.Status200OK, true, true, SentSuccess);
        }

        if (emailUnavailable)
        {
            return new Result(StatusCodes.Status200OK, true, false, SavedEmailUnavailable);
        }

        return new Result(StatusCodes.Status200OK, true, false, SavedEmailFailed);
    }
}
