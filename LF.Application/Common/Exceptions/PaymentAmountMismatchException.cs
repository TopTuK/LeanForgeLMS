namespace LF.Application.Common.Exceptions;

public sealed class PaymentAmountMismatchException(string message) : Exception(message);
