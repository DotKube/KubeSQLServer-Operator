using KubeOps.KubernetesClient;
using Moq;
using SqlServerOperator.Controllers.Services;
using SqlServerOperator.Entities.V1Alpha1;
using Xunit;

namespace SqlServerOperator.UnitTests.Services;

public class DatabaseReferenceResolverTests
{
    private readonly Mock<IKubernetesClient> _kubernetesClientMock = new();
    private readonly Mock<ISqlServerEndpointService> _endpointServiceMock = new();
    private readonly DatabaseReferenceResolver _resolver;
    private const string Namespace = "test-ns";

    public DatabaseReferenceResolverTests()
    {
        _resolver = new DatabaseReferenceResolver(_kubernetesClientMock.Object, _endpointServiceMock.Object);
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

        var sqlServer = new V1Alpha1SQLServer
        {
            Metadata = new k8s.Models.V1ObjectMeta { Name = "my-instance" },
            Spec = new V1Alpha1SQLServer.V1Alpha1SQLServerSpec { SecretName = "my-secret" }
        };

        _kubernetesClientMock.Setup(x => x.GetAsync<V1Alpha1SQLServerDatabase>("my-db-ref", Namespace, It.IsAny<CancellationToken>()))
            .ReturnsAsync(db);
        _kubernetesClientMock.Setup(x => x.GetAsync<V1Alpha1SQLServer>("my-instance", Namespace, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sqlServer);
        _endpointServiceMock.Setup(x => x.GetSqlServerEndpointAsync("my-instance", Namespace))
            .ReturnsAsync("my-instance.svc:1433");

        // Act
        var result = await _resolver.ResolveAsync("my-db-ref", null, null, Namespace);

        // Assert
        Assert.Equal("my-instance.svc:1433", result.Host);
        Assert.Equal("my-db", result.DatabaseName);
        Assert.Equal("my-secret", result.SecretName);
    }

    [Fact]
    public async Task ResolveAsync_WithDatabaseRef_ResolvesFromExternalDatabase()
    {
        // Arrange
        var externalDb = new V1Alpha1ExternalDatabase
        {
            Spec = new V1Alpha1ExternalDatabase.V1Alpha1ExternalDatabaseSpec
            {
                ServerUrl = "ext-server.com",
                DatabaseName = "ext-db",
                SecretName = "ext-secret"
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
        Assert.Equal("ext-server.com", result.Host);
        Assert.Equal("ext-db", result.DatabaseName);
        Assert.Equal("ext-secret", result.SecretName);
    }

    [Fact]
    public async Task ResolveAsync_NoDatabaseRef_UsesDirectValues()
    {
        // Arrange
        var externalServer = new V1Alpha1ExternalSQLServer
        {
            Spec = new V1Alpha1ExternalSQLServer.V1Alpha1ExternalSQLServerSpec { SecretName = "direct-secret" }
        };

        _kubernetesClientMock.Setup(x => x.GetAsync<V1Alpha1ExternalSQLServer>("direct-instance", Namespace, It.IsAny<CancellationToken>()))
            .ReturnsAsync(externalServer);
        _endpointServiceMock.Setup(x => x.GetSqlServerEndpointAsync("direct-instance", Namespace))
            .ReturnsAsync("direct-host:1433");

        // Act
        var result = await _resolver.ResolveAsync(null, "direct-instance", "direct-db", Namespace);

        // Assert
        Assert.Equal("direct-host:1433", result.Host);
        Assert.Equal("direct-db", result.DatabaseName);
        Assert.Equal("direct-secret", result.SecretName);
    }
}