using LF.WebApi.Endpoints;

namespace LF.WebApiTests.Endpoints;

public class EnrollmentModelsValidatorTests
{
    [Theory]
    [InlineData(1, true)]
    [InlineData(0, false)]
    [InlineData(-3, false)]
    public void Enroll_CourseIdValidity(int courseId, bool expectedValid)
    {
        var result = new EnrollRequestValidator().Validate(new EnrollRequest(courseId));

        Assert.Equal(expectedValid, result.IsValid);
    }

    [Fact]
    public void SubmitQuiz_ValidRequest_Passes()
    {
        var request = new SubmitQuizAttemptRequest([new QuizAnswerRequest(1, [10, 11])]);

        var result = new SubmitQuizAttemptRequestValidator().Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void SubmitQuiz_NoAnswers_Fails()
    {
        var result = new SubmitQuizAttemptRequestValidator().Validate(new SubmitQuizAttemptRequest([]));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void SubmitQuiz_AnswerWithNonPositiveQuestionId_Fails()
    {
        var request = new SubmitQuizAttemptRequest([new QuizAnswerRequest(0, [10])]);

        var result = new SubmitQuizAttemptRequestValidator().Validate(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void SubmitQuiz_AnswerWithNoSelectedOptions_Fails()
    {
        var request = new SubmitQuizAttemptRequest([new QuizAnswerRequest(1, [])]);

        var result = new SubmitQuizAttemptRequestValidator().Validate(request);

        Assert.False(result.IsValid);
    }
}
