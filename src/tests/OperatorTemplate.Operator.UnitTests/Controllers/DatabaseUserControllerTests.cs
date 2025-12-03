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

public class DatabaseUserControllerTests
{
    private readonly Mock<ILogger<SQLServerUserController>> _mockLogger;
    private readonly Mock<IKubernetesClient> _mockK8sClient;
    private readonly Mock<ISqlServerEndpointService> _mockEndpointService;
    private readonly Mock<ISqlExecutor> _mockSqlExecutor;
    private readonly SQLServerUserController _controller;

    public DatabaseUserControllerTests()
    {
        _mockLogger = new Mock<ILogger<SQLServerUserController>>();
        _mockK8sClient = new Mock<IKubernetesClient>();
        _mockEndpointService = new Mock<ISqlServerEndpointService>();
        _mockSqlExecutor = new Mock<ISqlExecutor>();

        _controller = new SQLServerUserController(
            _mockLogger.Object,
            _mockK8sClient.Object,
            _mockEndpointService.Object,
            _mockSqlExecutor.Object);
    }

    [Fact]
    public async Task ReconcileAsync_WithExternalServer_CreatesUserAndAssignsRoles()
    {
        // Arrange
        var entity = TestDataBuilder.CreateDatabaseUser("test-user", "test-login", "test-db", "external-sql", "default");
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
        _mockK8sClient.Setup(x => x.UpdateStatusAsync(It.IsAny<V1Alpha1DatabaseUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((V1Alpha1DatabaseUser e, CancellationToken ct) => e);

        // Act
        var result = await _controller.ReconcileAsync(entity, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Ready", entity.Status?.State);
        Assert.Equal("Database user ensured.", entity.Status?.Message);

        // Verify user creation
        _mockSqlExecutor.Verify(x => x.ExecuteNonQueryAsync(
            It.IsAny<string>(),
            It.Is<string>(cmd => cmd.Contains("CREATE USER")),
            It.Is<Dictionary<string, object>>(p => p.ContainsKey("@LoginName"))),
            Times.Once);

        // Verify role assignment (should be called once per role)
        _mockSqlExecutor.Verify(x => x.ExecuteNonQueryAsync(
            It.IsAny<string>(),
            It.Is<string>(cmd => cmd.Contains("sp_addrolemember")),
            It.Is<Dictionary<string, object>>(p => p.ContainsKey("@rolename") && p.ContainsKey("@membername"))),
            Times.Exactly(entity.Spec.Roles.Count));
    }

    [Fact]
    public async Task ReconcileAsync_WithInternalServer_CreatesUser()
    {
        // Arrange
        var entity = TestDataBuilder.CreateDatabaseUser("test-user", "test-login", "test-db", "internal-sql", "default");
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
        _mockK8sClient.Setup(x => x.UpdateStatusAsync(It.IsAny<V1Alpha1DatabaseUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((V1Alpha1DatabaseUser e, CancellationToken ct) => e);

        // Act
        var result = await _controller.ReconcileAsync(entity, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Ready", entity.Status?.State);
        _mockSqlExecutor.Verify(x => x.ExecuteNonQueryAsync(
            It.IsAny<string>(),
            It.Is<string>(cmd => cmd.Contains("CREATE USER")),
            It.IsAny<Dictionary<string, object>>()),
            Times.Once);
    }

    [Fact]
    public async Task ReconcileAsync_WhenServerNotFound_ReturnsFailure()
    {
        // Arrange
        var entity = TestDataBuilder.CreateDatabaseUser("test-user", "test-login", "test-db", "missing-server", "default");

        _mockK8sClient.Setup(x => x.GetAsync<V1Alpha1ExternalSQLServer>("missing-server", "default"))
            .ReturnsAsync((V1Alpha1ExternalSQLServer?)null);
        _mockK8sClient.Setup(x => x.GetAsync<V1Alpha1SQLServer>("missing-server", "default"))
            .ReturnsAsync((V1Alpha1SQLServer?)null);
        _mockK8sClient.Setup(x => x.UpdateStatusAsync(It.IsAny<V1Alpha1DatabaseUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((V1Alpha1DatabaseUser e, CancellationToken ct) => e);

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
        var entity = TestDataBuilder.CreateDatabaseUser();

        // Act
        var result = await _controller.DeletedAsync(entity, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
    }
}