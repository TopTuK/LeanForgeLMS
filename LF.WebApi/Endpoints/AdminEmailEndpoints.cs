using LF.Application.ModelDto.Email;
using LF.Application.Services.Email;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LF.WebApi.Endpoints;

// Lets an admin verify the SMTP setup end to end: the email is only queued here, and
// LF.NotificationService delivers it on its next scheduled run.
public sealed class AdminEmailEndpoints : IEndpointGroup
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/email").WithTags("AdminEmail").RequireAuthorization("AdminOnly");

        group.MapPost("/test", async Task<Results<Accepted<EmailStatusResponse>, ValidationProblem>>
            (SendTestEmailRequest request, IEmailQueue emailQueue, TimeProvider timeProvider, CancellationToken ct) =>
        {
            var validation = new SendTestEmailRequestValidator().Validate(request);
            if (!validation.IsValid) return TypedResults.ValidationProblem(validation.ToDictionary());

            var queuedAt = timeProvider.GetUtcNow();
            var status = await emailQueue.EnqueueAsync(new EnqueueEmailDto
            {
                ToAddress = request.To,
                Subject = "LeanForge test email",
                HtmlBody = $"<p>This is a test email from LeanForge, queued at {queuedAt:u}.</p><p>If you can read this, SMTP delivery works.</p>",
                TextBody = $"This is a test email from LeanForge, queued at {queuedAt:u}. If you can read this, SMTP delivery works.",
            }, ct);

            return TypedResults.Accepted($"/api/admin/email/{status.Id}", EmailStatusResponse.From(status));
        });

        group.MapGet("/{id:int}", async Task<Results<Ok<EmailStatusResponse>, NotFound>>
            (int id, IEmailQueue emailQueue, CancellationToken ct) =>
        {
            var status = await emailQueue.GetStatusAsync(id, ct);
            return status is null ? TypedResults.NotFound() : TypedResults.Ok(EmailStatusResponse.From(status));
        });
    }
}
