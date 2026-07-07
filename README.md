# 🎮 VirtualGameCard Payment Service

Microserviço de pagamentos do ecossistema **VirtualGameCard**, responsável por
consumir pedidos de pagamento via **AWS SQS**, simular o processamento por 1
minuto e publicar o evento de pagamento aprovado.

> Extensão separada do backend principal:
> [`VirtualGameCard`](https://github.com/lbss9/VirtualGameCard).

---

## ✨ Objetivo

Separar o fluxo de pagamento em um serviço independente, praticando uma
arquitetura orientada a eventos com filas.

```txt
VirtualGameCard API
   ↓ envia PaymentRequested
SQS: vgc-payment-requested
   ↓
Payment Service
   ↓ espera 1 minuto
   ↓ envia PaymentApproved
SQS: vgc-payment-approved
   ↓
VirtualGameCard API
   ↓ marca compra como Approved
```

---

## 🧠 Conceitos praticados

- .NET Worker hospedado dentro de um Web Service
- AWS SQS
- Processamento assíncrono
- Microserviço separado
- Mensagens de integração
- Idempotência com `IdempotencyKey`
- DLQ
- Long polling
- Docker
- Deploy preparado para Render
- Configuração segura por variáveis de ambiente

---

## 🧩 Estrutura

```txt
VirtualGameCardPaymentService/
├── VirtualGameCard.PaymentService.Contracts/
│   └── Messages/
│       ├── PaymentRequestedMessage.cs
│       └── PaymentApprovedMessage.cs
├── VirtualGameCard.PaymentService.Worker/
│   ├── Consumers/
│   │   └── PaymentRequestedConsumer.cs
│   ├── Options/
│   │   └── SqsOptions.cs
│   └── Program.cs
├── Dockerfile
├── render.yaml.example
└── README.md
```

---

## 📦 Mensagens

### PaymentRequested

Recebida da API principal:

```csharp
public sealed record PaymentRequestedMessage(
    Guid PurchaseId,
    Guid UserId,
    long AmountInCents,
    string Platform,
    string PaymentMethod,
    string IdempotencyKey,
    DateTime RequestedAtUtc
);
```

### PaymentApproved

Publicada pelo Payment Service após a simulação:

```csharp
public sealed record PaymentApprovedMessage(
    Guid PurchaseId,
    Guid PaymentId,
    string IdempotencyKey,
    DateTime ApprovedAtUtc
);
```

---

## ⏱️ Simulação de pagamento

Por padrão, o processamento demora **60 segundos**:

```txt
Sqs__PaymentSimulationDelaySeconds=60
```

Isso simula um provedor de pagamento externo levando tempo para confirmar uma
transação.

---

## ☁️ Filas AWS SQS

Filas usadas:

```txt
vgc-payment-requested
vgc-payment-requested-dlq
vgc-payment-approved
vgc-payment-approved-dlq
```

Configurações aplicadas:

- long polling: `20s`
- visibility timeout: `180s`
- DLQ após `5` tentativas

---

## ▶️ Rodar localmente

Antes, garanta que o AWS CLI está autenticado e com região:

```bash
aws sts get-caller-identity
aws configure get region
```

Depois:

```bash
dotnet run --project VirtualGameCard.PaymentService.Worker
```

Health check local:

```txt
http://localhost:5000/healthz
```

---

## ⚙️ Variáveis de ambiente

| Variável | Uso | Sensível |
| --- | --- | --- |
| `Sqs__Enabled` | Liga/desliga o consumo SQS | Não |
| `Sqs__Region` | Região AWS | Não |
| `Sqs__PaymentRequestedQueueUrl` | URL da fila de pedidos | Não |
| `Sqs__PaymentApprovedQueueUrl` | URL da fila de aprovações | Não |
| `Sqs__PaymentSimulationDelaySeconds` | Delay da simulação | Não |
| `AWS_ACCESS_KEY_ID` | Access key do IAM user | Sim |
| `AWS_SECRET_ACCESS_KEY` | Secret key do IAM user | Sim |

Nunca coloque credenciais AWS no repositório.

---

## 🔐 Segurança

Este repositório não deve conter:

- access key AWS;
- secret key AWS;
- token do Render;
- token do GitHub;
- senha de banco;
- segredo JWT;
- qualquer credencial de produção.

No Render, use env vars secretas:

```yaml
- key: AWS_ACCESS_KEY_ID
  sync: false
- key: AWS_SECRET_ACCESS_KEY
  sync: false
```

---

## 🚀 Deploy no Render

Este projeto roda como **Web Service free** com um background consumer interno.

Por quê?

- Render Background Worker não tem plano free.
- Web Service free permite health check.
- O worker roda enquanto o serviço está acordado.

Atenção: serviços free podem dormir quando inativos. Para produção real, use um
worker pago ou outro host de workers.

Use [render.yaml.example](./render.yaml.example) como base.

---

## 🐳 Build Docker

```bash
docker build -t virtualgamecard-payment-service:local .
```

---

## 🧪 Validação

```bash
dotnet build
```

---

## 🗺️ Próximos passos

- Adicionar banco próprio do Payment Service.
- Persistir pagamentos processados.
- Garantir idempotência real no banco do serviço.
- Adicionar testes de integração com SQS.
- Adicionar métricas Prometheus/Grafana.

---

## 📄 Licença

Este projeto é proprietário. O uso, cópia, modificação, distribuição ou
comercialização sem autorização prévia não é permitido.
