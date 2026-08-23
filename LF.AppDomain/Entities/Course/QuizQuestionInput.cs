using LF.AppDomain.Models.Course.Enums;

namespace LF.AppDomain.Entities.Course;

public sealed record QuizQuestionInput(string Text, QuestionType QuestionType, int SortOrder, IReadOnlyList<QuizOptionInput> Options);
