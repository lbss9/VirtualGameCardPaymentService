using VirtualGameCard.PaymentService.Worker.Consumers;
using VirtualGameCard.PaymentService.Worker.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<SqsOptions>(builder.Configuration.GetSection("Sqs"));

builder.Services.AddHostedService<PaymentRequestedConsumer>();

var app = builder.Build();

app.MapGet("/healthz", () => Results.Ok(new { status = "ok", service = "payment-service" }));

app.Run();
