using LF.Application.Services.CourseAuthoring;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LF.WebApi.Endpoints;

public sealed class AdminCategoryEndpoints : IEndpointGroup
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/categories").WithTags("AdminCategories").RequireAuthorization("AdminOnly");

        group.MapGet("/", async Task<Ok<IReadOnlyList<CategoryResponse>>>
            (ICourseAuthoringService courseService, CancellationToken ct) =>
        {
            var categories = await courseService.ListCategoriesAsync();
            return TypedResults.Ok<IReadOnlyList<CategoryResponse>>([.. categories.Select(c => new CategoryResponse(c.Id, c.Name, c.IsDefault))]);
        });

        group.MapPost("/", async Task<Results<Created<CategoryResponse>, ValidationProblem>>
            (CreateCategoryRequest request, ICourseAuthoringService courseService, CancellationToken ct) =>
        {
            var validation = new CreateCategoryRequestValidator().Validate(request);
            if (!validation.IsValid) return TypedResults.ValidationProblem(validation.ToDictionary());

            try
            {
                var category = await courseService.CreateCategoryAsync(request.Name);
                var response = new CategoryResponse(category.Id, category.Name, category.IsDefault);
                return TypedResults.Created($"/api/admin/categories/{category.Id}", response);
            }
            catch (ArgumentException ex)
            {
                return TypedResults.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["name"] = [ex.Message],
                });
            }
        });

        group.MapDelete("/{id:int}", async Task<Results<NoContent, NotFound, Conflict<string>>>
            (int id, ICourseAuthoringService courseService, CancellationToken ct) =>
        {
            try
            {
                var deleted = await courseService.DeleteCategoryAsync(id);
                return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return TypedResults.Conflict(ex.Message);
            }
        });
    }
}
