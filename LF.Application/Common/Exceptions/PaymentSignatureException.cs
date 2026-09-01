namespace LF.Application.Common.Exceptions;

public sealed class PaymentSignatureException(string message) : Exception(message);
