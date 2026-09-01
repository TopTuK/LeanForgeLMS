namespace LF.Application.Common.Exceptions;

public sealed class PaymentOrderNotFoundException(string message) : Exception(message);
