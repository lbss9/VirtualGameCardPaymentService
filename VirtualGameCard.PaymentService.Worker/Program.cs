using VirtualGameCard.PaymentService.Worker.Consumers;
using VirtualGameCard.PaymentService.Worker.Options;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));

builder.Services.AddHostedService<PaymentRequestedConsumer>();

var host = builder.Build();

host.Run();
