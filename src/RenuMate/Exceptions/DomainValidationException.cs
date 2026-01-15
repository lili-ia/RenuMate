namespace RenuMate.Exceptions;

public class DomainValidationException(string message) : DomainException(message);