using k8s.Models;
using KubeOps.KubernetesClient;
using Microsoft.Extensions.Logging;
using Moq;
using OperatorTemplate.Operator.UnitTests.Helpers;
using SqlServerOperator.Controllers;
using SqlServerOperator.Controllers.Services;
using SqlServerOperator.Entities;
using Xunit;

namespace OperatorTemplate.Operator.UnitTests.Controllers;

public class SchemaControllerTests
{
    private readonly Mock<ILogger<SQLServerSchemaController>> _mockLogger;
    private readonly Mock<IKubernetesClient> _mockK8sClient;
    private readonly Mock<ISqlServerEndpointService> _mockEndpointService;
    private readonly Mock<ISqlExecutor> _mockSqlExecutor;
    private readonly SQLServerSchemaController _controller;

    public SchemaControllerTests()
    {
        _mockLogger = new Mock<ILogger<SQLServerSchemaController>>();
        _mockK8sClient = new Mock<IKubernetesClient>();
        _mockEndpointService = new Mock<ISqlServerEndpointService>();
        _mockSqlExecutor = new Mock<ISqlExecutor>();

        _controller = new SQLServerSchemaController(
            _mockLogger.Object,
            _mockK8sClient.Object,
            _mockEndpointService.Object,
            _mockSqlExecutor.Object);
    }

    [Fact]
    public async Task ReconcileAsync_WithExternalServer_CreatesSchema()
    {
        // Arrange
        var entity = TestDataBuilder.CreateSchema("test-schema", "external-sql", "default");
        var externalServer = TestDataBuilder.CreateExternalSqlServer("external-sql", "default");
        var secret = TestDataBuilder.CreateSecret("external-secret", "default", "TestPass123!");

        _mockK8sClient.Setup(x => x.GetAsync<V1Alpha1ExternalSQLServer>("external-sql", "default"))
            .ReturnsAsync(externalServer);
        _mockK8sClient.Setup(x => x.GetAsync<V1Secret>("external-secret", "default"))
            .ReturnsAsync(secret);
        _mockEndpointService.Setup(x => x.GetSqlServerEndpointAsync("external-sql", "default"))
            .ReturnsAsync("localhost,1433");
        _mockSqlExecutor.Setup(x => x.ExecuteNonQueryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
            .Returns(Task.CompletedTask);
        _mockK8sClient.Setup(x => x.UpdateStatusAsync(It.IsAny<V1Alpha1SQLServerSchema>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((V1Alpha1SQLServerSchema e, CancellationToken ct) => e);

        // Act
        var result = await _controller.ReconcileAsync(entity, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Ready", entity.Status?.State);
        Assert.Equal("Schema ensured.", entity.Status?.Message);
        _mockSqlExecutor.Verify(x => x.ExecuteNonQueryAsync(
            It.IsAny<string>(),
            It.Is<string>(cmd => cmd.Contains("CREATE SCHEMA")),
            It.Is<Dictionary<string, object>>(p => p.ContainsKey("@SchemaName") && p.ContainsKey("@SchemaOwner"))),
            Times.Once);
    }

    [Fact]
    public async Task ReconcileAsync_WithInternalServer_CreatesSchema()
    {
        // Arrange
        var entity = TestDataBuilder.CreateSchema("test-schema", "internal-sql", "default");
        var internalServer = TestDataBuilder.CreateSqlServer("internal-sql", "default");
        var secret = TestDataBuilder.CreateSecret("internal-sql-secret", "default", "TestPass123!");

        _mockK8sClient.Setup(x => x.GetAsync<V1Alpha1ExternalSQLServer>("internal-sql", "default"))
            .ReturnsAsync((V1Alpha1ExternalSQLServer?)null);
        _mockK8sClient.Setup(x => x.GetAsync<V1Alpha1SQLServer>("internal-sql", "default"))
            .ReturnsAsync(internalServer);
        _mockK8sClient.Setup(x => x.GetAsync<V1Secret>("internal-sql-secret", "default"))
            .ReturnsAsync(secret);
        _mockEndpointService.Setup(x => x.GetSqlServerEndpointAsync("internal-sql", "default"))
            .ReturnsAsync("internal-sql-headless.default.svc.cluster.local");
        _mockSqlExecutor.Setup(x => x.ExecuteNonQueryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
            .Returns(Task.CompletedTask);
        _mockK8sClient.Setup(x => x.UpdateStatusAsync(It.IsAny<V1Alpha1SQLServerSchema>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((V1Alpha1SQLServerSchema e, CancellationToken ct) => e);

        // Act
        var result = await _controller.ReconcileAsync(entity, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Ready", entity.Status?.State);
        _mockSqlExecutor.Verify(x => x.ExecuteNonQueryAsync(
            It.IsAny<string>(),
            It.Is<string>(cmd => cmd.Contains("CREATE SCHEMA")),
            It.IsAny<Dictionary<string, object>>()),
            Times.Once);
    }

    [Fact]
    public async Task ReconcileAsync_WhenServerNotFound_ReturnsFailure()
    {
        // Arrange
        var entity = TestDataBuilder.CreateSchema("test-schema", "missing-server", "default");

        _mockK8sClient.Setup(x => x.GetAsync<V1Alpha1ExternalSQLServer>("missing-server", "default"))
            .ReturnsAsync((V1Alpha1ExternalSQLServer?)null);
        _mockK8sClient.Setup(x => x.GetAsync<V1Alpha1SQLServer>("missing-server", "default"))
            .ReturnsAsync((V1Alpha1SQLServer?)null);
        _mockK8sClient.Setup(x => x.UpdateStatusAsync(It.IsAny<V1Alpha1SQLServerSchema>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((V1Alpha1SQLServerSchema e, CancellationToken ct) => e);

        // Act
        var result = await _controller.ReconcileAsync(entity, CancellationToken.None);

        // Assert
        Assert.Equal("Error", entity.Status?.State);
        Assert.Contains("not found", entity.Status?.Message);
        _mockSqlExecutor.Verify(x => x.ExecuteNonQueryAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Dictionary<string, object>>()),
            Times.Never);
    }

    [Fact]
    public async Task DeletedAsync_ReturnsSuccess()
    {
        // Arrange
        var entity = TestDataBuilder.CreateSchema();

        // Act
        var result = await _controller.DeletedAsync(entity, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
    }
}