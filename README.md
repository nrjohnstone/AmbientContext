# AmbientContext
Implementation of the base ambient context class from which others can be derived

# Implementing
Derive from AmbientService with the desired generic type parameter. Create a wrapper for the class being turned into an 
AmbientService to allow substitution of a mock/fake during testing.

A simple implementation for an AmbientDateTimeService might look like this

```csharp
public class AmbientDateTimeService : AmbientService<IDateTime>, IDateTime
{
    protected override IDateTime DefaultCreate()
    {
        return new DateTimeAdapter();
    }

    public DateTime Now => Instance.Now;
    public DateTime UtcNow => Instance.UtcNow;
    public DateTime Today => Instance.Today;
}
```    
and the adapter class might look like this

```csharp
public class DateTimeAdapter : IDateTime
{
    public DateTime Now => DateTime.Now;
    public DateTime UtcNow => DateTime.UtcNow;
    public DateTime Today => DateTime.Today;
}
```

# Default Instances
If the derived service has an acceptable default, implement this by overriding the protected virtual DefaultCreate method.

If there is no valid default, this means the user must supply the Create delegate in the composition root.

# Create Delegate
Providing the static create delegate in the composition root means the first time the ambient service is used, this delegate will supply
the instance to be used internally.

If the AmbientDateTimeService above did not have a default create implementation, or if you simply wanted to override the existing default, then to set it in the composition root one would do

```csharp
AmbientDateTimeService.Create = () => new AlternativeDateTimeAdapter();
```

# Usage
To use an AmbientService implementation, simply create a new instance and assign it to a read only property on the class.

```csharp
public readonly AmbientDateTimeService DateTime = new AmbientDateTimeService();
```

Since the AmbientDateTimeService will also implement theIDateTime interface, you can use it directly and it will act as a proxy to the 
underlying instance of the wrapper that was created which in turn calls the .NET BCL DateTime static directly.

# Testing
An AmbientService can be mocked for testing by setting the Instance property to a suitable mock/fake implementation.

```csharp
sut.DateTime.Instance = new Mock<IDateTime>();
```
