# Lexxys.Results

A lightweight, flexible result pattern library for .NET that provides a standardized way to handle operation outcomes, eliminating the need for exception-based control flow in business logic.

## Overview

**Lexxys.Results** provides a type-safe, expressive way to represent the success or failure of operations. Instead of throwing exceptions for expected failure cases, you can return structured `Result<T>` objects that clearly communicate success with a value or failure with detailed error information.

### Key Features

- ✅ **Type-safe result handling** - Explicit success and failure states
- 🔗 **Fluent chaining** - Compose operations with `Then`, `Otherwise`, `Cast`, and `Assert`
- 📊 **Rich error information** - Status codes, titles, messages, and custom data
- ⚡ **Async/await support** - Full support for asynchronous operations
- 🎯 **Implicit conversions** - Seamless integration with existing code
- 🔧 **Boolean operators** - Use results directly in conditional statements
- 📦 **Multi-target support** - .NET 10, .NET 8, .NET Standard 2.0, .NET Framework 4.6.2

## Installation

```bash
# NuGet Package Manager
Install-Package Lexxys.Results

# .NET CLI
dotnet add package Lexxys.Results
```

## Quick Start

### Basic Success and Failure

```csharp
using Lexxys;

// Return success with a value
public Result<int> Divide(int a, int b)
{
    if (b == 0)
        return Result.Fail("Cannot divide by zero");
    
    return a / b; // Implicit conversion to Result<int>
}

// Check the result
var result = Divide(10, 2);
if (result.IsSuccess)
{
    Console.WriteLine($"Result: {result.Value}"); // Output: Result: 5
}
```

### Using Boolean Operators

```csharp
var result = Divide(10, 0);

// Use result directly in if statements
if (result)
{
    Console.WriteLine($"Success: {result.Value}");
}
else
{
    Console.WriteLine($"Error: {result.Error.Message}");
}

// Use negation for failure checks
if (!result)
{
    Console.WriteLine($"Operation failed: {result.Error.Message}");
}
```

## Core Types

### `Result` (Non-Generic)

Base class for results without a value.

```csharp
public Result SaveConfiguration(Config config)
{
    if (config == null)
        return Result.Fail("Configuration cannot be null");
    
    // Save logic...
    return Result.Success();
}
```

### `Result<T>` (Generic)

Result that contains a value on success or error on failure.

```csharp
public Result<User> GetUser(int id)
{
    var user = _database.Users.Find(id);
    
    if (user == null)
        return Result.NotFound($"User with ID {id} not found");
    
    return user; // Implicit conversion
}
```

### `ErrorResult`

Structured error information with status code, title, message, and custom data.

```csharp
var error = new ErrorResult(
    message: "Invalid email format",
    title: "Validation Error",
    statusCode: 400,
    data: new Dictionary<string, string?>
    {
        ["Field"] = "Email",
        ["Provided"] = "invalid-email"
    }
);

// Or use factory methods
var badRequest = Result.BadRequest("Invalid input");
var notFound = Result.NotFound("Resource not found");
var custom = Result.Fail("Something went wrong", statusCode: 500);
```

## Fluent API

### `Then` - Chaining Operations

Execute subsequent operations only if the previous one succeeds:

```csharp
public async Task<Result<OrderConfirmation>> PlaceOrder(int userId, OrderDetails details)
{
    return await GetUser(userId)
        .Then(user => ValidateUser(user))
        .Then(user => CreateOrder(user, details))
        .Then(order => ProcessPayment(order))
        .Then(order => SendConfirmation(order));
}

// If any step fails, subsequent steps are skipped and the error propagates
```

### `Otherwise` - Error Recovery

Provide fallback logic when operations fail:

```csharp
public async Task<Result<User>> GetUserWithFallback(int id)
{
    return await GetUserFromCache(id)
        .Otherwise(error => GetUserFromDatabase(id))
        .Otherwise(error => GetGuestUser());
}
```

