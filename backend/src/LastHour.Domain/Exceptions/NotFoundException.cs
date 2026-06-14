namespace LastHour.Domain.Exceptions;

public class NotFoundException : DomainException
{
    public NotFoundException(string entityName, string id)
        : base($"{entityName} with id '{id}' was not found", "NOT_FOUND")
    {
    }

    public NotFoundException(string entityName, string propertyName, string value)
        : base($"{entityName} with {propertyName} '{value}' was not found", "NOT_FOUND")
    {
    }
}
