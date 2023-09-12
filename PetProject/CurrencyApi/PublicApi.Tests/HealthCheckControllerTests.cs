using Fuse8_ByteMinds.SummerSchool.PublicApi.Controllers;
using Grpc.Core;
using Grpc.Health.V1;
using Moq;

namespace PublicApi.Tests;

public class HealthCheckControllerTests
{
    private readonly Mock<Health.HealthClient> _healthClientMock;
    private readonly HealthCheckController _sut;

    public HealthCheckControllerTests()
    {
        _healthClientMock = new Mock<Health.HealthClient>();

        _sut = new HealthCheckController(_healthClientMock.Object);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(null)]
    public async Task Check_ReturnOkStatus_WithoutExternalServiceChecks(bool? checkExternalApi)
    {
        // Arrange
        var ct = CancellationToken.None;
        var expected = new HealthCheckResult
        {
            CheckedOn = default,
            Status = HealthCheckResult.CheckStatus.Ok
        };
        // Act
        var actual = await _sut.Check(checkExternalApi, ct);

        // Assert

        Assert.Equal(expected.Status, actual.Status);
        _healthClientMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Check_ReturnOkStatus_WhenExternalServiceServing()
    {
        // Arrange
        var ct = CancellationToken.None;
        var expected = new HealthCheckResult
        {
            CheckedOn = default,
            Status = HealthCheckResult.CheckStatus.Ok
        };

        var healthResponse = new HealthCheckResponse
        {
            Status = HealthCheckResponse.Types.ServingStatus.Serving
        };
        var unaryCall = new AsyncUnaryCall<HealthCheckResponse>(
            Task.FromResult(healthResponse), Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess, () => new Metadata(), () => { });
        _healthClientMock.Setup(c => c.CheckAsync(It.IsAny<HealthCheckRequest>(), It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(unaryCall);

        // Act
        var actual = await _sut.Check(true, ct);

        // Assert

        Assert.Equal(expected.Status, actual.Status);
        _healthClientMock.Verify(
            c => c.CheckAsync(It.IsAny<HealthCheckRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    [Fact]
    public async Task Check_ReturnFailedStatus_WhenExternalServiceNotServing()
    {
        // Arrange
        var ct = CancellationToken.None;
        var expected = new HealthCheckResult
        {
            CheckedOn = default,
            Status = HealthCheckResult.CheckStatus.Failed
        };

        var healthResponse = new HealthCheckResponse
        {
            Status = HealthCheckResponse.Types.ServingStatus.NotServing
        };
        var unaryCall = new AsyncUnaryCall<HealthCheckResponse>(
            Task.FromResult(healthResponse), Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess, () => new Metadata(), () => { });
        _healthClientMock.Setup(c => c.CheckAsync(It.IsAny<HealthCheckRequest>(), It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(unaryCall);

        // Act
        var actual = await _sut.Check(true, ct);

        // Assert

        Assert.Equal(expected.Status, actual.Status);
        _healthClientMock.Verify(
            c => c.CheckAsync(It.IsAny<HealthCheckRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Check_ReturnFailedStatus_WhenExternalServiceUnavailable()
    {
        // Arrange
        var ct = CancellationToken.None;
        var expected = new HealthCheckResult
        {
            CheckedOn = default,
            Status = HealthCheckResult.CheckStatus.Failed
        };

        _healthClientMock.Setup(c => c.CheckAsync(It.IsAny<HealthCheckRequest>(), It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Throws(new Grpc.Core.RpcException(status: new Status(StatusCode.Unavailable, "")));
        
        // Act
        var actual = await _sut.Check(true, ct);

        // Assert

        Assert.Equal(expected.Status, actual.Status);
        
        _healthClientMock.Verify(
            c => c.CheckAsync(It.IsAny<HealthCheckRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}