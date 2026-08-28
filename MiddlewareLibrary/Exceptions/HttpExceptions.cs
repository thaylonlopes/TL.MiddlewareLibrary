using System;

namespace MiddlewareLibrary.Exceptions;

public class BadRequestException : Exception
{
    public BadRequestException(string message) : base(message) { }
    public BadRequestException(string message, Exception innerException) : base(message, innerException) { }
}

public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message) : base(message) { }
    public UnauthorizedException(string message, Exception innerException) : base(message, innerException) { }
}

public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message) { }
    public ForbiddenException(string message, Exception innerException) : base(message, innerException) { }
}

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
    public NotFoundException(string message, Exception innerException) : base(message, innerException) { }
}

public class NotAcceptableException : Exception
{
    public NotAcceptableException(string message) : base(message) { }
    public NotAcceptableException(string message, Exception innerException) : base(message, innerException) { }
}

public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
    public ConflictException(string message, Exception innerException) : base(message, innerException) { }
}

public class UnsupportedMediaTypeException : Exception
{
    public UnsupportedMediaTypeException(string message) : base(message) { }
    public UnsupportedMediaTypeException(string message, Exception innerException) : base(message, innerException) { }
}

public class LockedException : Exception
{
    public LockedException(string message) : base(message) { }
    public LockedException(string message, Exception innerException) : base(message, innerException) { }
}

public class TooManyRequestsException : Exception
{
    public TooManyRequestsException(string message) : base(message) { }
    public TooManyRequestsException(string message, Exception innerException) : base(message, innerException) { }
}

public class BadGatewayException : Exception
{
    public BadGatewayException(string message) : base(message) { }
    public BadGatewayException(string message, Exception innerException) : base(message, innerException) { }
}

public class GatewayTimeoutException : Exception
{
    public GatewayTimeoutException(string message) : base(message) { }
    public GatewayTimeoutException(string message, Exception innerException) : base(message, innerException) { }
}

public class InsufficientStorageException : Exception
{
    public InsufficientStorageException(string message) : base(message) { }
    public InsufficientStorageException(string message, Exception innerException) : base(message, innerException) { }
}

