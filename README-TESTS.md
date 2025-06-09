# ToDo Application Test Suite

This project includes a comprehensive test suite with **Unit Tests**, **Integration Tests**, and **Functional Tests** to ensure the reliability and quality of the ToDo application.

## Test Structure

The test suite is organized into three main categories:

### 📋 Test Projects Overview

```
ToDo.sln
├── ToDo/                           # Main application
├── ToDo.UnitTests/                 # Unit Tests
│   ├── TodoServiceTests.cs
│   ├── AuthServiceTests.cs
│   └── ToDo.UnitTests.csproj
├── ToDo.IntegrationTests/          # Integration Tests
│   ├── TodoApiIntegrationTests.cs
│   └── ToDo.IntegrationTests.csproj
└── ToDo.FunctionalTests/           # Functional Tests
    ├── TodoAppFunctionalTests.cs
    └── ToDo.FunctionalTests.csproj
```

## 🧪 Test Types

### 1. Unit Tests (`ToDo.UnitTests`)

**Purpose**: Test individual components in isolation with mocked dependencies.

**Test Files**:
- **`TodoServiceTests.cs`** - Tests all TodoService methods
- **`AuthServiceTests.cs`** - Tests all AuthService methods

**Key Features**:
- ✅ Tests business logic in isolation
- ✅ Uses in-memory database for each test
- ✅ Mocks external dependencies
- ✅ Fast execution (< 1 second per test)
- ✅ Tests edge cases and error scenarios

**Example Test Scenarios**:
```csharp
// TodoService Tests
- Add todo with default status
- Update todo status transitions
- Get user-specific todos
- Handle overdue todos
- Delete operations

// AuthService Tests  
- User registration and login
- JWT token generation
- Refresh token handling
- Password hashing verification
- Logout and token cleanup
```

### 2. Integration Tests (`ToDo.IntegrationTests`)

**Purpose**: Test the integration between multiple components and services.

**Test Files**:
- **`TodoApiIntegrationTests.cs`** - Tests service integrations and data flow

**Key Features**:
- ✅ Tests service-to-service communication
- ✅ Validates database operations end-to-end
- ✅ Uses WebApplicationFactory for realistic testing
- ✅ Tests data persistence and retrieval
- ✅ Validates business workflow integrity

**Example Test Scenarios**:
```csharp
// Integration Workflows
- Complete todo lifecycle (CRUD operations)
- User authentication flow with database
- Multi-user data isolation
- Status transitions with persistence
- Error handling across service boundaries
```

### 3. Functional Tests (`ToDo.FunctionalTests`)

**Purpose**: Test complete user workflows and end-to-end scenarios.

**Test Files**:
- **`TodoAppFunctionalTests.cs`** - Tests complete user scenarios

**Key Features**:
- ✅ Tests complete user journeys
- ✅ Validates application behavior from user perspective
- ✅ Tests real-world scenarios
- ✅ Includes error handling and edge cases
- ✅ Validates data integrity across operations

**Example Test Scenarios**:
```csharp
// End-to-End Workflows
- User registration → Login → Create Todos → Management
- Multi-user scenarios with data isolation
- Authentication flow with token refresh
- Todo status management consistency
- Database persistence verification
- Error handling for invalid operations
```

## 🛠️ Technology Stack

**Testing Frameworks**:
- **xUnit** - Primary testing framework
- **Moq** - Mocking framework for unit tests
- **ASP.NET Core Test Host** - Integration testing
- **Entity Framework InMemory** - Database testing

**Key Dependencies**:
- `Microsoft.AspNetCore.Mvc.Testing` - Web application testing
- `Microsoft.EntityFrameworkCore.InMemory` - In-memory database
- `Moq` - Mocking framework
- `Microsoft.Extensions.Configuration` - Configuration testing

## 🚀 Running the Tests

### Run All Tests
```bash
# Run all test projects
dotnet test

# Run with detailed output
dotnet test --verbosity normal

# Run with coverage (if coverage tools are installed)
dotnet test --collect:"XPlat Code Coverage"
```

### Run Specific Test Projects
```bash
# Run only Unit Tests
dotnet test ToDo.UnitTests

# Run only Integration Tests
dotnet test ToDo.IntegrationTests

# Run only Functional Tests
dotnet test ToDo.FunctionalTests
```

### Run Specific Test Classes
```bash
# Run specific test class
dotnet test --filter "FullyQualifiedName~TodoServiceTests"

# Run specific test method
dotnet test --filter "Method=GetTodosAsync_ShouldReturnAllTodos"
```

### Run Tests in Parallel
```bash
# Run tests in parallel for faster execution
dotnet test --parallel
```

## 📊 Test Coverage

