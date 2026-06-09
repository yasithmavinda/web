namespace TaskFlow.Domain.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string entity, object key) : base($"{entity} with id '{key}' was not found.") { }
    public NotFoundException(string message) : base(message) { }
}

public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message = "You are not authorized to perform this action.") : base(message) { }
}

public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}

public class BadRequestException : Exception
{
    public BadRequestException(string message) : base(message) { }
}

public class ValidationException : Exception
{
    public Dictionary<string, string[]> Errors { get; }
    public ValidationException(Dictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }
}

public class AccountLockedException : Exception
{
    public DateTime? LockedUntil { get; }
    public AccountLockedException(DateTime? lockedUntil)
        : base("Account is temporarily locked due to too many failed login attempts.")
    {
        LockedUntil = lockedUntil;
    }
}
