namespace FinalProject.MVC.Services;

public sealed class CarApiException : Exception
{
    public CarApiException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
