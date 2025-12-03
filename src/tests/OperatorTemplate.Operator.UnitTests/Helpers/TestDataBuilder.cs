using k8s.Models;
using SqlServerOperator.Entities;
using System.Text;

namespace OperatorTemplate.Operator.UnitTests.Helpers;

public static class TestDataBuilder
{
    public static V1Alpha1SQLServer CreateSqlServer(string name = "test-sqlserver", string namespaceName = "default")
    {
        return new V1Alpha1SQLServer
        {
            Metadata = new V1ObjectMeta
            {
                Name = name,
                NamespaceProperty = namespaceName
            },
            Spec = new V1Alpha1SQLServer.V1Alpha1SQLServerSpec
            {
                Image = "mcr.microsoft.com/mssql/server:2022-latest",
                StorageClass = "standard",
                StorageSize = "1Gi",
                SecretName = $"{name}-secret",
                ServiceType = "NodePort"
            }
        };
    }

    public static V1Alpha1ExternalSQLServer CreateExternalSqlServer(string name = "test-external-sql", string namespaceName = "default")
    {
        return new V1Alpha1ExternalSQLServer
        {
            Metadata = new V1ObjectMeta
            {
                Name = name,
                NamespaceProperty = namespaceName
            },
            Spec = new V1Alpha1ExternalSQLServer.V1Alpha1ExternalSQLServerSpec
            {
                Host = "localhost",
                Port = 1433,
                SecretName = "external-secret",
                UseEncryption = false,
                TrustServerCertificate = true
            }
        };
    }

    public static V1Alpha1SQLServerDatabase CreateDatabase(string name = "test-database", string instanceName = "test-sqlserver", string namespaceName = "default")
    {
        return new V1Alpha1SQLServerDatabase
        {
            Metadata = new V1ObjectMeta
            {
                Name = name,
                NamespaceProperty = namespaceName
            },
            Spec = new V1Alpha1SQLServerDatabase.V1Alpha1SQLServerDatabaseSpec
            {
                InstanceName = instanceName,
                DatabaseName = "TestDB"
            }
        };
    }

    public static V1Alpha1SQLServerLogin CreateLogin(string name = "test-login", string sqlServerName = "test-sqlserver", string namespaceName = "default")
    {
        return new V1Alpha1SQLServerLogin
        {
            Metadata = new V1ObjectMeta
            {
                Name = name,
                NamespaceProperty = namespaceName
            },
            Spec = new V1Alpha1SQLServerLogin.V1Alpha1SQLServerLoginSpec
            {
                SqlServerName = sqlServerName,
                LoginName = "testuser",
                AuthenticationType = "SQL",
                SecretName = "login-secret"
            }
        };
    }

    public static V1Alpha1DatabaseUser CreateDatabaseUser(
        string name = "test-user", 
        string loginName = "testuser",
        string databaseName = "TestDB",
        string sqlServerName = "test-sqlserver", 
        string namespaceName = "default")
    {
        return new V1Alpha1DatabaseUser
        {
            Metadata = new V1ObjectMeta
            {
                Name = name,
                NamespaceProperty = namespaceName
            },
            Spec = new V1Alpha1DatabaseUser.V1Alpha1DatabaseUserSpec
            {
                SqlServerName = sqlServerName,
                DatabaseName = databaseName,
                LoginName = loginName,
                Roles = new List<string> { "db_datareader", "db_datawriter" }
            }
        };
    }

    public static V1Alpha1SQLServerSchema CreateSchema(string name = "test-schema", string instanceName = "test-sqlserver", string namespaceName = "default")
    {
        return new V1Alpha1SQLServerSchema
        {
            Metadata = new V1ObjectMeta
            {
                Name = name,
                NamespaceProperty = namespaceName
            },
            Spec = new V1Alpha1SQLServerSchema.V1Alpha1SQLServerSchemaSpec
            {
                InstanceName = instanceName,
                DatabaseName = "TestDB",
                SchemaName = "TestSchema",
                SchemaOwner = "dbo"
            }
        };
    }

    public static V1Secret CreateSecret(string name, string namespaceName, string password, string? username = null)
    {
        var data = new Dictionary<string, byte[]>
        {
            ["password"] = Encoding.UTF8.GetBytes(password)
        };

        if (username != null)
        {
            data["username"] = Encoding.UTF8.GetBytes(username);
        }

        return new V1Secret
        {
            Metadata = new V1ObjectMeta
            {
                Name = name,
                NamespaceProperty = namespaceName
            },
            Data = data
        };
    }

    public static V1Service CreateService(string name, string namespaceName, string serviceType = "ClusterIP", string? externalIP = null)
    {
        var service = new V1Service
        {
            Metadata = new V1ObjectMeta
            {
                Name = name,
                NamespaceProperty = namespaceName
            },
            Spec = new V1ServiceSpec
            {
                Type = serviceType,
                Ports = new List<V1ServicePort>
                {
                    new V1ServicePort { Port = 1433, TargetPort = 1433 }
                }
            }
        };

        if (serviceType == "LoadBalancer" && externalIP != null)
        {
            service.Status = new V1ServiceStatus
            {
                LoadBalancer = new V1LoadBalancerStatus
                {
                    Ingress = new List<V1LoadBalancerIngress>
                    {
                        new V1LoadBalancerIngress { Ip = externalIP }
                    }
                }
            };
        }

        return service;
    }
}
