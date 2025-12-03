using k8s.Models;
using KubeOps.KubernetesClient;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using OperatorTemplate.Operator.UnitTests.Helpers;
using SqlServerOperator.Controllers.Services;
using SqlServerOperator.Entities;
using Xunit;

namespace OperatorTemplate.Operator.UnitTests.Services;

public class SqlServerEndpointServiceTests
{
    private readonly Mock<ILogger<SqlServerEndpointService>> _mockLogger;
    private readonly Mock<IKubernetesClient> _mockK8sClient;
    private readonly Mock<IWebHostEnvironment> _mockEnvironment;
    private readonly SqlServerEndpointService _service;

    public SqlServerEndpointServiceTests()
    {
        _mockLogger = new Mock<ILogger<SqlServerEndpointService>>();
        _mockK8sClient = new Mock<IKubernetesClient>();
        _mockEnvironment = new Mock<IWebHostEnvironment>();
        
        _service = new SqlServerEndpointService(_mockLogger.Object, _mockK8sClient.Object, _mockEnvironment.Object);
    }

    [Fact]
    public async Task GetSqlServerEndpointAsync_WithExternalServer_ReturnsHostAndPort()
    {
        // Arrange
        var externalServer = TestDataBuilder.CreateExternalSqlServer("external-sql", "default");
        _mockK8sClient.Setup(x => x.GetAsync<V1Alpha1ExternalSQLServer>("external-sql", "default"))
            .ReturnsAsync(externalServer);

        // Act
        var result = await _service.GetSqlServerEndpointAsync("external-sql", "default");

        // Assert
        Assert.Equal("localhost,1433", result);
    }

    [Fact]
    public async Task GetSqlServerEndpointAsync_ProductionEnvironment_ReturnsHeadlessFQDN()
    {
        // Arrange
        _mockK8sClient.Setup(x => x.GetAsync<V1Alpha1ExternalSQLServer>(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((V1Alpha1ExternalSQLServer?)null);
        _mockEnvironment.Setup(x => x.EnvironmentName).Returns(Environments.Production);
        _mockK8sClient.Setup(x => x.GetAsync<V1Service>("sqlserver-service", "default"))
            .ReturnsAsync(TestDataBuilder.CreateService("sqlserver-service", "default"));

        // Act
        var result = await _service.GetSqlServerEndpointAsync("sqlserver", "default");

        // Assert
        Assert.Equal("sqlserver-headless.default.svc.cluster.local", result);
    }

    [Fact]
    public async Task GetSqlServerEndpointAsync_DevWithLoadBalancer_ReturnsExternalIP()
    {
        // Arrange
        _mockK8sClient.Setup(x => x.GetAsync<V1Alpha1ExternalSQLServer>(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((V1Alpha1ExternalSQLServer?)null);
        _mockEnvironment.Setup(x => x.EnvironmentName).Returns(Environments.Development);
        _mockK8sClient.Setup(x => x.GetAsync<V1Service>("sqlserver-service", "default"))
            .ReturnsAsync(TestDataBuilder.CreateService("sqlserver-service", "default", "LoadBalancer", "203.0.113.1"));

        // Act
        var result = await _service.GetSqlServerEndpointAsync("sqlserver", "default");

        // Assert
        Assert.Equal("203.0.113.1", result);
    }

    [Fact]
    public async Task GetSqlServerEndpointAsync_DevWithNodePort_ReturnsLocalhost()
    {
        // Arrange
        _mockK8sClient.Setup(x => x.GetAsync<V1Alpha1ExternalSQLServer>(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((V1Alpha1ExternalSQLServer?)null);
        _mockEnvironment.Setup(x => x.EnvironmentName).Returns(Environments.Development);
        _mockK8sClient.Setup(x => x.GetAsync<V1Service>("sqlserver-service", "default"))
            .ReturnsAsync(TestDataBuilder.CreateService("sqlserver-service", "default", "NodePort"));

        // Act
        var result = await _service.GetSqlServerEndpointAsync("sqlserver", "default");

        // Assert
        Assert.Equal("localhost,1434", result);
    }

    [Fact]
    public async Task GetSqlServerEndpointAsync_DevWithNoneServiceType_ReturnsHeadlessFQDN()
    {
        // Arrange
        _mockK8sClient.Setup(x => x.GetAsync<V1Alpha1ExternalSQLServer>(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((V1Alpha1ExternalSQLServer?)null);
        _mockEnvironment.Setup(x => x.EnvironmentName).Returns(Environments.Development);
        _mockK8sClient.Setup(x => x.GetAsync<V1Service>("sqlserver-service", "default"))
            .ReturnsAsync((V1Service?)null);

        // Act
        var result = await _service.GetSqlServerEndpointAsync("sqlserver", "default");

        // Assert
        Assert.Equal("sqlserver-headless.default.svc.cluster.local", result);
    }

    [Fact]
    public async Task GetSqlServerEndpointAsync_LoadBalancerWithoutIP_ThrowsException()
    {
        // Arrange
        _mockK8sClient.Setup(x => x.GetAsync<V1Alpha1ExternalSQLServer>(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((V1Alpha1ExternalSQLServer?)null);
        _mockEnvironment.Setup(x => x.EnvironmentName).Returns(Environments.Development);
        _mockK8sClient.Setup(x => x.GetAsync<V1Service>("sqlserver-service", "default"))
            .ReturnsAsync(TestDataBuilder.CreateService("sqlserver-service", "default", "LoadBalancer"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(
            async () => await _service.GetSqlServerEndpointAsync("sqlserver", "default"));
        Assert.Contains("Unable to determine a valid SQL Server endpoint", exception.Message);
    }
}
