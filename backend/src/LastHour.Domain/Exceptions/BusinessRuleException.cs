namespace LastHour.Domain.Exceptions;

public class BusinessRuleException : DomainException
{
    public BusinessRuleException(string message, string code = "BUSINESS_RULE_VIOLATION")
        : base(message, code)
    {
    }
}
