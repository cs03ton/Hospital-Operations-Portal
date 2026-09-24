using Hop.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Hop.Api.Tests;

public sealed class RepairValidationMetadataTests
{
    [Theory]
    [MemberData(nameof(RepairModels))]
    public void PositionalRecordValidationMetadata_IsReadFromConstructorParameters(object model)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddControllers().AddApplicationPart(typeof(RepairsController).Assembly);
        using var provider = services.BuildServiceProvider();
        var httpContext = new DefaultHttpContext { RequestServices = provider };
        var actionContext = new ActionContext(httpContext, new(), new());

        var exception = Record.Exception(() =>
            provider.GetRequiredService<IObjectModelValidator>()
                .Validate(actionContext, validationState: null, prefix: string.Empty, model));

        Assert.Null(exception);
    }

    public static TheoryData<object> RepairModels => new()
    {
        new RepairInput(Guid.NewGuid(), "title", "description", "location", "contact"),
        new RepairAction(Guid.NewGuid(), "note"),
        new RepairCategoryInput(null, "category", "IT", true, null)
    };
}
