using LF.AppDomain.Models.Course.Enums;
using LF.WebApi.Endpoints;

namespace LF.WebApiTests.Endpoints;

public class ReplaceLessonPartsValidatorTests
{
    private static ReplaceLessonPartsRequest Wrap(params LessonPartRequest[] parts) => new(parts);

    private static readonly ReplaceLessonPartsRequestValidator Validator = new();

    [Fact]
    public void EmptyParts_Passes()
    {
        // The validator only constrains parts that are present; clearing all parts is allowed.
        var result = Validator.Validate(Wrap());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void UnknownPartType_Fails()
    {
        var result = Validator.Validate(Wrap(new LessonPartRequest("Hologram", Html: "x", StorageObjectId: null)));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void TextPart_WithHtml_Passes()
    {
        var result = Validator.Validate(Wrap(new LessonPartRequest(nameof(LessonPartType.Text), Html: "<p>hi</p>", StorageObjectId: null)));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void TextPart_WithoutHtml_Fails()
    {
        var result = Validator.Validate(Wrap(new LessonPartRequest(nameof(LessonPartType.Text), Html: "", StorageObjectId: null)));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void MediaPart_WithNonPositiveStorageObjectId_Fails()
    {
        var result = Validator.Validate(Wrap(new LessonPartRequest(nameof(LessonPartType.Image), Html: null, StorageObjectId: 0)));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void MediaPart_WithStorageObjectId_Passes()
    {
        var result = Validator.Validate(Wrap(new LessonPartRequest(nameof(LessonPartType.Video), Html: null, StorageObjectId: 7)));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void FilesPart_WithoutFiles_Fails()
    {
        var result = Validator.Validate(Wrap(new LessonPartRequest(nameof(LessonPartType.Files), Html: null, StorageObjectId: null, Files: [])));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void FilesPart_WithFiles_Passes()
    {
        var part = new LessonPartRequest(nameof(LessonPartType.Files), Html: null, StorageObjectId: null,
            Files: [new LessonPartFileRequest("notes.pdf", 3)]);

        var result = Validator.Validate(Wrap(part));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void QuizPart_WithoutQuestions_Fails()
    {
        var result = Validator.Validate(Wrap(new LessonPartRequest(nameof(LessonPartType.Quiz), Html: null, StorageObjectId: null, QuizQuestions: [])));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void QuizPart_SingleChoiceWithExactlyOneCorrect_Passes()
    {
        var question = new QuizQuestionRequest("2 + 2?", nameof(QuestionType.SingleChoice), 0,
        [
            new QuizOptionRequest("3", false, 0),
            new QuizOptionRequest("4", true, 1),
        ]);
        var part = new LessonPartRequest(nameof(LessonPartType.Quiz), null, null, QuizQuestions: [question], QuizPassThresholdPercent: 80);

        var result = Validator.Validate(Wrap(part));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void QuizPart_SingleChoiceWithTwoCorrect_Fails()
    {
        var question = new QuizQuestionRequest("2 + 2?", nameof(QuestionType.SingleChoice), 0,
        [
            new QuizOptionRequest("4", true, 0),
            new QuizOptionRequest("four", true, 1),
        ]);
        var part = new LessonPartRequest(nameof(LessonPartType.Quiz), null, null, QuizQuestions: [question]);

        var result = Validator.Validate(Wrap(part));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void QuizPart_MultipleChoiceWithMultipleCorrect_Passes()
    {
        var question = new QuizQuestionRequest("Primes?", nameof(QuestionType.MultipleChoice), 0,
        [
            new QuizOptionRequest("2", true, 0),
            new QuizOptionRequest("3", true, 1),
            new QuizOptionRequest("4", false, 2),
        ]);
        var part = new LessonPartRequest(nameof(LessonPartType.Quiz), null, null, QuizQuestions: [question]);

        var result = Validator.Validate(Wrap(part));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void QuizPart_QuestionWithFewerThanTwoOptions_Fails()
    {
        var question = new QuizQuestionRequest("One?", nameof(QuestionType.SingleChoice), 0,
        [
            new QuizOptionRequest("only", true, 0),
        ]);
        var part = new LessonPartRequest(nameof(LessonPartType.Quiz), null, null, QuizQuestions: [question]);

        var result = Validator.Validate(Wrap(part));

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void QuizPart_ThresholdOutOfRange_Fails(int threshold)
    {
        var question = new QuizQuestionRequest("2 + 2?", nameof(QuestionType.SingleChoice), 0,
        [
            new QuizOptionRequest("3", false, 0),
            new QuizOptionRequest("4", true, 1),
        ]);
        var part = new LessonPartRequest(nameof(LessonPartType.Quiz), null, null, QuizQuestions: [question], QuizPassThresholdPercent: threshold);

        var result = Validator.Validate(Wrap(part));

        Assert.False(result.IsValid);
    }
}
