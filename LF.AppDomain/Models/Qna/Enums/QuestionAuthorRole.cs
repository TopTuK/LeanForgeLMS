namespace LF.AppDomain.Models.Qna.Enums;

// The capacity the author wrote in, captured at write time. It is deliberately not derived from the
// author's current UserRole: a thread must still read correctly after someone is promoted, demoted,
// or unassigned from the course.
public enum QuestionAuthorRole
{
    Student = 1,
    Instructor = 2,
    Admin = 3,
}
