namespace LF.Application.ModelDto.Qna;

// Which inbox the caller is asking for. A user can legitimately be both a student on one course and
// teaching staff on another, so the two lists are separate rather than merged.
public enum LessonQuestionScope
{
    AsStudent = 1,
    AsStaff = 2,
}
