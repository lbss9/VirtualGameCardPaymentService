# 🎮 VirtualGameCard Payment Service

Serviço separado de pagamentos do ecossistema **VirtualGameCard**.

Este projeto existe para praticar uma arquitetura orientada a eventos usando:

- .NET Worker Service
- RabbitMQ
- Contratos de mensagens
- Processamento assíncrono de pagamentos
- Separação real entre backend principal e serviço de pagamento

## Estrutura inicial

```txt
VirtualGameCardPaymentService/
├── VirtualGameCard.PaymentService.Contracts/
│   ├── Messages/
│   └── Queues/
├── VirtualGameCard.PaymentService.Worker/
│   ├── Consumers/
│   ├── Extensions/
│   ├── Infrastructure/
│   ├── Options/
│   └── Services/
└── docker-compose.yml
```

## Rodar RabbitMQ local

```bash
docker compose up -d
```

Painel do RabbitMQ:

- URL: http://localhost:15672
- Usuário: `guest`
- Senha: `guest`

## Rodar o worker

```bash
dotnet run --project VirtualGameCard.PaymentService.Worker
```

## Observação

Este projeto ainda é uma fundação. Os arquivos `.cs` de contratos, consumers,
publishers e serviços serão criados conforme a evolução do estudo.
