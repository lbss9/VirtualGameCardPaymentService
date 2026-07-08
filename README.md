# 🎮 VirtualGameCard Payment Service

Microserviço de pagamentos do ecossistema **VirtualGameCard**. Hoje ele roda em
**AWS Lambda**, consome eventos da fila **AWS SQS** e publica a aprovação do
pagamento de volta para o backend principal.

> Extensão separada do backend principal:
> [`VirtualGameCard`](https://github.com/lbss9/VirtualGameCard).

---

## ✨ Fluxo atual

```txt
VirtualGameCard API
   ↓ publica PaymentRequested
SQS: vgc-payment-requested
   ↓ delay nativo de 30s
AWS Lambda: vgc-payment-processor
   ↓ publica PaymentApproved
SQS: vgc-payment-approved
   ↓
VirtualGameCard API
   ↓ marca compra como Approved e libera o código
```

O delay de 30 segundos fica na própria SQS, não em `Thread.Sleep`/`Task.Delay`.
Assim a Lambda só executa quando a mensagem já pode ser processada.

---

## 🧠 Conceitos praticados

- AWS Lambda com runtime `.NET 8`
- AWS SQS como mensageria assíncrona
- Event source mapping SQS → Lambda
- DLQ para mensagens com falha
- Idempotência com `IdempotencyKey`
- Separação por contratos de integração
- Worker legado para testes locais/fallback
- Configuração segura por variáveis de ambiente

---

## 🧩 Estrutura

```txt
VirtualGameCardPaymentService/
├── VirtualGameCard.PaymentService.Contracts/
│   └── Messages/
│       ├── PaymentRequestedMessage.cs
│       └── PaymentApprovedMessage.cs
├── VirtualGameCard.PaymentService.Lambda/
│   ├── Function.cs
│   └── aws-lambda-tools-defaults.json
├── VirtualGameCard.PaymentService.Worker/
│   └── Worker legado/local
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

Publicada pela Lambda:

```csharp
public sealed record PaymentApprovedMessage(
    Guid PurchaseId,
    Guid PaymentId,
    string IdempotencyKey,
    DateTime ApprovedAtUtc
);
```

---

## ⏱️ Delay da simulação

O tempo de espera é feito pela fila:

```txt
vgc-payment-requested
DelaySeconds = 30
```

Isso evita deixar a Lambda executando parada por 30 segundos.

---

## ☁️ Recursos AWS

Recursos usados:

```txt
Lambda: vgc-payment-processor
Role:   vgc-payment-processor-lambda-role

SQS: vgc-payment-requested
SQS: vgc-payment-requested-dlq
SQS: vgc-payment-approved
SQS: vgc-payment-approved-dlq
```

Configuração validada:

- `vgc-payment-requested`: `DelaySeconds=30`
- event source mapping: `vgc-payment-requested` → `vgc-payment-processor`
- batch size: `5`
- partial batch failure: `ReportBatchItemFailures`

---

## ⚙️ Variáveis de ambiente da Lambda

| Variável | Uso | Sensível |
| --- | --- | --- |
| `PAYMENT_APPROVED_QUEUE_URL` | URL da fila `vgc-payment-approved` | Não |

Credenciais AWS não ficam no repositório. Em produção, a Lambda usa IAM Role.

---

## ▶️ Build local

```bash
dotnet build
```

Publicar pacote localmente:

```bash
dotnet publish VirtualGameCard.PaymentService.Lambda \
  -c Release \
  -f net8.0 \
  -o ./publish/lambda
```

---

## 🧪 Validação ponta a ponta

Teste real executado:

```txt
POST /api/cards/purchase
  → status inicial: pending

SQS delay 30s
  → Lambda publicou PaymentApproved

GET /api/purchases
GET /api/purchases/{id}
  → status final: approved
  → code disponível: true
```

---

## 🧯 Sobre o Worker/Render legado

O projeto ainda mantém `VirtualGameCard.PaymentService.Worker` como referência
de aprendizado/local/fallback. Porém, o processamento principal em produção é
feito por Lambda.

Se usar o exemplo do Render, mantenha:

```txt
Sqs__Enabled=false
```

Isso evita consumidor duplicado competindo com a Lambda.

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

---

## 📄 Licença

Este projeto é proprietário. O uso, cópia, modificação, distribuição ou
comercialização sem autorização prévia não é permitido.
