Cadastrar clientes via API e comunicar por mensageria


1. Cadastro de Clientes
2. Proposta de crédito
3. Cartao de crédito


## Como rodar localmente

### Pré-requisitos
- Docker Desktop
- .NET 8 SDK

### 1. Subir a infra
docker-compose up -d

### 2. Aplicar migrations
cd PB.Cliente.Infrastructure
dotnet ef database update --startup-project ../PB.Cliente.Presentation

dotnet ef database update --project PB.Cliente.Infrastructure
                          --startup-project PB.Cliente.Presentation

### 3. Rodar a API
cd PB.Cliente.Presentation
dotnet run