### `Cast` - Transform Success Values

Transform the value in a successful result:

```csharp
var result = GetUser(123)
    .Cast(user => user.Name)
    .Cast(name => name.ToUpper());

if (result)
{
    Console.WriteLine(result.Value); // "JOHN DOE"
}
```

### `Assert` - Validation

Add validation steps to your chain:

```csharp
public Result<Order> CreateOrder(OrderDetails details)
{
    return Result.Success(new Order(details))
        .Assert(
            o => o.TotalAmount > 0,
            "Order total must be greater than zero",
            statusCode: 400
        )
        .Assert(
            o => o.Items.Count > 0,
            "Order must contain at least one item"
        )
        .Assert(
            o => o.ShippingAddress is null ? Result.Fail("Shipping address is required"): null
        );
}
```

## Asynchronous Support

All extension methods support `Task<Result<T>>`:

```csharp
public async Task<Result<Invoice>> ProcessOrderAsync(int orderId)
{
    return await GetOrderAsync(orderId)
        .Then(async o => await ValidateInventoryAsync(o))
        .Then(async o => await ChargeCustomerAsync(o))
        .Then(async o => await GenerateInvoiceAsync(o))
        .Assert(
            invoice => invoice.Amount > 0,
            "Invoice amount must be positive"
        );
}
```

## Error Handling Patterns

### Validation with Multiple Errors

```csharp
public Result<User> ValidateUser(UserInput input)
{
    var data = new Dictionary<string, string?>();
    
    if (string.IsNullOrEmpty(input.Email))
        data.Add("Email", "Email is required");
    
    if (string.IsNullOrEmpty(input.Password))
        data.Add("Password", "Password is required");
    
    if (input.Age < 18)
        data.Add("Age", "Must be 18 or older");
    
    if (data.Count > 0)
        return new ErrorResult("User validation failed", "Validation Error", 400, data);
    
    return new User(input);
}
```

### HTTP Status Codes

```csharp
public Result<Resource> GetResource(string id)
{
    if (string.IsNullOrEmpty(id))
        return Result.BadRequest("ID is required"); // 400
    
    var resource = _repository.Find(id);
    
    if (resource == null)
        return Result.NotFound($"Resource '{id}' not found"); // 404
    
    if (!HasPermission(resource))
        return Result.Fail("Access denied", statusCode: 403); // 403
    
    return resource;
}
```

### Pattern Matching

```csharp
var result = GetUser(123);

var message = result switch
{
    SuccessResult<User> success => $"Found user: {success.Value.Name}",
    FailureResult<User> failure => $"Error: {failure.Error.Message}",
    _ => "Unknown result"
};
```

## Real-World Examples

### Minimal API Endpoint

```csharp
app.MapGet("/api/users/{id}", async (int id, IUserService userService) =>
{
    var result = await userService.GetUserAsync(id);
    result switch
    {
        SuccessResult<T> success => Results.Ok(success.Value),
        FailureResult<T> failure => Results.Problem(new ProblemDetails
            {
                Title = failure.Error.Title,
                Detail = failure.Error.Message,
                Status = failure.Error.StatusCode,
                Extensions = failure.Error.Data
            }),
        _ => Results.InternalServerError()
    }
});
```

### Service Layer

```csharp
public class UserService
{
    public async Task<Result<User>> CreateUserAsync(UserInput input)
    {
        return await ValidateInput(input)
            .Then(async validInput => await CheckEmailUnique(validInput))
            .Then(async validInput => await HashPassword(validInput))
            .Then(async validInput => await SaveToDatabase(validInput))
            .Then(async user => await SendWelcomeEmail(user))
            .Otherwise(async error => 
            {
                await LogError(error);
                return error;
            });
    }
    
    private Result<UserInput> ValidateInput(UserInput input)
    {
        var error = new ErrorResult("Validation failed", statusCode: 400);
        
        if (!IsValidEmail(input.Email))
            error.Add("email", "Invalid email format");
            
        if (input.Password.Length < 8)
            error.Add("password", "Password must be at least 8 characters");
        
        return error.Data.Count > 0 ? error : Result.Success(input);
    }
}
```

