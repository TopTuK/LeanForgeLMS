namespace LF.Application.Common.Exceptions;

public sealed class CourseUnavailableException(string message) : Exception(message);
