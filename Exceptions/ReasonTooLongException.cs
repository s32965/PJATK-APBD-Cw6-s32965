namespace WebApplication1.Exceptions;

public class ReasonTooLongException(string message) : Exception(message);