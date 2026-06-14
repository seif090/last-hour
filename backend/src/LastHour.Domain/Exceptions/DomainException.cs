namespace LastHour.Domain.Exceptions;

public class DomainException : Exception
{
    public string Code { get; }

    public DomainException(string message, string code = "DOMAIN_ERROR")
        : base(message)
    {
        Code = code;
    }

    public DomainException(string message, Exception innerException, string code = "DOMAIN_ERROR")
        : base(message, innerException)
    {
        Code = code;
    }
}
