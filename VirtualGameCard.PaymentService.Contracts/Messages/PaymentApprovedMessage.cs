namespace VirtualGameCard.PaymentService.Contracts.Messages;

public sealed record PaymentApprovedMessage(
    Guid PurchaseId,
    Guid PaymentId,
    string IdempotencyKey,
    DateTime ApprovedAtUtc
);
