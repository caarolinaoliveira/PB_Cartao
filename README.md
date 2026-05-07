# PB.Cartao — MS Emissão de Cartões

Microsserviço responsável pela emissão de cartões de crédito. Consome o evento `CreditoAprovado` do RabbitMQ, emite 1 ou 2 cartões conforme o score da proposta e notifica o cliente por e-mail.

## Tecnologias

- .NET 10
- ASP.NET Core Web API + BackgroundService
- Entity Framework Core + SQL Server
- RabbitMQ.Client 6.8.1
- Clean Architecture + DDD

## Estrutura
```
PB.Cartao/
  PB.Cartao.Domain/                    # Entidades, interfaces
  PB.Cartao.Application/               # Services, interfaces, eventos
  PB.Cartao.Infrastructure/            # EF Core, repositórios, RabbitMQ
  PB.Cartao.Presentation/              # Controllers, middlewares, Program.cs
  Testes/
    PB.Cartao.Application.Tests/       # Testes unitários com xUnit
```

## Regras de emissão

| QuantidadeCartoes (do evento) | Cartões emitidos | Limite cada |
|-------------------------------|------------------|-------------|
| 1 | Cartão #1 | R$ 1.000,00 |
| 2 | Cartão #1 e Cartão #2 | R$ 5.000,00 |

> O número do cartão é gerado de forma aleatória no formato `0000 0000 0000 0000`. Em produção, viria de uma integradora como Mastercard ou Visa.

## Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- MS Cliente e MS Proposta rodando

## Como rodar localmente

### 1. Subir a infraestrutura

Na raiz da solution (onde está o `docker-compose.yml`), sobe o RabbitMQ e o SQL Server:

```bash
docker-compose up -d rabbitmq sqlserver
```

Confirma que os containers estão rodando:

```bash
docker ps
```

Você deve ver:

```
pb_rabbitmq    → portas 5672 e 15672
pb_sqlserver   → porta 1433
```

> Painel do RabbitMQ disponível em http://localhost:15672 com usuário `guest` e senha `guest`

### 2. Configurar o appsettings

No arquivo `PB.Cartao/appsettings.json`, configure as credenciais de acordo com o ambiente:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=PB_Cartoes;User Id=sa;Password=Pb@123456;TrustServerCertificate=True"
  },
  "RabbitMQ": {
    "Host": "localhost",
    "User": "guest",
    "Password": "guest"
  }
}
```

> Para usar o Azure SQL Server, substitua a connection string mantendo `Database=PB_Cartoes`.

### 3. Criar e aplicar migrations

```bash
cd PB.Cartao

dotnet ef migrations add InitialCreate --project PB.Cartao.Infrastructure  --startup-project PB.Cartao.Presentation --context CartaoDbContext

dotnet ef database update --project PB.Cartao.Infrastructure  --startup-project PB.Cartao.Presentation  --context PBCartaoDbContext
```

> A migration também é aplicada automaticamente na inicialização via `MigrateAsync()` 

### 4. Rodar o serviço

```bash
cd PB.Cartao
dotnet run
```

O consumer ficará aguardando mensagens na fila `credito.aprovado`:

```
[CONSUMER] Aguardando mensagens na fila credito.aprovado...
```

A API estará disponível para consulta de cartões em:

```
http://localhost:5102/swagger/index.html
```

## Infraestrutura Docker

O `docker-compose.yml` sobe dois serviços compartilhados entre todos os microsserviços:

| Container | Imagem | Porta | Uso |
|---|---|---|---|
| pb_rabbitmq | rabbitmq:3-management | 5672 / 15672 | Broker de mensagens + painel web |
| pb_sqlserver | mssql/server:2022 | 1433 | Banco de dados SQL Server |

Os dados são persistidos em volumes Docker (`rabbitmq_data` e `sqlserver_data`) — reiniciar os containers não apaga filas nem bancos.

Comandos úteis:

```bash
# Parar os containers
docker-compose down

# Parar e apagar todos os dados (reset total)
docker-compose down -v

# Ver logs do RabbitMQ
docker logs pb_rabbitmq

# Ver logs do SQL Server
docker logs pb_sqlserver
```

## Fluxo do evento

```
RabbitMQ: credito.aprovado
        ↓
Verifica cartões já emitidos para o ClienteId (idempotência)
        ↓
Quantidade já emitida >= aprovada? → lança ConflictException
        ↓
Loop: emite cartão #1 (e #2 se QuantidadeCartoes = 2)
        ↓
Persiste no banco (PB_Cartoes)
        ↓
[EMAIL] Cartão #N emitido para cliente
        ↓
Publica → RabbitMQ: cartao.emitido
```

## Fluxo completo dos 3 microsserviços

```
[MS Cliente] POST /api/clientes/registrar
        ↓ publica
RabbitMQ: cliente.cadastrado
        ↓ consome
[MS Proposta] calcula score → aprova/nega
        ↓ publica (se aprovado)
RabbitMQ: credito.aprovado
        ↓ consome
[MS Cartão] emite 1 ou 2 cartões → notifica por e-mail
        ↓ publica
RabbitMQ: cartao.emitido
```

## Testes

```bash
cd PB.Cartao/Testes/PB.Cartao.Application.Tests
dotnet test
```

Cenários cobertos:

- Um cartão aprovado emite exatamente um cartão, um e-mail e um evento
- Dois cartões aprovados emite dois cartões com sequenciais distintos
- Cartões já emitidos lança ConflictException sem efeitos colaterais

## Resiliência

O consumer implementa retry automático via `BasicNack(requeue: true)` — em caso de falha no processamento, a mensagem é devolvida à fila e reprocessada automaticamente.

## Decisões arquiteturais

- **Idempotência com retomada**: verifica quantos cartões já foram emitidos para o cliente. Se emitiu 1 de 2 e falhou, na próxima tentativa emite apenas o segundo — sem duplicar o primeiro.
- **Evento por cartão**: publica um `CartaoEmitidoEvent` individual para cada cartão emitido, permitindo rastreabilidade granular.
- **Índice composto `(ClienteId, Sequencial)`**: garante no banco que um cliente nunca tenha dois cartões com o mesmo sequencial, mesmo em cenários de concorrência.
- **EmailService fictício**: implementação via `ILogger` simulando envio de e-mail. Em produção, bastaria trocar a implementação por SendGrid ou MailKit sem alterar nenhuma regra de negócio.
- **ConflictException**: diferente do MS Proposta que ignora silenciosamente, o MS Cartão lança exceção quando a quantidade já emitida é igual ou superior à aprovada — comportamento explícito e rastreável.