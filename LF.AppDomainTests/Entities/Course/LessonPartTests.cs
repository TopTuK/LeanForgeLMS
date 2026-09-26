using LF.AppDomain.Entities.Course;
using LF.AppDomain.Entities.Storage;
using LF.AppDomain.Models.Course.Enums;
using LF.AppDomain.Models.Storage.Enums;

namespace LF.AppDomainTests.Entities.CourseAggregate;

public class LessonPartTests
{
    private static Lesson CreateLesson()
    {
        var category = Category.Create("Backend");
        var course = Course.Create("Title", "Short intro", "Description", category, 1, DateTime.UtcNow);
        var chapter = course.AddChapter("Chapter 1");
        return chapter.AddLesson("Lesson 1", "Initial content", includeInPreview: false);
    }

    private static StorageObject CreateStorageObject(StorageObjectType type = StorageObjectType.Image) =>
        StorageObject.Create(type, "images/abc.png", "image/png", 1024, 1, DateTime.UtcNow);

    [Fact]
    public void ReplaceParts_TextPartWithEmptyHtml_Throws()
    {
        // Arrange
        var lesson = CreateLesson();
        var parts = new[] { new LessonPartInput(LessonPartType.Text, "  ", null) };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => lesson.ReplaceParts(parts));
    }

    [Fact]
    public void ReplaceParts_MediaPartWithoutStorageObject_Throws()
    {
        // Arrange
        var lesson = CreateLesson();
        var parts = new[] { new LessonPartInput(LessonPartType.Video, null, null) };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => lesson.ReplaceParts(parts));
    }

    [Fact]
    public void ReplaceParts_ImagePartWithoutStorageObjectOrFiles_Throws()
    {
        // Arrange
        var lesson = CreateLesson();
        var parts = new[] { new LessonPartInput(LessonPartType.Image, null, null, Files: []) };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => lesson.ReplaceParts(parts));
    }

    [Fact]
    public void ReplaceParts_ImagePartWithNonImageFile_Throws()
    {
        // Arrange
        var lesson = CreateLesson();
        var parts = new[]
        {
            new LessonPartInput(LessonPartType.Image, null, null,
                Files: [new LessonPartFileInput("notes.pdf", CreateStorageObject(StorageObjectType.File))]),
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => lesson.ReplaceParts(parts));
    }

    [Fact]
    public void ReplaceParts_ImagePartWithFiles_BuildsOrderedImageRow()
    {
        // Arrange
        var lesson = CreateLesson();
        var parts = new[]
        {
            new LessonPartInput(LessonPartType.Image, null, null,
                Files: [new LessonPartFileInput("a.png", CreateStorageObject()), new LessonPartFileInput("b.png", CreateStorageObject())]),
        };

        // Act
        lesson.ReplaceParts(parts);

        // Assert
        var image = lesson.Parts[0];
        Assert.Equal(LessonPartType.Image, image.PartType);
        Assert.Null(image.StorageObject);
        Assert.Equal(new[] { "a.png", "b.png" }, image.Files.Select(f => f.FileName));
        Assert.Equal(new[] { 1, 2 }, image.Files.Select(f => f.SortOrder));
    }

    [Fact]
    public void ReplaceParts_ValidMixedParts_AssignsSequentialSortOrder()
    {
        // Arrange
        var lesson = CreateLesson();
        var storageObject = CreateStorageObject();
        var parts = new[]
        {
            new LessonPartInput(LessonPartType.Text, "<p>Intro</p>", null),
            new LessonPartInput(LessonPartType.Image, null, storageObject),
        };

        // Act
        lesson.ReplaceParts(parts);

        // Assert
        Assert.Equal(2, lesson.Parts.Count);
        Assert.Equal(1, lesson.Parts[0].SortOrder);
        Assert.Equal(LessonPartType.Text, lesson.Parts[0].PartType);
        Assert.Equal(2, lesson.Parts[1].SortOrder);
        Assert.Equal(LessonPartType.Image, lesson.Parts[1].PartType);
        Assert.Same(storageObject, lesson.Parts[1].StorageObject);
    }

    [Fact]
    public void ReplaceParts_CalledTwice_ReplacesPreviousSet()
    {
        // Arrange
        var lesson = CreateLesson();
        lesson.ReplaceParts([new LessonPartInput(LessonPartType.Text, "<p>First</p>", null)]);

        // Act
        lesson.ReplaceParts([new LessonPartInput(LessonPartType.Text, "<p>Second</p>", null)]);

        // Assert
        Assert.Single(lesson.Parts);
        Assert.Equal("<p>Second</p>", lesson.Parts[0].Html);
    }

    private static QuizQuestionInput SingleChoiceQuestion(int correctCount = 1) => new(
        "Q1",
        QuestionType.SingleChoice,
        1,
        [
            new QuizOptionInput("A", correctCount >= 1, 1),
            new QuizOptionInput("B", correctCount >= 2, 2),
        ]);

    [Fact]
    public void ReplaceParts_QuizWithNoQuestions_Throws()
    {
        // Arrange
        var lesson = CreateLesson();
        var parts = new[] { new LessonPartInput(LessonPartType.Quiz, null, null, QuizQuestions: []) };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => lesson.ReplaceParts(parts));
    }

    [Fact]
    public void ReplaceParts_QuizQuestionWithOneOption_Throws()
    {
        // Arrange
        var lesson = CreateLesson();
        var question = new QuizQuestionInput("Q1", QuestionType.SingleChoice, 1, [new QuizOptionInput("A", true, 1)]);
        var parts = new[] { new LessonPartInput(LessonPartType.Quiz, null, null, [question]) };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => lesson.ReplaceParts(parts));
    }

    [Fact]
    public void ReplaceParts_SingleChoiceWithTwoCorrectOptions_Throws()
    {
        // Arrange
        var lesson = CreateLesson();
        var parts = new[] { new LessonPartInput(LessonPartType.Quiz, null, null, [SingleChoiceQuestion(correctCount: 2)]) };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => lesson.ReplaceParts(parts));
    }

    [Fact]
    public void ReplaceParts_SingleChoiceWithNoCorrectOptions_Throws()
    {
        // Arrange
        var lesson = CreateLesson();
        var parts = new[] { new LessonPartInput(LessonPartType.Quiz, null, null, [SingleChoiceQuestion(correctCount: 0)]) };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => lesson.ReplaceParts(parts));
    }

    [Fact]
    public void ReplaceParts_MultipleChoiceWithNoCorrectOptions_Throws()
    {
        // Arrange
        var lesson = CreateLesson();
        var question = new QuizQuestionInput("Q1", QuestionType.MultipleChoice, 1,
            [new QuizOptionInput("A", false, 1), new QuizOptionInput("B", false, 2)]);
        var parts = new[] { new LessonPartInput(LessonPartType.Quiz, null, null, [question]) };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => lesson.ReplaceParts(parts));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void ReplaceParts_QuizPassThresholdOutOfRange_Throws(int threshold)
    {
        // Arrange
        var lesson = CreateLesson();
        var parts = new[] { new LessonPartInput(LessonPartType.Quiz, null, null, [SingleChoiceQuestion()], threshold) };

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => lesson.ReplaceParts(parts));
    }

    [Fact]
    public void ReplaceParts_QuizWithoutExplicitThreshold_DefaultsTo60()
    {
        // Arrange
        var lesson = CreateLesson();
        var parts = new[] { new LessonPartInput(LessonPartType.Quiz, null, null, [SingleChoiceQuestion()]) };

        // Act
        lesson.ReplaceParts(parts);

        // Assert
        Assert.Equal(LessonPart.DefaultQuizPassThresholdPercent, lesson.Parts[0].QuizPassThresholdPercent);
    }

    [Fact]
    public void ReplaceParts_ValidQuiz_BuildsQuestionsAndOptionsInOrder()
    {
        // Arrange
        var lesson = CreateLesson();
        var multipleChoice = new QuizQuestionInput("Q2", QuestionType.MultipleChoice, 2,
            [new QuizOptionInput("A", true, 1), new QuizOptionInput("B", true, 2), new QuizOptionInput("C", false, 3)]);
        var parts = new[] { new LessonPartInput(LessonPartType.Quiz, null, null, [SingleChoiceQuestion(), multipleChoice], 80) };

        // Act
        lesson.ReplaceParts(parts);

        // Assert
        var quiz = lesson.Parts[0];
        Assert.Equal(LessonPartType.Quiz, quiz.PartType);
        Assert.Equal(80, quiz.QuizPassThresholdPercent);
        Assert.Equal(2, quiz.QuizQuestions.Count);
        Assert.Equal(QuestionType.SingleChoice, quiz.QuizQuestions[0].QuestionType);
        Assert.Equal(2, quiz.QuizQuestions[0].Options.Count);
        Assert.Equal(3, quiz.QuizQuestions[1].Options.Count);
    }

    [Fact]
    public void ReplaceParts_IdenticalContent_ReportsNoChange()
    {
        var lesson = CreateLesson();
        var storageObject = CreateStorageObject();
        lesson.ReplaceParts([new LessonPartInput(LessonPartType.Text, "<p>Intro</p>", null), new LessonPartInput(LessonPartType.Image, null, storageObject)]);

        var changed = lesson.ReplaceParts([new LessonPartInput(LessonPartType.Text, "<p>Intro</p>", null), new LessonPartInput(LessonPartType.Image, null, storageObject)]);

        Assert.False(changed);
    }

    [Fact]
    public void ReplaceParts_FirstParts_ReportsChange()
    {
        var lesson = CreateLesson();

        Assert.True(lesson.ReplaceParts([new LessonPartInput(LessonPartType.Text, "<p>Intro</p>", null)]));
    }

    [Fact]
    public void ReplaceParts_EditedHtml_ReportsChange()
    {
        var lesson = CreateLesson();
        lesson.ReplaceParts([new LessonPartInput(LessonPartType.Text, "<p>Intro</p>", null)]);

        Assert.True(lesson.ReplaceParts([new LessonPartInput(LessonPartType.Text, "<p>Intro, revised</p>", null)]));
    }

    [Fact]
    public void ReplaceParts_ReorderedParts_ReportsChange()
    {
        var lesson = CreateLesson();
        lesson.ReplaceParts([new LessonPartInput(LessonPartType.Text, "<p>A</p>", null), new LessonPartInput(LessonPartType.Text, "<p>B</p>", null)]);

        Assert.True(lesson.ReplaceParts([new LessonPartInput(LessonPartType.Text, "<p>B</p>", null), new LessonPartInput(LessonPartType.Text, "<p>A</p>", null)]));
    }

    [Fact]
    public void ReplaceParts_ChangedQuizAnswer_ReportsChange()
    {
        var lesson = CreateLesson();
        lesson.ReplaceParts([new LessonPartInput(LessonPartType.Quiz, null, null, [SingleChoiceQuestion()], 60)]);

        // Same question and option texts as SingleChoiceQuestion(); only the correct answer moves.
        var flipped = new QuizQuestionInput("Q1", QuestionType.SingleChoice, 1,
            [new QuizOptionInput("A", false, 1), new QuizOptionInput("B", true, 2)]);

        Assert.True(lesson.ReplaceParts([new LessonPartInput(LessonPartType.Quiz, null, null, [flipped], 60)]));
    }

    [Fact]
    public void RenameAndUpdateContent_ReportWhetherAnythingChanged()
    {
        var lesson = CreateLesson();

        Assert.False(lesson.Rename("  Lesson 1  "));
        Assert.True(lesson.Rename("Lesson One"));
        Assert.False(lesson.UpdateContent("Initial content"));
        Assert.True(lesson.UpdateContent("New content"));
        Assert.True(lesson.UpdateContent(null));
        Assert.False(lesson.UpdateContent(""));
    }
}