### Repository Pattern

```csharp
public class UserRepository
{
    public async Task<Result<User>> GetByIdAsync(int id)
    {
        try
        {
            var user = await _dbContext.Users
                .Include(u => u.Profile)
                .FirstOrDefaultAsync(u => u.Id == id);
            
            return user ?? Result.NotFound($"User with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database error retrieving user {UserId}", id);
            return Result.Fail("Database error occurred", statusCode: 500);
        }
    }
}
```

## Advanced Scenarios

### Combining Multiple Operations

```csharp
public async Task<Result<Report>> GenerateUserReport(int userId)
{
    var userResult = await GetUserAsync(userId);
    var ordersResult = await GetUserOrdersAsync(userId);
    
    if (!userResult)
        return userResult.Error;
    
    if (!ordersResult)
        return ordersResult.Error;
    
    return new Report(userResult.Value, ordersResult.Value);
}
```

### Error Aggregation

```csharp
public Result<Configuration> LoadConfiguration(string[] filePaths)
{
    var errors = new List<(string Path, ErrorResult Error)>();
    var configs = new List<ConfigSection>();
    
    foreach (var path in filePaths)
    {
        var result = LoadConfigFile(path);
        if (result)
            configs.Add(result.Value);
        else
            errors.Add((path, result.Error));
    }
    
    if (errors.Count > 0)
    {
        var error = new ErrorResult($"Failed to load {errors.Count} configuration files");
        foreach (var e in errors)
            error.Add(e.Path, e.Error);
        return error;
    }
    
    return new Configuration(configs);
}
```

## Best Practices

### ✅ Do's

- **Use for expected failures** - Authentication failures, validation errors, resource not found
- **Chain operations** - Use `Then` and `Cast` for readable, composable code
- **Provide meaningful errors** - Include status codes, titles, and contextual data
- **Use implicit conversions** - Return values directly: `return user;` instead of `return Result.Success(user);`
- **Check before accessing** - Always check `IsSuccess` before accessing `Value`

### ❌ Don'ts

- **Don't use for unexpected exceptions** - Let critical errors bubble up as exceptions
- **Don't access Value on failure** - It will throw `InvalidOperationException`
- **Don't ignore error information** - Propagate errors with full context
- **Don't nest Result types** - Use `Result<T>` not `Result<Result<T>>`

## API Reference

### Static Factory Methods

```csharp
Result.Success()                                    // Non-generic success
Result.Success<T>(T value)                          // Generic success with value
Result.Fail(string message, ...)                    // Generic failure with custom status
Result.BadRequest(string message, ...)              // 400 error
Result.NotFound(string message, ...)                // 404 error
```

### Extension Methods

```csharp
Result<TOut> Then<TIn, TOut>(Func<TIn, Result<TOut>>)
Result<TOut> Cast<TIn, TOut>(Func<TIn, TOut>)
Result<T> Otherwise<T>(Func<ErrorResult, Result<T>>)
Result<T> Assert<T>(Func<T, ErrorResult?>)
Result<T> Assert<T>(Func<T, bool>, string message, ...)
```

### Properties

```csharp
bool IsSuccess                // True if operation succeeded
bool IsFailure                // True if operation failed
T Value                       // Get value (throws if failure)
ErrorResult Error             // Get error (throws if success)
```

## Target Frameworks

- .NET 10.0
- .NET 8.0
- .NET Standard 2.0
- .NET Framework 4.6.2

## License

This code is licensed under the MIT License.

## Contributing

Contributions are welcome! Please submit issues and pull requests to the repository.

## See Also

- **Lexxys.Validation** - Validation framework
- **Lexxys.Testing** - Testing utilities
- **Lexxys** - Core utilities library
