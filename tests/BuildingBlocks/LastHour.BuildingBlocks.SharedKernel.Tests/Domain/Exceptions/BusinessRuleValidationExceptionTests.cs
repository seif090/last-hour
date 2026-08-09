using LastHour.BuildingBlocks.SharedKernel.Domain;
using LastHour.BuildingBlocks.SharedKernel.Domain.Exceptions;

namespace LastHour.BuildingBlocks.SharedKernel.Tests.Domain.Exceptions;

public class BusinessRuleValidationExceptionTests
{
    [Fact]
    public void Constructor_ExposesRuleAndMessage()
    {
        TestBusinessRule rule = new TestBusinessRule();
        BusinessRuleValidationException exception = new BusinessRuleValidationException(rule);

        Assert.Same(rule, exception.Rule);
        Assert.Equal(rule.Message, exception.Message);
    }

    [Fact]
    public void ToString_IncludesRuleNameAndMessage()
    {
        BusinessRuleValidationException exception =
            new BusinessRuleValidationException(new TestBusinessRule());

        Assert.Equal("TestBusinessRule: Rule violated", exception.ToString());
    }

    [Fact]
    public void Constructor_NullRule_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new BusinessRuleValidationException(null!));
    }

    private sealed class TestBusinessRule : IBusinessRule
    {
        public bool IsSatisfied => false;

        public string Message => "Rule violated";
    }
}
