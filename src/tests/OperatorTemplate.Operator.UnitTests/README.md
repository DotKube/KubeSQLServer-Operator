# KubeSQLServer Operator - Unit Tests

Unit tests for the KubeSQLServer Operator using xUnit, Moq, and AutoFixture.

## Running Tests

```bash
dotnet test
```

## Test Framework

- **xUnit** - Test framework
- **Moq** - Mocking dependencies
- **AutoFixture** - Test data generation

All tests mock external dependencies (Kubernetes client, SQL executor) to verify reconciliation logic, error handling, and status updates.
