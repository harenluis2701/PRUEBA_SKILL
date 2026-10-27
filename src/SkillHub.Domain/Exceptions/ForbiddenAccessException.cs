namespace SkillHub.Domain.Exceptions;

public class ForbiddenAccessException : DomainException
{
    public ForbiddenAccessException(string message) : base(message)
    {
    }
}
