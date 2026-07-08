using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Amazon.SQS;
using Amazon.SQS.Model;
using VirtualGameCard.PaymentService.Contracts.Messages;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace VirtualGameCard.PaymentService.Lambda;

public sealed class Function
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IAmazonSQS _sqs;
    private readonly string _paymentApprovedQueueUrl;

    public Function()
        : this(
            new AmazonSQSClient(),
            Environment.GetEnvironmentVariable("PAYMENT_APPROVED_QUEUE_URL") ?? string.Empty
        )
    {
    }

    public Function(IAmazonSQS sqs, string paymentApprovedQueueUrl)
    {
        _sqs = sqs;
        _paymentApprovedQueueUrl = paymentApprovedQueueUrl;
    }

    public async Task<SQSBatchResponse> FunctionHandler(SQSEvent sqsEvent, ILambdaContext context)
    {
        if (string.IsNullOrWhiteSpace(_paymentApprovedQueueUrl))
            throw new InvalidOperationException("Configure PAYMENT_APPROVED_QUEUE_URL.");

        var failures = new List<SQSBatchResponse.BatchItemFailure>();

        foreach (var record in sqsEvent.Records ?? [])
        {
            try
            {
                await ProcessRecordAsync(record, context);
            }
            catch (Exception exception)
            {
                context.Logger.LogError(
                    exception,
                    "Erro ao processar pagamento. MessageId: {0}",
                    record.MessageId
                );

                failures.Add(new SQSBatchResponse.BatchItemFailure
                {
                    ItemIdentifier = record.MessageId,
                });
            }
        }

        return new SQSBatchResponse(failures);
    }

    private async Task ProcessRecordAsync(SQSEvent.SQSMessage record, ILambdaContext context)
    {
        var requested = JsonSerializer.Deserialize<PaymentRequestedMessage>(
            record.Body,
            JsonOptions
        );

        if (requested is null || string.IsNullOrWhiteSpace(requested.IdempotencyKey))
        {
            context.Logger.LogWarning("Mensagem PaymentRequested inválida descartada.");
            return;
        }

        context.Logger.LogInformation(
            "Processando pagamento. PurchaseId: {0}, UserId: {1}, AmountInCents: {2}, IdempotencyKey: {3}",
            requested.PurchaseId,
            requested.UserId,
            requested.AmountInCents,
            requested.IdempotencyKey
        );

        var approved = new PaymentApprovedMessage(
            requested.PurchaseId,
            Guid.NewGuid(),
            requested.IdempotencyKey,
            DateTime.UtcNow
        );

        await _sqs.SendMessageAsync(
            new SendMessageRequest
            {
                QueueUrl = _paymentApprovedQueueUrl,
                MessageBody = JsonSerializer.Serialize(approved, JsonOptions),
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
            }
        );

        context.Logger.LogInformation(
            "Pagamento aprovado publicado. PurchaseId: {0}, PaymentId: {1}, IdempotencyKey: {2}",
            approved.PurchaseId,
            approved.PaymentId,
            approved.IdempotencyKey
        );
    }
}
