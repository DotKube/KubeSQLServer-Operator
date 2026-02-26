using k8s.Models;
using KubeOps.KubernetesClient;
using Microsoft.Extensions.Logging;
using Moq;
using OperatorTemplate.Operator.UnitTests.Helpers;
using SqlServerOperator.Configuration;
using SqlServerOperator.Controllers.V1Alpha1;
using SqlServerOperator.Controllers.Services;
using SqlServerOperator.Entities.V1Alpha1;
using Xunit;

namespace OperatorTemplate.Operator.UnitTests.Controllers;

public class DatabaseControllerTests
{
    private readonly Mock<ILogger<SQLServerDatabaseController>> _mockLogger;
    private readonly Mock<IKubernetesClient> _mockK8sClient;
    private readonly Mock<DefaultMssqlConfig> _mockConfig;
    private readonly Mock<ISqlServerEndpointService> _mockEndpointService;
    private readonly Mock<ISqlExecutor> _mockSqlExecutor;
    private readonly SQLServerDatabaseController _controller;

    public DatabaseControllerTests()
    {
        _mockLogger = new Mock<ILogger<SQLServerDatabaseController>>();
        _mockK8sClient = new Mock<IKubernetesClient>();
        _mockConfig = new Mock<DefaultMssqlConfig>();
        _mockEndpointService = new Mock<ISqlServerEndpointService>();
        _mockSqlExecutor = new Mock<ISqlExecutor>();

        _controller = new SQLServerDatabaseController(
            _mockLogger.Object,
            _mockK8sClient.Object,
            _mockConfig.Object,
            _mockEndpointService.Object,
            _mockSqlExecutor.Object);
    }

    [Fact]
    public async Task ReconcileAsync_WithExternalServer_CreatesDatabase()
    {
        // Arrange
        var entity = TestDataBuilder.CreateDatabase("test-db", "external-sql", "default");
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
        _mockK8sClient.Setup(x => x.UpdateStatusAsync(It.IsAny<V1Alpha1SQLServerDatabase>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((V1Alpha1SQLServerDatabase e, CancellationToken ct) => e);

        // Act
        var result = await _controller.ReconcileAsync(entity, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Ready", entity.Status?.State);
        Assert.Equal("Database ensured.", entity.Status?.Message);
        _mockSqlExecutor.Verify(x => x.ExecuteNonQueryAsync(
            It.IsAny<string>(),
            It.Is<string>(cmd => cmd.Contains("CREATE DATABASE")),
            It.Is<Dictionary<string, object>>(p => p.ContainsKey("@DatabaseName"))),
            Times.Once);
    }

    [Fact]
    public async Task ReconcileAsync_WhenInstanceNotFound_ReturnsFailure()
    {
        // Arrange
        var entity = TestDataBuilder.CreateDatabase("test-db", "missing-instance", "default");

        _mockK8sClient.Setup(x => x.GetAsync<V1Alpha1ExternalSQLServer>("missing-instance", "default"))
            .ReturnsAsync((V1Alpha1ExternalSQLServer?)null);
        _mockK8sClient.Setup(x => x.GetAsync<V1Alpha1SQLServer>("missing-instance", "default"))
            .ReturnsAsync((V1Alpha1SQLServer?)null);
        _mockK8sClient.Setup(x => x.UpdateStatusAsync(It.IsAny<V1Alpha1SQLServerDatabase>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((V1Alpha1SQLServerDatabase e, CancellationToken ct) => e);

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
}