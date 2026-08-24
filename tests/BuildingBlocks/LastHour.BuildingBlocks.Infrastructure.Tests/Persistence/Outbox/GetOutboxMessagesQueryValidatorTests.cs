using FluentValidation.TestHelper;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Outbox.Queries;

namespace LastHour.BuildingBlocks.Infrastructure.Tests.Persistence.Outbox;

public class GetOutboxMessagesQueryValidatorTests
{
    private readonly GetOutboxMessagesQueryValidator _validator = new();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void Validate_InvalidPageNumber_Fails(int pageNumber)
    {
        var query = new GetOutboxMessagesQuery(pageNumber: pageNumber);
        TestValidationResult<GetOutboxMessagesQuery> result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(q => q.PageNumber)
            .WithErrorMessage("Page number must be greater than or equal to 1.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(101)]
    [InlineData(500)]
    public void Validate_InvalidPageSize_Fails(int pageSize)
    {
        var query = new GetOutboxMessagesQuery(pageSize: pageSize);
        TestValidationResult<GetOutboxMessagesQuery> result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(q => q.PageSize)
            .WithErrorMessage($"Page size must be between 1 and {GetOutboxMessagesQuery.MaxPageSize}.");
    }

    [Fact]
    public void Validate_OccurredFromUtcGreaterThanOccurredToUtc_Fails()
    {
        var query = new GetOutboxMessagesQuery(
            occurredFromUtc: new DateTime(2026, 8, 17, 12, 0, 0, DateTimeKind.Utc),
            occurredToUtc: new DateTime(2026, 8, 17, 10, 0, 0, DateTimeKind.Utc));

        TestValidationResult<GetOutboxMessagesQuery> result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor("OccurredFromUtc")
            .WithErrorMessage("OccurredFromUtc must be less than or equal to OccurredToUtc.");
    }

    [Fact]
    public void Validate_ValidQuery_Succeeds()
    {
        var query = new GetOutboxMessagesQuery(
            status: OutboxMessageStatus.Pending,
            eventType: "TestEvent",
            occurredFromUtc: new DateTime(2026, 8, 17, 10, 0, 0, DateTimeKind.Utc),
            occurredToUtc: new DateTime(2026, 8, 17, 12, 0, 0, DateTimeKind.Utc),
            pageNumber: 1,
            pageSize: 20);

        TestValidationResult<GetOutboxMessagesQuery> result = _validator.TestValidate(query);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
