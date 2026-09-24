using NetArchTest.Rules;

namespace Recam.Server.Tests.Architecture;

public sealed class LayerDependencyTests
{
    private const string DomainNamespace = "Recam.Server.Domain";
    private const string InfrastructureNamespace = "Recam.Server.Infrastructure";
    private const string FeaturesNamespace = "Recam.Server.Features";

    private static readonly Types ServerTypes = Types.InAssembly(typeof(Program).Assembly);

    [Fact(DisplayName = "Domain does not depend on infrastructure or features")]
    public void Domain_WhenInspected_DoesNotDependOnOuterLayers()
    {
        // arrange
        var domainTypes = ServerTypes.That().ResideInNamespace(DomainNamespace);

        // act
        var result = domainTypes.ShouldNot()
            .HaveDependencyOnAny(InfrastructureNamespace, FeaturesNamespace)
            .GetResult();

        // assert
        Assert.True(result.IsSuccessful, FailureMessage(result.FailingTypeNames));
    }

    [Fact(DisplayName = "Infrastructure does not depend on features")]
    public void Infrastructure_WhenInspected_DoesNotDependOnFeatures()
    {
        // arrange
        var infrastructureTypes = ServerTypes.That().ResideInNamespace(InfrastructureNamespace);

        // act
        var result = infrastructureTypes.ShouldNot()
            .HaveDependencyOn(FeaturesNamespace)
            .GetResult();

        // assert
        Assert.True(result.IsSuccessful, FailureMessage(result.FailingTypeNames));
    }

    [Fact(DisplayName = "A feature does not depend on another feature")]
    public void Features_WhenInspected_DoNotDependOnEachOther()
    {
        // arrange
        var features = FeatureNamespaces();

        // act
        var violations = features
            .Select(feature => ServerTypes.That().ResideInNamespace(feature)
                .ShouldNot()
                .HaveDependencyOnAny([.. features.Where(other => other != feature)])
                .GetResult())
            .Where(result => !result.IsSuccessful)
            .Select(result => FailureMessage(result.FailingTypeNames))
            .ToList();

        // assert
        Assert.Empty(violations);
    }

    private static List<string> FeatureNamespaces() =>
        [.. typeof(Program).Assembly.GetTypes()
            .Select(type => type.Namespace)
            .OfType<string>()
            .Where(ns => ns.StartsWith(FeaturesNamespace + ".", StringComparison.Ordinal))
            .Select(ns => string.Join('.', ns.Split('.').Take(4)))
            .Distinct()];

    private static string FailureMessage(IEnumerable<string>? failingTypeNames) =>
        "Violating types: " + string.Join(", ", failingTypeNames ?? []);
}
