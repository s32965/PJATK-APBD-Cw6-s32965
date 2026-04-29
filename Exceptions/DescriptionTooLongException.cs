namespace WebApplication1.Exceptions;

public class DescriptionTooLongException(string message) : Exception(message);