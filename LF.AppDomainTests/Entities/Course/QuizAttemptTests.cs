using LF.AppDomain.Entities.Course;
using LF.AppDomain.Models.Course.Enums;
using LF.AppDomainTests.TestSupport;

namespace LF.AppDomainTests.Entities.CourseAggregate;

public class QuizAttemptTests
{
    private static LessonPart CreateGradedQuizPart(int? passThresholdPercent, out int q1CorrectOptionId, out int q2OptionAId, out int q2OptionBId)
    {
        var category = Category.Create("Backend");
        var course = Course.Create("Title", "Short intro", "Description", category, 1, DateTime.UtcNow);
        var chapter = course.AddChapter("Chapter 1");
        var lesson = chapter.AddLesson("Lesson 1", "Initial content", includeInPreview: false);

        var q1 = new QuizQuestionInput("Q1", QuestionType.SingleChoice, 1,
            [new QuizOptionInput("Correct", true, 1), new QuizOptionInput("Wrong", false, 2)]);
        var q2 = new QuizQuestionInput("Q2", QuestionType.MultipleChoice, 2,
            [new QuizOptionInput("A", true, 1), new QuizOptionInput("B", true, 2), new QuizOptionInput("C", false, 3)]);

        lesson.ReplaceParts([new LessonPartInput(LessonPartType.Quiz, null, null, [q1, q2], passThresholdPercent)]);
        var quizPart = lesson.Parts[0];

        EntityIdSetter.SetId(quizPart, 1);
        var index = 100;
        foreach (var question in quizPart.QuizQuestions)
        {
            EntityIdSetter.SetId(question, index++);
            foreach (var option in question.Options)
                EntityIdSetter.SetId(option, index++);
        }

        q1CorrectOptionId = quizPart.QuizQuestions[0].Options.First(o => o.IsCorrect).Id;
        q2OptionAId = quizPart.QuizQuestions[1].Options[0].Id;
        q2OptionBId = quizPart.QuizQuestions[1].Options[1].Id;

        return quizPart;
    }

    [Fact]
    public void Grade_AllQuestionsAnsweredCorrectly_ScoresOneHundredAndPasses()
    {
        // Arrange
        var quizPart = CreateGradedQuizPart(60, out var q1Correct, out var q2A, out var q2B);
        var answers = new Dictionary<int, IReadOnlyList<int>>
        {
            [quizPart.QuizQuestions[0].Id] = [q1Correct],
            [quizPart.QuizQuestions[1].Id] = [q2A, q2B],
        };

        // Act
        var attempt = QuizAttempt.Grade(quizPart, answers, enrollmentId: 1, lessonId: 1, DateTimeOffset.UtcNow);

        // Assert
        Assert.Equal(100, attempt.ScorePercent);
        Assert.True(attempt.Passed);
    }

    [Fact]
    public void Grade_MultipleChoicePartiallySelected_QuestionCountsAsIncorrect()
    {
        // Arrange — selecting only one of the two correct options for Q2 doesn't match the correct set exactly
        var quizPart = CreateGradedQuizPart(60, out var q1Correct, out var q2A, out _);
        var answers = new Dictionary<int, IReadOnlyList<int>>
        {
            [quizPart.QuizQuestions[0].Id] = [q1Correct],
            [quizPart.QuizQuestions[1].Id] = [q2A],
        };

        // Act
        var attempt = QuizAttempt.Grade(quizPart, answers, enrollmentId: 1, lessonId: 1, DateTimeOffset.UtcNow);

        // Assert
        Assert.Equal(50, attempt.ScorePercent);
    }

    [Fact]
    public void Grade_ScoreBelowThreshold_DoesNotPass()
    {
        // Arrange
        var quizPart = CreateGradedQuizPart(80, out var q1Correct, out var q2A, out _);
        var answers = new Dictionary<int, IReadOnlyList<int>>
        {
            [quizPart.QuizQuestions[0].Id] = [q1Correct],
            [quizPart.QuizQuestions[1].Id] = [q2A],
        };

        // Act
        var attempt = QuizAttempt.Grade(quizPart, answers, enrollmentId: 1, lessonId: 1, DateTimeOffset.UtcNow);

        // Assert
        Assert.Equal(50, attempt.ScorePercent);
        Assert.False(attempt.Passed);
    }

    [Fact]
    public void Grade_UnansweredQuestion_CountsAsIncorrect()
    {
        // Arrange
        var quizPart = CreateGradedQuizPart(60, out var q1Correct, out _, out _);
        var answers = new Dictionary<int, IReadOnlyList<int>>
        {
            [quizPart.QuizQuestions[0].Id] = [q1Correct],
        };

        // Act
        var attempt = QuizAttempt.Grade(quizPart, answers, enrollmentId: 1, lessonId: 1, DateTimeOffset.UtcNow);

        // Assert
        Assert.Equal(50, attempt.ScorePercent);
        Assert.Equal(2, attempt.Answers.Count);
        Assert.Empty(attempt.Answers.Single(a => a.QuestionId == quizPart.QuizQuestions[1].Id).SelectedOptionIds);
    }

    [Fact]
    public void Grade_NonQuizPart_Throws()
    {
        // Arrange
        var category = Category.Create("Backend");
        var course = Course.Create("Title", "Short intro", "Description", category, 1, DateTime.UtcNow);
        var chapter = course.AddChapter("Chapter 1");
        var lesson = chapter.AddLesson("Lesson 1", "Initial content", includeInPreview: false);
        lesson.ReplaceParts([new LessonPartInput(LessonPartType.Text, "<p>Hi</p>", null)]);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            QuizAttempt.Grade(lesson.Parts[0], new Dictionary<int, IReadOnlyList<int>>(), enrollmentId: 1, lessonId: 1, DateTimeOffset.UtcNow));
    }
}
