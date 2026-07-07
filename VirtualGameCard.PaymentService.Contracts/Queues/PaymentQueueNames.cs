namespace VirtualGameCard.PaymentService.Contracts.Queues;

public static class PaymentQueueNames
{
    public const string Exchange = "virtualgamecard.payments";
    public const string PaymentRequestedQueue = "payments.requested";
    public const string PaymentRequestedRoutingKey = "payment.requested";
}
