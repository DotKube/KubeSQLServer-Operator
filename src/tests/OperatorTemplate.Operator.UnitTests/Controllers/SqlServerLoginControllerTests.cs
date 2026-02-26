using k8s.Models;
using KubeOps.KubernetesClient;
using Microsoft.Extensions.Logging;
using Moq;
using OperatorTemplate.Operator.UnitTests.Helpers;
using SqlServerOperator.Controllers.Services;
using SqlServerOperator.Controllers.V1Alpha1;
using SqlServerOperator.Entities.V1Alpha1;
using Xunit;

namespace OperatorTemplate.Operator.UnitTests.Controllers;

public class SqlServerLoginControllerTests
{
    private readonly Mock<ILogger<SQLServerLoginController>> _mockLogger;
    private readonly Mock<IKubernetesClient> _mockK8sClient;
    private readonly Mock<ISqlServerEndpointService> _mockEndpointService;
    private readonly Mock<ISqlExecutor> _mockSqlExecutor;
    private readonly SQLServerLoginController _controller;

    public SqlServerLoginControllerTests()
    {
        _mockLogger = new Mock<ILogger<SQLServerLoginController>>();
        _mockK8sClient = new Mock<IKubernetesClient>();
        _mockEndpointService = new Mock<ISqlServerEndpointService>();
        _mockSqlExecutor = new Mock<ISqlExecutor>();

        _controller = new SQLServerLoginController(
            _mockLogger.Object,
            _mockK8sClient.Object,
            _mockEndpointService.Object,
            _mockSqlExecutor.Object);
    }

    [Fact]
    public async Task ReconcileAsync_WithExternalServer_CreatesLogin()
    {
        // Arrange
        var entity = TestDataBuilder.CreateLogin("test-login", "external-sql", "default");
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
        _mockK8sClient.Setup(x => x.UpdateStatusAsync(It.IsAny<V1Alpha1SQLServerLogin>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((V1Alpha1SQLServerLogin e, CancellationToken ct) => e);

        // Act
        var result = await _controller.ReconcileAsync(entity, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Ready", entity.Status?.State);
        Assert.Equal("Login ensured.", entity.Status?.Message);
        _mockSqlExecutor.Verify(x => x.ExecuteNonQueryAsync(
            It.IsAny<string>(),
            It.Is<string>(cmd => cmd.Contains("CREATE LOGIN")),
            It.Is<Dictionary<string, object>>(p => p.ContainsKey("@LoginName") && p.ContainsKey("@Password"))),
            Times.Once);
    }

    [Fact]
    public async Task ReconcileAsync_WhenServerNotFound_ReturnsFailure()
    {
        // Arrange
        var entity = TestDataBuilder.CreateLogin("test-login", "missing-server", "default");

        _mockK8sClient.Setup(x => x.GetAsync<V1Alpha1ExternalSQLServer>("missing-server", "default"))
            .ReturnsAsync((V1Alpha1ExternalSQLServer?)null);
        _mockK8sClient.Setup(x => x.GetAsync<V1Alpha1SQLServer>("missing-server", "default"))
            .ReturnsAsync((V1Alpha1SQLServer?)null);
        _mockK8sClient.Setup(x => x.UpdateStatusAsync(It.IsAny<V1Alpha1SQLServerLogin>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((V1Alpha1SQLServerLogin e, CancellationToken ct) => e);

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
        var entity = TestDataBuilder.CreateLogin();

        // Act
        var result = await _controller.DeletedAsync(entity, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
    }
}