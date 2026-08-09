using LastHour.BuildingBlocks.SharedKernel.Domain.Exceptions;

namespace LastHour.BuildingBlocks.SharedKernel.Tests.Domain.Exceptions;

public class DomainExceptionTests
{
    [Fact]
    public void ParameterlessConstructor_CreatesException()
    {
        DomainException exception = new DomainException();

        Assert.IsType<DomainException>(exception);
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public void MessageConstructor_PreservesMessage()
    {
        DomainException exception = new DomainException("broken");

        Assert.Equal("broken", exception.Message);
    }

    [Fact]
    public void MessageAndInnerConstructor_PreservesBoth()
    {
        InvalidOperationException inner = new InvalidOperationException("inner");
        DomainException exception = new DomainException("broken", inner);

        Assert.Equal("broken", exception.Message);
        Assert.Same(inner, exception.InnerException);
    }
}
