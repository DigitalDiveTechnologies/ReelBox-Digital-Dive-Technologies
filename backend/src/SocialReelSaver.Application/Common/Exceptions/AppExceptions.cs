namespace SocialReelSaver.Application.Common.Exceptions;

public sealed class ConflictException : Exception
{
    public ConflictException(string message)
        : base(message)
    {
    }
}

public sealed class UnauthorizedAppException : Exception
{
    public UnauthorizedAppException(string message)
        : base(message)
    {
    }
}

public sealed class NotFoundException : Exception
{
    public NotFoundException(string message)
        : base(message)
    {
    }
}

public sealed class BadRequestException : Exception
{
    public BadRequestException(string message, string? code = null)
        : base(message)
    {
        Code = code;
    }

    public string? Code { get; }
}
