using FluentAssertions;
using LastHour.BuildingBlocks.Infrastructure.Observability.FailureClassification;
using Microsoft.EntityFrameworkCore;

namespace LastHour.BuildingBlocks.Infrastructure.Tests.Observability;

public class FailureClassifierTests
{
    [Fact]
    public void Classify_Null_ReturnsUnknown()
    {
        FailureClassifier.Classify(null).Should().Be(FailureClassifier.Unknown);
    }

    [Fact]
    public void Classify_CancellationExceptions_ReturnsCancellation()
    {
        FailureClassifier.Classify(new OperationCanceledException()).Should().Be(FailureClassifier.Cancellation);
        FailureClassifier.Classify(new TaskCanceledException()).Should().Be(FailureClassifier.Cancellation);
    }

    [Fact]
    public void Classify_ConcurrencyException_ReturnsConcurrency()
    {
        FailureClassifier.Classify(new DbUpdateConcurrencyException("concurrency")).Should().Be(FailureClassifier.Concurrency);
    }

    [Fact]
    public void Classify_DatabaseExceptions_ReturnsDatabase()
    {
        FailureClassifier.Classify(new DbUpdateException("db update", new InvalidOperationException("inner"))).Should().Be(FailureClassifier.Database);
    }

    [Fact]
    public void Classify_TimeoutException_ReturnsTransient()
    {
        FailureClassifier.Classify(new TimeoutException("timeout")).Should().Be(FailureClassifier.Transient);
    }

    [Fact]
    public void Classify_ValidationExceptions_ReturnsValidation()
    {
        FailureClassifier.Classify(new ArgumentException("invalid arg")).Should().Be(FailureClassifier.Validation);
        FailureClassifier.Classify(new ArgumentNullException("null arg")).Should().Be(FailureClassifier.Validation);
        FailureClassifier.Classify(new FormatException("bad format")).Should().Be(FailureClassifier.Validation);
    }

    [Fact]
    public void Classify_InfrastructureExceptions_ReturnsInfrastructure()
    {
        FailureClassifier.Classify(new HttpRequestException("network down")).Should().Be(FailureClassifier.Infrastructure);
        FailureClassifier.Classify(new System.Net.Sockets.SocketException()).Should().Be(FailureClassifier.Infrastructure);
    }

    [Fact]
    public void Classify_GeneralException_ReturnsUnknown()
    {
        FailureClassifier.Classify(new InvalidOperationException("generic error")).Should().Be(FailureClassifier.Unknown);
    }
}
