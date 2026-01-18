namespace RenuMate.Api.Exceptions;

public class DomainValidationException(string message) : DomainException(message);