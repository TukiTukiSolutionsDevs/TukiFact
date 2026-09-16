using TukiFact.Contracts.V1;

namespace TukiFact.ArchitectureTests.Contracts;

/// <summary>
/// Fails to compile — not just to run — if <c>contracts/tukifact/v1/cpe.proto</c> stops generating
/// these types: a drift guard for the contract-first <c>TukiFact.Contracts.Grpc</c> project.
/// A hand edit of the .proto that removes or renames the service or
/// the shared error message becomes a build error here instead of a later runtime surprise.
/// </summary>
public sealed class GrpcContractDriftTests
{
    [Fact]
    public void CpeServiceBase_Exists_AndIsAnAbstractGrpcServiceBase()
    {
        // Act
        var serviceBaseType = typeof(CpeService.CpeServiceBase);

        // Assert
        serviceBaseType.IsAbstract.Should().BeTrue();
    }

    [Fact]
    public void ErrorDetail_Exists_AndCarriesTheDocumentedFields()
    {
        // Act
        var propertyNames = typeof(ErrorDetail).GetProperties().Select(property => property.Name);

        // Assert
        propertyNames.Should().Contain(["Code", "Type", "Description", "FieldErrors", "CorrelationId"]);
    }
}
