using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using VirtualGameCard.PaymentService.Contracts.Messages;
using VirtualGameCard.PaymentService.Worker.Options;

namespace VirtualGameCard.PaymentService.Worker.Consumers;

public sealed class PaymentRequestedConsumer(
    IOptions<RabbitMqOptions> options,
    ILogger<PaymentRequestedConsumer> logger
) : BackgroundService
{
    private readonly RabbitMqOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = CreateConnectionFactory();

        await using var connection = await factory.CreateConnectionAsync(stoppingToken);

        await using var channel = await connection.CreateChannelAsync(
            cancellationToken: stoppingToken
        );

        await channel.ExchangeDeclareAsync(
            exchange: _options.Exchange,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: stoppingToken
        );

        await channel.QueueDeclareAsync(
            queue: _options.PaymentRequestedQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken
        );

        await channel.QueueBindAsync(
            queue: _options.PaymentRequestedQueue,
            exchange: _options.Exchange,
            routingKey: _options.PaymentRequestedRoutingKey,
            cancellationToken: stoppingToken
        );

        await channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: 1,
            global: false,
            cancellationToken: stoppingToken
        );

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(eventArgs.Body.ToArray());

                var message = JsonSerializer.Deserialize<PaymentRequestedMessage>(
                    json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                if (message is null)
                {
                    logger.LogWarning("Mensagem de pagamento inválida recebida.");
                    await channel.BasicNackAsync(
                        eventArgs.DeliveryTag,
                        multiple: false,
                        requeue: false
                    );
                    return;
                }

                if (string.IsNullOrWhiteSpace(message.IdempotencyKey))
                {
                    logger.LogWarning(
                        "Mensagem de pagamento sem chave de idempotência. PurchaseId: {PurchaseId}",
                        message.PurchaseId
                    );

                    await channel.BasicNackAsync(
                        eventArgs.DeliveryTag,
                        multiple: false,
                        requeue: false
                    );

                    return;
                }

                logger.LogInformation(
                    "Pagamento recebido. PurchaseId: {PurchaseId}, UserId: {UserId}, Valor: {AmountInCents}, IdempotencyKey: {IdempotencyKey}",
                    message.PurchaseId,
                    message.UserId,
                    message.AmountInCents,
                    message.IdempotencyKey
                );

                await SimulatePaymentProcessingAsync(message, stoppingToken);

                await channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false);

                logger.LogInformation(
                    "Pagamento processado com sucesso. PurchaseId: {PurchaseId}, IdempotencyKey: {IdempotencyKey}",
                    message.PurchaseId,
                    message.IdempotencyKey
                );
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Erro ao processar mensagem de pagamento.");

                await channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: true);
            }
        };

        await channel.BasicConsumeAsync(
            queue: _options.PaymentRequestedQueue,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken
        );

        logger.LogInformation(
            "PaymentService escutando fila {Queue}.",
            _options.PaymentRequestedQueue
        );

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private ConnectionFactory CreateConnectionFactory()
    {
        if (!string.IsNullOrWhiteSpace(_options.Uri))
        {
            return new ConnectionFactory { Uri = new Uri(_options.Uri) };
        }

        return new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
        };
    }

    private static async Task SimulatePaymentProcessingAsync(
        PaymentRequestedMessage message,
        CancellationToken cancellationToken
    )
    {
        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
    }
}
