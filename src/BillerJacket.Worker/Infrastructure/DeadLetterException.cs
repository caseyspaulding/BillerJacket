namespace BillerJacket.Worker.Infrastructure;

public class DeadLetterException : Exception
{
    public string Reason { get; }

    public DeadLetterException(string reason) : base(reason)
    {
        Reason = reason;
    }

    public DeadLetterException(string reason, Exception innerException) : base(reason, innerException)
    {
        Reason = reason;
    }
}
