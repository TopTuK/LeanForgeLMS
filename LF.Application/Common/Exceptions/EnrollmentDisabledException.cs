namespace LF.Application.Common.Exceptions;

// Thrown when the "lf.self_enrollment" Unleash flag is off. Raised in EnrollmentLearningService
// (LF.WebApi) before the gRPC hop. Extends InvalidOperationException so it rides the existing
// enrollment plumbing to HTTP 409 without new catch clauses in EnrollmentEndpoints/PaymentEndpoints.
public sealed class EnrollmentDisabledException(string message) : InvalidOperationException(message);
