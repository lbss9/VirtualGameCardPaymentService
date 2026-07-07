# 🎮 VirtualGameCard Payment Service

Microserviço de pagamentos do ecossistema **VirtualGameCard**, criado para
processar compras de gift cards de forma assíncrona usando **RabbitMQ**.

> Este projeto é uma extensão separada do backend principal
> [`VirtualGameCard`](https://github.com/lbss9/VirtualGameCard).

---

## ✨ Objetivo

O objetivo deste serviço é praticar uma arquitetura mais próxima do mercado,
separando o fluxo de pagamento em um processo independente.

Em vez do backend principal processar tudo diretamente, ele cria uma compra
pendente e publica uma mensagem na fila. O Payment Service consome essa
mensagem e executa o processamento do pagamento.

```txt
Frontend
   ↓
VirtualGameCard API
   ↓ cria compra Pending
RabbitMQ
   ↓
Payment Service
   ↓ processa pagamento
```

---

## 🧠 Conceitos praticados

- Worker Service com .NET
- RabbitMQ
- Mensageria assíncrona
- Microserviço separado
- Contratos compartilhados de mensagem
- Idempotência via `IdempotencyKey`
- Docker
- Deploy preparado para Render
- Configuração segura por variáveis de ambiente

---

## 🧩 Estrutura

```txt
VirtualGameCardPaymentService/
├── VirtualGameCard.PaymentService.Contracts/
│   ├── Messages/
│   │   └── PaymentRequestedMessage.cs
│   └── Queues/
│       └── PaymentQueueNames.cs
├── VirtualGameCard.PaymentService.Worker/
│   ├── Consumers/
│   │   └── PaymentRequestedConsumer.cs
│   ├── Options/
│   │   └── RabbitMqOptions.cs
│   ├── Infrastructure/
│   ├── Services/
│   └── Program.cs
├── docker-compose.yml
├── Dockerfile
├── render.yaml.example
└── README.md
```

---

## 📦 Mensagem consumida

O serviço consome mensagens do tipo:

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

A `IdempotencyKey` é importante porque mensagens podem ser entregues mais de
uma vez em sistemas distribuídos. Hoje ela já é validada e logada pelo worker.
Em uma próxima etapa, o ideal é persistir os pagamentos processados para impedir
reprocessamento definitivo.

---

## 🐇 RabbitMQ local

Suba o RabbitMQ local:

```bash
docker compose up -d
```

Painel de gerenciamento:

```txt
http://localhost:15672
```

Credenciais locais:

```txt
Usuário: guest
Senha: guest
```

Essas credenciais são apenas para desenvolvimento local.

---

## ▶️ Rodar o worker localmente

```bash
dotnet run --project VirtualGameCard.PaymentService.Worker
```

Se estiver tudo certo, o log deve mostrar algo parecido com:

```txt
PaymentService escutando fila payments.requested.
```

---

## ⚙️ Variáveis de ambiente

Configurações suportadas:

| Variável | Uso | Sensível |
| --- | --- | --- |
| `RabbitMq__Uri` | Connection string completa AMQP/AMQPS | Sim |
| `RabbitMq__HostName` | Host local ou privado do RabbitMQ | Não necessariamente |
| `RabbitMq__Port` | Porta RabbitMQ, normalmente `5672` | Não |
| `RabbitMq__UserName` | Usuário RabbitMQ | Sim |
| `RabbitMq__Password` | Senha RabbitMQ | Sim |
| `RabbitMq__Exchange` | Exchange usada pelo domínio de pagamentos | Não |
| `RabbitMq__PaymentRequestedQueue` | Fila de pagamento solicitado | Não |
| `RabbitMq__PaymentRequestedRoutingKey` | Routing key da mensagem | Não |

Em produção, prefira:

```txt
RabbitMq__Uri=amqps://...
```

E configure essa variável diretamente no painel do provedor, nunca no código.

---

## 🔐 Segurança

Este repositório não deve conter:

- connection string real do RabbitMQ;
- senha de banco;
- token do Render;
- token do GitHub;
- segredo JWT;
- webhook secret;
- qualquer credencial de produção.

No Render, mantenha segredos como `sync: false` no Blueprint ou configure
diretamente pelo Dashboard.

Exemplo seguro:

```yaml
- key: RabbitMq__Uri
  sync: false
```

---

## 🚀 Deploy no Render

Este serviço é um **Background Worker**.

Importante:

- Worker não expõe URL pública.
- Worker precisa de um RabbitMQ acessível pela internet.
- O RabbitMQ local do Docker não funciona no Render.
- Background Workers no Render podem exigir plano pago.

Antes de subir, crie um RabbitMQ online, por exemplo:

- CloudAMQP
- LavinMQ gerenciado
- RabbitMQ em outro provedor

Depois configure no Render:

```txt
DOTNET_ENVIRONMENT=Production
RabbitMq__Uri=amqps://...
RabbitMq__Exchange=virtualgamecard.payments
RabbitMq__PaymentRequestedQueue=payments.requested
RabbitMq__PaymentRequestedRoutingKey=payment.requested
```

Use o arquivo [render.yaml.example](./render.yaml.example) como base.

---

## 🐳 Build Docker local

```bash
docker build -t virtualgamecard-payment-service:local .
```

---

## 🧪 Validação rápida

```bash
dotnet build
docker compose config --quiet
```

---

## 🗺️ Próximos passos

- Persistir pagamentos processados.
- Criar tabela `ProcessedPayments`.
- Garantir idempotência real no Payment Service.
- Publicar evento de pagamento aprovado.
- Fazer a API principal consumir confirmação do pagamento.
- Adicionar testes de integração com RabbitMQ.
- Adicionar observabilidade com Prometheus/Grafana.

---

## 📄 Licença

Este projeto é proprietário. O uso, cópia, modificação, distribuição ou
comercialização sem autorização prévia não é permitido.
