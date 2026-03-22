using KubeOps.KubernetesClient;
using Moq;
using SqlServerOperator.Controllers.Services;
using SqlServerOperator.Entities.V1Alpha1;
using Xunit;

namespace SqlServerOperator.UnitTests.Services;

public class DatabaseReferenceResolverTests
{
    private readonly Mock<IKubernetesClient> _kubernetesClientMock = new();
    private readonly DatabaseReferenceResolver _resolver;
    private const string Namespace = "test-ns";

    public DatabaseReferenceResolverTests()
    {
        _resolver = new DatabaseReferenceResolver(_kubernetesClientMock.Object);
    }

    [Fact]
    public async Task ResolveAsync_WithDatabaseRef_ResolvesFromSQLServerDatabase()
    {
        // Arrange
        var db = new V1Alpha1SQLServerDatabase
        {
            Spec = new V1Alpha1SQLServerDatabase.V1Alpha1SQLServerDatabaseSpec
            {
                InstanceName = "my-instance",
                DatabaseName = "my-db"
            },
            Status = new V1Alpha1SQLServerDatabase.V1Alpha1SQLServerDatabaseStatus
            {
                State = "Ready"
            }
        };

        _kubernetesClientMock.Setup(x => x.GetAsync<V1Alpha1SQLServerDatabase>("my-db-ref", Namespace, It.IsAny<CancellationToken>()))
            .ReturnsAsync(db);

        // Act
        var result = await _resolver.ResolveAsync("my-db-ref", null, null, Namespace);

        // Assert
        Assert.Equal("my-instance", result.InstanceName);
        Assert.Equal("my-db", result.DatabaseName);
    }

    [Fact]
    public async Task ResolveAsync_WithDatabaseRef_ResolvesFromExternalDatabase()
    {
        // Arrange
        var externalDb = new V1Alpha1ExternalDatabase
        {
            Spec = new V1Alpha1ExternalDatabase.V1Alpha1ExternalDatabaseSpec
            {
                InstanceName = "ext-instance",
                DatabaseName = "ext-db"
            },
            Status = new V1Alpha1ExternalDatabase.V1Alpha1ExternalDatabaseStatus
            {
                State = "Ready"
            }
        };

        _kubernetesClientMock.Setup(x => x.GetAsync<V1Alpha1SQLServerDatabase>("ext-db-ref", Namespace, It.IsAny<CancellationToken>()))
            .ReturnsAsync((V1Alpha1SQLServerDatabase?)null);
        _kubernetesClientMock.Setup(x => x.GetAsync<V1Alpha1ExternalDatabase>("ext-db-ref", Namespace, It.IsAny<CancellationToken>()))
            .ReturnsAsync(externalDb);

        // Act
        var result = await _resolver.ResolveAsync("ext-db-ref", null, null, Namespace);

        // Assert
        Assert.Equal("ext-instance", result.InstanceName);
        Assert.Equal("ext-db", result.DatabaseName);
    }

    [Fact]
    public async Task ResolveAsync_NoDatabaseRef_UsesDirectValues()
    {
        // Act
        var result = await _resolver.ResolveAsync(null, "direct-instance", "direct-db", Namespace);

        // Assert
        Assert.Equal("direct-instance", result.InstanceName);
        Assert.Equal("direct-db", result.DatabaseName);
    }

    [Fact]
    public async Task ResolveAsync_WithDatabaseRef_ThrowsIfResourceNotReady()
    {
        // Arrange
        var db = new V1Alpha1SQLServerDatabase
        {
            Spec = new V1Alpha1SQLServerDatabase.V1Alpha1SQLServerDatabaseSpec
            {
                InstanceName = "my-instance",
                DatabaseName = "my-db"
            },
            Status = new V1Alpha1SQLServerDatabase.V1Alpha1SQLServerDatabaseStatus
            {
                State = "Pending"
            }
        };

        _kubernetesClientMock.Setup(x => x.GetAsync<V1Alpha1SQLServerDatabase>("my-db-ref", Namespace, It.IsAny<CancellationToken>()))
            .ReturnsAsync(db);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => _resolver.ResolveAsync("my-db-ref", null, null, Namespace));
        Assert.Contains("Ready", exception.Message);
    }
}