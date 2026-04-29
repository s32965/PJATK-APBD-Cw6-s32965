namespace WebApplication1.Exceptions;

public class AppointmentAlreadyCompletedException(string message) : Exception(message);