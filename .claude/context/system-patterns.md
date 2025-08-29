---
created: 2025-08-29T07:40:21Z
last_updated: 2025-08-29T07:40:21Z
version: 1.0
author: Claude Code PM System
---

# System Patterns

## Architectural Patterns

### Service Layer Pattern
The application implements a clean service layer architecture:
```
UI Layer (Blazor Components)
    ↓
Service Layer (Business Logic)
    ↓
Data Access Layer (EF Core)
    ↓
Database (SQLite/PostgreSQL)
```

### Dependency Injection Pattern
- All services registered in `Program.cs`
- Interface-based programming for testability
- Scoped lifetime for database operations
- Singleton for cross-cutting concerns

### Repository Pattern (Implicit)
- DbContext acts as Unit of Work
- DbSet<T> provides repository functionality
- Service layer abstracts database operations

## Design Patterns Observed

### 1. Observer Pattern
**Implementation**: ToastService
```csharp
public class ToastService : IToastService
{
    public event Action<string, string> OnShow;
    public void ShowToast(string message, string cssClass) 
        => OnShow?.Invoke(message, cssClass);
}
```
- Components subscribe to toast events
- Decoupled notification system

### 2. Strategy Pattern
**Implementation**: Validation Rules
- Different validation strategies per field type
- Pluggable validation logic
- Runtime strategy selection

### 3. Factory Pattern (Implicit)
**Implementation**: Service Registration
```csharp
builder.Services.AddScoped<IPromptService, PromptService>();
```
- DI container acts as factory
- Creates service instances on demand

### 4. Singleton Pattern
**Implementation**: Cross-Component Communication
```csharp
builder.Services.AddSingleton<IToastService, ToastService>();
```
- Single instance shared across application
- Maintains global state for notifications

### 5. Unit of Work Pattern
**Implementation**: DbContext
- Tracks entity changes
- Coordinates database writes
- Ensures transactional consistency

## Data Flow Patterns

### Query Pattern
```
Component → Service → DbContext → Database
    ↑←←←←←←←←←←←←←←←←←←←←←←←←←←←←←↓
```

### Command Pattern
```
Component → Validation → Service → DbContext → Database
    ↑←←←←←←← Result ←←←←←←←←←←←←←←←←←←←←←←←↓
```

### Event Flow
```
Action → Service → Event → Subscribers → UI Update
```

## Error Handling Patterns

### Global Exception Handling
- Middleware intercepts all exceptions
- Logs errors with context
- Returns user-friendly messages

### Service-Level Try-Catch
```csharp
try
{
    // Business logic
}
catch (DbUpdateConcurrencyException)
{
    // Handle specific exception
}
catch (Exception ex)
{
    // Log and wrap
}
```

### Validation Pipeline
1. Client-side validation (HTML5)
2. Component validation (Blazor)
3. Service validation (Business rules)
4. Database constraints (Final check)

## Concurrency Patterns

### Optimistic Concurrency Control
- RowVersion field on entities
- Conflict detection on save
- User resolution for conflicts

### Async/Await Pattern
- All database operations async
- Non-blocking UI updates
- Improved scalability

## State Management Patterns

### Server-Side State
- Blazor Server maintains component state
- SignalR connection preserves state
- Automatic reconnection handling

### Scoped State
- Per-request service instances
- Isolated user sessions
- Thread-safe operations

## Security Patterns

### Input Sanitization Pipeline
```
User Input → HTML Encoding → Validation → Storage
```

### Parameterized Queries
- All database queries parameterized
- LINQ prevents SQL injection
- No raw SQL execution

## Performance Patterns

### Eager Loading
```csharp
_context.Prompts
    .Include(p => p.PromptTags)
    .ThenInclude(pt => pt.Tag)
```

### Compiled Queries
- Frequently used queries pre-compiled
- Reduced query parsing overhead
- Improved response times

### Pagination Pattern
```csharp
query.Skip((page - 1) * pageSize).Take(pageSize)
```

## Integration Patterns

### Health Check Pattern
```csharp
app.MapHealthChecks("/health");
app.MapGet("/alive", () => "OK");
```

### Service Discovery Pattern (Aspire)
- Automatic service registration
- Dynamic endpoint resolution
- Load balancing ready

## Monitoring Patterns

### Structured Logging
```csharp
_logger.LogInformation("Action {Action} by {User}", action, userId);
```

### Metrics Collection
- Performance counters
- Custom metrics
- OpenTelemetry integration

### Distributed Tracing
- Request correlation IDs
- Cross-service tracing
- Performance bottleneck identification

## Testing Patterns (Planned)

### Arrange-Act-Assert
- Clear test structure
- Isolated test scenarios
- Predictable outcomes

### Test Data Builder Pattern
- Fluent interfaces for test data
- Reusable test fixtures
- Maintainable test code

## Anti-Patterns Avoided

### ❌ God Object
- Services have single responsibilities
- Clear separation of concerns

### ❌ Anemic Domain Model
- Models contain business logic where appropriate
- Not just data containers

### ❌ Magic Strings
- Configuration in appsettings.json
- Constants for repeated values

### ❌ Premature Optimization
- Performance monitoring identifies bottlenecks
- Optimization based on metrics

### ❌ Copy-Paste Programming
- Shared services for common functionality
- Component reuse through Razor components