namespace LF.Application.Common.Exceptions;

public sealed class QuizCompletionRequiredException(string message) : Exception(message);
