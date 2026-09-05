namespace LF.Application.Common.Exceptions;

// Thrown when self-enrollment is globally switched off by an admin (PlatformSettings.StudentEnrollmentEnabled).
// Extends InvalidOperationException so it rides the existing enrollment plumbing
// (RpcCourseService -> FailedPrecondition -> GrpcEnrollmentService -> InvalidOperationException -> HTTP 409)
// without new catch clauses; only the message survives the gRPC boundary anyway.
public sealed class EnrollmentDisabledException(string message) : InvalidOperationException(message);
