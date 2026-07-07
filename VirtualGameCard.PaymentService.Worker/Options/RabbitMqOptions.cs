namespace VirtualGameCard.PaymentService.Worker.Options;

public sealed class RabbitMqOptions
{
    public string HostName { get; init; } = "localhost";
    public int Port { get; init; } = 5672;
    public string UserName { get; init; } = "guest";
    public string Password { get; init; } = "guest";
    public string Exchange { get; init; } = "virtualgamecard.payments";
    public string PaymentRequestedQueue { get; init; } = "payments.requested";
    public string PaymentRequestedRoutingKey { get; init; } = "payment.requested";
}
