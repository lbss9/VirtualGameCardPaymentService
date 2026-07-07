using System.Text.Json;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Options;
using VirtualGameCard.PaymentService.Contracts.Messages;
using VirtualGameCard.PaymentService.Worker.Options;

namespace VirtualGameCard.PaymentService.Worker.Consumers;

public sealed class PaymentRequestedConsumer(
    IOptions<SqsOptions> options,
    ILogger<PaymentRequestedConsumer> logger
) : BackgroundService
{
    private readonly SqsOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("SQS desativado. PaymentService não iniciou consumo.");
            return;
        }

        if (
            string.IsNullOrWhiteSpace(_options.PaymentRequestedQueueUrl)
            || string.IsNullOrWhiteSpace(_options.PaymentApprovedQueueUrl)
        )
            throw new InvalidOperationException(
                "Configure Sqs:PaymentRequestedQueueUrl e Sqs:PaymentApprovedQueueUrl."
            );

        using var sqs = CreateSqsClient();

        logger.LogInformation("PaymentService escutando SQS: {QueueUrl}", _options.PaymentRequestedQueueUrl);

        while (!stoppingToken.IsCancellationRequested)
        {
            var response = await sqs.ReceiveMessageAsync(
                new ReceiveMessageRequest
                {
                    QueueUrl = _options.PaymentRequestedQueueUrl,
                    MaxNumberOfMessages = _options.MaxNumberOfMessages,
                    WaitTimeSeconds = _options.WaitTimeSeconds,
                    VisibilityTimeout = _options.VisibilityTimeoutSeconds,
                    MessageAttributeNames = ["All"],
                },
                stoppingToken
            );

            foreach (var sqsMessage in response.Messages ?? [])
            {
                await ProcessMessageAsync(sqs, sqsMessage, stoppingToken);
            }
        }
    }

    private async Task ProcessMessageAsync(
        IAmazonSQS sqs,
        Message sqsMessage,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var message = JsonSerializer.Deserialize<PaymentRequestedMessage>(
                sqsMessage.Body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            if (message is null || string.IsNullOrWhiteSpace(message.IdempotencyKey))
            {
                logger.LogWarning("Mensagem de pagamento inválida recebida.");
                await DeleteMessageAsync(sqs, sqsMessage, cancellationToken);
                return;
            }

            logger.LogInformation(
                "Pagamento recebido. PurchaseId: {PurchaseId}, UserId: {UserId}, Valor: {AmountInCents}, IdempotencyKey: {IdempotencyKey}",
                message.PurchaseId,
                message.UserId,
                message.AmountInCents,
                message.IdempotencyKey
            );

            await SimulatePaymentProcessingAsync(cancellationToken);

            var approved = new PaymentApprovedMessage(
                message.PurchaseId,
                Guid.NewGuid(),
                message.IdempotencyKey,
                DateTime.UtcNow
            );

            await sqs.SendMessageAsync(
                new SendMessageRequest
                {
                    QueueUrl = _options.PaymentApprovedQueueUrl,
                    MessageBody = JsonSerializer.Serialize(approved),
                    MessageAttributes = new Dictionary<string, MessageAttributeValue>
                    {
                        ["messageType"] = new()
                        {
                            DataType = "String",
                            StringValue = nameof(PaymentApprovedMessage),
                        },
                        ["idempotencyKey"] = new()
                        {
                            DataType = "String",
                            StringValue = approved.IdempotencyKey,
                        },
                    },
                },
                cancellationToken
            );

            await DeleteMessageAsync(sqs, sqsMessage, cancellationToken);

            logger.LogInformation(
                "Pagamento aprovado e publicado. PurchaseId: {PurchaseId}, PaymentId: {PaymentId}, IdempotencyKey: {IdempotencyKey}",
                approved.PurchaseId,
                approved.PaymentId,
                approved.IdempotencyKey
            );
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Erro ao processar pagamento via SQS.");
        }
    }

    private IAmazonSQS CreateSqsClient()
    {
        var region = RegionEndpoint.GetBySystemName(_options.Region);
        return new AmazonSQSClient(region);
    }

    private async Task SimulatePaymentProcessingAsync(CancellationToken cancellationToken) =>
        await Task.Delay(TimeSpan.FromSeconds(_options.PaymentSimulationDelaySeconds), cancellationToken);

    private Task DeleteMessageAsync(
        IAmazonSQS sqs,
        Message message,
        CancellationToken cancellationToken
    ) =>
        sqs.DeleteMessageAsync(
            _options.PaymentRequestedQueueUrl,
            message.ReceiptHandle,
            cancellationToken
        );
}
