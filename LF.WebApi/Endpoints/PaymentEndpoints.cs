using System.Security.Claims;
using LF.AppDomain.Models.Course.Enums;
using LF.Application.Common.Exceptions;
using LF.Application.ModelDto.Enrollment;
using LF.Application.ModelDto.Payment;
using LF.Application.Services.EnrollmentLearning;
using LF.Application.Services.Payment;
using LF.WebApi.Common;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LF.WebApi.Endpoints;

public sealed class PaymentEndpoints : IEndpointGroup
{
    public void Map(IEndpointRouteBuilder app)
    {
        // No blanket RequireAuthorization on the group — the Robokassa ResultURL webhook is anonymous
        // and authenticated by signature instead.
        var group = app.MapGroup("/api/payments").WithTags("Payments");

        group.MapPost("/checkout", async Task<Results<Ok<CheckoutResponse>, UnauthorizedHttpResult, ValidationProblem, Conflict<string>, ForbidHttpResult>>
            (CheckoutRequest request, ClaimsPrincipal user, IEnrollmentLearningService enrollmentService, IGrpcPaymentService paymentService, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();

            var validation = new CheckoutRequestValidator().Validate(request);
            if (!validation.IsValid) return TypedResults.ValidationProblem(validation.ToDictionary());

            EnrollmentDetailDto enrollment;
            try
            {
                enrollment = await enrollmentService.EnrollAsync(request.CourseId, userId.Value, request.PromoCode);
            }
            catch (SelfEnrollmentException)
            {
                return TypedResults.Forbid();
            }
            catch (EnrollmentModeException)
            {
                return TypedResults.Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return TypedResults.Conflict(ex.Message);
            }

            if (enrollment.Status != EnrollmentStatus.PendingPayment)
                return TypedResults.Ok(new CheckoutResponse(enrollment.Id, null, null, enrollment.Status.ToString()));

            var order = await paymentService.CreatePaymentOrderAsync(new CreatePaymentOrderDto
            {
                EnrollmentId = enrollment.Id,
                UserId = userId.Value,
                Amount = enrollment.PricePaid,
                Description = enrollment.CourseTitle,
            });

            return TypedResults.Ok(new CheckoutResponse(enrollment.Id, order.Id, order.PaymentUrl, enrollment.Status.ToString()));
        }).RequireAuthorization();

        group.MapGet("/orders/{orderId:int}", async Task<Results<Ok<PaymentOrderResponse>, UnauthorizedHttpResult, NotFound>>
            (int orderId, ClaimsPrincipal user, IGrpcPaymentService paymentService, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();

            var order = await paymentService.GetPaymentOrderAsync(orderId, userId.Value);
            return order is null
                ? TypedResults.NotFound()
                : TypedResults.Ok(new PaymentOrderResponse(
                    order.Id, order.EnrollmentId, order.Amount, order.Status.ToString(), order.CreatedAt, order.PaidAt));
        }).RequireAuthorization();

        // Robokassa server-to-server webhook. Anonymous, signature-verified, idempotent. Must reply
        // with the plain-text body "OK{InvId}" or Robokassa keeps retrying.
        group.MapMethods("/robokassa/result", ["GET", "POST"], async Task<ContentHttpResult>
            (HttpContext http, IGrpcPaymentService paymentService, IEnrollmentLearningService enrollmentService) =>
        {
            var values = await ReadCallbackValuesAsync(http);

            if (!int.TryParse(values.GetValueOrDefault("InvId"), out var invId))
                return TypedResults.Text("bad sign");

            var callback = new PaymentCallbackDto
            {
                OutSum = values.GetValueOrDefault("OutSum") ?? string.Empty,
                InvId = invId,
                SignatureValue = values.GetValueOrDefault("SignatureValue") ?? string.Empty,
            };

            PaymentConfirmationDto confirmation;
            try
            {
                confirmation = await paymentService.ConfirmPaymentAsync(callback);
            }
            catch (PaymentSignatureException)
            {
                return TypedResults.Text("bad sign");
            }
            catch (PaymentOrderNotFoundException)
            {
                return TypedResults.Text("bad sign");
            }
            catch (PaymentAmountMismatchException)
            {
                return TypedResults.Text("bad sign");
            }

            try
            {
                await enrollmentService.ActivatePaidEnrollmentAsync(confirmation.EnrollmentId, confirmation.AmountPaid);
            }
            catch (InvalidOperationException)
            {
                // Order is settled but activation failed — reply non-OK so Robokassa retries; both
                // ConfirmPaymentAsync and ActivatePaidEnrollmentAsync are idempotent.
                return TypedResults.Text("retry", "text/plain", statusCode: StatusCodes.Status500InternalServerError);
            }

            return TypedResults.Text($"OK{invId}");
        }).DisableAntiforgery();
    }

    private static async Task<Dictionary<string, string?>> ReadCallbackValuesAsync(HttpContext http)
    {
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        foreach (var (key, value) in http.Request.Query)
            values[key] = value.ToString();

        if (http.Request.HasFormContentType)
        {
            var form = await http.Request.ReadFormAsync();
            foreach (var (key, value) in form)
                values[key] = value.ToString();
        }

        return values;
    }
}
