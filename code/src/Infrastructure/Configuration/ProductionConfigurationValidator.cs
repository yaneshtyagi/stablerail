using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace StableRail.Infrastructure.Configuration;

public sealed class ProductionConfigurationValidator(IHostEnvironment environment, IConfiguration configuration)
    : IValidateOptions<StableRailOptions>
{
    public ValidateOptionsResult Validate(string? name, StableRailOptions options)
    {
        var problems = ProductionRules.Evaluate(environment, configuration)
            .Select(problem => "Production startup rejected configuration: " + problem)
            .ToArray();
        return problems.Length == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(problems);
    }
}
