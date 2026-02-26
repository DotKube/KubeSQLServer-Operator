using k8s.Models;
using KubeOps.KubernetesClient;
using Microsoft.Extensions.Logging;
using Moq;
using OperatorTemplate.Operator.UnitTests.Helpers;
using SqlServerOperator.Controllers.V1Alpha1;
using SqlServerOperator.Controllers.Services;
using SqlServerOperator.Entities.V1Alpha1;
using Xunit;

namespace OperatorTemplate.Operator.UnitTests.Controllers;

public class ExternalSqlServerControllerTests
{
    private readonly Mock<ILogger<ExternalSQLServerController>> _mockLogger;
    private readonly Mock<IKubernetesClient> _mockK8sClient;
    private readonly Mock<ISqlExecutor> _mockSqlExecutor;
    private readonly ExternalSQLServerController _controller;

    public ExternalSqlServerControllerTests()
    {
        _mockLogger = new Mock<ILogger<ExternalSQLServerController>>();
        _mockK8sClient = new Mock<IKubernetesClient>();
        _mockSqlExecutor = new Mock<ISqlExecutor>();

        _controller = new ExternalSQLServerController(
            _mockLogger.Object,
            _mockK8sClient.Object,
            _mockSqlExecutor.Object);
    }

    [Fact]
    public async Task ReconcileAsync_WithValidConnection_VerifiesSuccessfully()
    {
        // Arrange
        var entity = TestDataBuilder.CreateExternalSqlServer("external-sql", "default");
        var secret = TestDataBuilder.CreateSecret("external-secret", "default", "TestPass123!", "sa");

        _mockK8sClient.Setup(x => x.GetAsync<V1Secret>("external-secret", "default"))
            .ReturnsAsync(secret);
        _mockSqlExecutor.Setup(x => x.ExecuteScalarAsync<string>(It.IsAny<string>(), "SELECT @@VERSION", null))
            .ReturnsAsync("Microsoft SQL Server 2022");
        _mockK8sClient.Setup(x => x.UpdateStatusAsync(It.IsAny<V1Alpha1ExternalSQLServer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((V1Alpha1ExternalSQLServer e, CancellationToken ct) => e);

        // Act
        var result = await _controller.ReconcileAsync(entity, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Ready", entity.Status?.State);
        Assert.Equal("Connection verified successfully.", entity.Status?.Message);
        Assert.True(entity.Status?.IsConnected);
        _mockSqlExecutor.Verify(x => x.ExecuteScalarAsync<string>(
            It.IsAny<string>(),
            "SELECT @@VERSION",
            null),
            Times.Once);
    }

    [Fact]
    public async Task ReconcileAsync_WhenSecretMissing_ReturnsFailure()
    {
        // Arrange
        var entity = TestDataBuilder.CreateExternalSqlServer("external-sql", "default");

        _mockK8sClient.Setup(x => x.GetAsync<V1Secret>("external-secret", "default"))
            .ReturnsAsync((V1Secret?)null);
        _mockK8sClient.Setup(x => x.UpdateStatusAsync(It.IsAny<V1Alpha1ExternalSQLServer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((V1Alpha1ExternalSQLServer e, CancellationToken ct) => e);

        // Act
        var result = await _controller.ReconcileAsync(entity, CancellationToken.None);

        // Assert
        Assert.Equal("Error", entity.Status?.State);
        Assert.False(entity.Status?.IsConnected);
        _mockSqlExecutor.Verify(x => x.ExecuteScalarAsync<string>(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Dictionary<string, object>>()),
            Times.Never);
    }

    [Fact]
    public async Task ReconcileAsync_WhenConnectionFails_ReturnsFailure()
    {
        // Arrange
        var entity = TestDataBuilder.CreateExternalSqlServer("external-sql", "default");
        var secret = TestDataBuilder.CreateSecret("external-secret", "default", "TestPass123!");

        _mockK8sClient.Setup(x => x.GetAsync<V1Secret>("external-secret", "default"))
            .ReturnsAsync(secret);
        _mockSqlExecutor.Setup(x => x.ExecuteScalarAsync<string>(It.IsAny<string>(), "SELECT @@VERSION", null))
            .ThrowsAsync(new Exception("Connection timeout"));
        _mockK8sClient.Setup(x => x.UpdateStatusAsync(It.IsAny<V1Alpha1ExternalSQLServer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((V1Alpha1ExternalSQLServer e, CancellationToken ct) => e);

        // Act
        var result = await _controller.ReconcileAsync(entity, CancellationToken.None);

        // Assert
        Assert.Equal("Error", entity.Status?.State);
        Assert.Contains("Connection timeout", entity.Status?.Message);
        Assert.False(entity.Status?.IsConnected);
    }

    [Fact]
    public async Task DeletedAsync_ReturnsSuccess()
    {
        // Arrange
        var entity = TestDataBuilder.CreateExternalSqlServer();

        // Act
        var result = await _controller.DeletedAsync(entity, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
    }
}