### Unit Tests Coverage
- **TodoService**: 100% method coverage
  - ✅ GetTodosAsync
  - ✅ GetUserTodosAsync  
  - ✅ GetUserCompletedTodosAsync
  - ✅ GetUserOverdueTodosAsync
  - ✅ GetTodoByIdAsync
  - ✅ AddTodoAsync
  - ✅ UpdateTodoAsync
  - ✅ DeleteTodoAsync

- **AuthService**: 100% method coverage
  - ✅ RegisterAsync
  - ✅ LoginAsync
  - ✅ RefreshTokenAsync
  - ✅ LogoutAsync
  - ✅ Token generation and validation

### Integration Tests Coverage
- ✅ Service-to-service integration
- ✅ Database persistence operations
- ✅ Authentication flow integration
- ✅ Data isolation between users
- ✅ Status transition consistency

### Functional Tests Coverage
- ✅ Complete user workflows
- ✅ Multi-user scenarios
- ✅ Error handling scenarios
- ✅ Data integrity validation
- ✅ Application startup verification

## 🎯 Test Scenarios

### Critical User Journeys
1. **New User Journey**
   - User registration
   - Email verification (if applicable)
   - First login
   - Create first todo
   - Complete todo workflow

2. **Existing User Journey**
   - User login
   - View existing todos
   - Create new todos
   - Update todo status
   - Delete completed todos

3. **Multi-User Scenarios**
   - Data isolation between users
   - Concurrent user operations
   - User-specific todo retrieval

### Edge Cases & Error Scenarios
- Invalid login credentials
- Duplicate user registration
- Non-existent todo operations
- Expired token handling
- Database connection issues
- Invalid data input validation

## 🔧 Test Configuration

### In-Memory Database
Tests use Entity Framework InMemory provider for:
- Fast test execution
- Isolated test data
- No external dependencies
- Easy cleanup between tests

### Test Data Setup
Each test class includes:
- Fresh database instance per test
- Proper disposal of resources
- Realistic test data scenarios
- Consistent test environment

## 📈 Continuous Integration

### Recommended CI Pipeline
```yaml
# Example GitHub Actions workflow
- name: Run Unit Tests
  run: dotnet test ToDo.UnitTests --no-build --verbosity normal

- name: Run Integration Tests  
  run: dotnet test ToDo.IntegrationTests --no-build --verbosity normal

- name: Run Functional Tests
  run: dotnet test ToDo.FunctionalTests --no-build --verbosity normal
```

### Test Execution Order
1. **Unit Tests** (fastest, run first)
2. **Integration Tests** (medium speed)
3. **Functional Tests** (comprehensive, run last)

## 🛡️ Best Practices Implemented

### Test Organization
- ✅ Clear test naming conventions
- ✅ Arrange-Act-Assert pattern
- ✅ One assertion per test concept
- ✅ Descriptive test method names

### Test Data Management
- ✅ Fresh test data for each test
- ✅ Realistic test scenarios
- ✅ Proper cleanup and disposal
- ✅ Isolated test environments

### Maintainability
- ✅ DRY principle in test setup
- ✅ Helper methods for common operations
- ✅ Clear documentation and comments
- ✅ Consistent coding style

## 🐛 Debugging Tests

### Common Issues & Solutions

1. **Test Database Issues**
   ```bash
   # Clear test databases
   dotnet ef database drop --context ApplicationDbContext
   ```

2. **Dependency Injection Issues**
   ```bash
   # Verify service registration in test setup
   # Check WebApplicationFactory configuration
   ```

3. **Async Test Issues**
   ```bash
   # Ensure proper async/await usage
   # Use ConfigureAwait(false) when appropriate
   ```

## 📝 Adding New Tests

### For New Features
1. Add unit tests for new service methods
2. Add integration tests for new workflows
3. Add functional tests for new user scenarios
4. Update this documentation

### Test Template
```csharp
[Fact]
public async Task MethodName_Scenario_ExpectedResult()
{
    // Arrange
    var testData = CreateTestData();
    
    // Act
    var result = await _service.MethodAsync(testData);
    
    // Assert
    Assert.NotNull(result);
    Assert.Equal(expected, result.Property);
}
```

## 🎉 Benefits

This comprehensive test suite provides:
- ✅ **Confidence** in code changes
- ✅ **Early bug detection**
- ✅ **Documentation** of expected behavior
- ✅ **Regression prevention**
- ✅ **Faster development** cycles
- ✅ **Better code quality**

---

## 📞 Support

For questions about the test suite:
1. Check test output for detailed error messages
2. Review test documentation in code comments
3. Verify test environment setup
4. Run tests individually to isolate issues

**Happy Testing! 🚀** 