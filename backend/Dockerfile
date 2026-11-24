# Stage 1: Build & Tests
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia arquivos de projeto para otimização de cache do restore
COPY NeoAgenda.sln ./
COPY src/NeoAgenda.Domain/NeoAgenda.Domain.csproj src/NeoAgenda.Domain/
COPY src/NeoAgenda.Application/NeoAgenda.Application.csproj src/NeoAgenda.Application/
COPY src/NeoAgenda.Infrastructure/NeoAgenda.Infrastructure.csproj src/NeoAgenda.Infrastructure/
COPY src/NeoAgenda.Api/NeoAgenda.Api.csproj src/NeoAgenda.Api/
COPY tests/NeoAgenda.UnitTests/NeoAgenda.UnitTests.csproj tests/NeoAgenda.UnitTests/
COPY tests/NeoAgenda.IntegrationTests/NeoAgenda.IntegrationTests.csproj tests/NeoAgenda.IntegrationTests/

RUN dotnet restore NeoAgenda.sln

# Copia todo o código fonte
COPY . .

# Compilação e execução da suíte de testes automatizados
RUN dotnet build NeoAgenda.sln --configuration Release --no-restore
RUN dotnet test NeoAgenda.sln --configuration Release --no-build --verbosity normal

# Publicação do executável da API
RUN dotnet publish src/NeoAgenda.Api/NeoAgenda.Api.csproj --configuration Release --no-build -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Instala curl para healthcheck
USER root
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

# Cria diretório de persistência do banco
RUN mkdir -p /app/data && chmod 777 /app/data

COPY --from=build /app/publish .

EXPOSE 5000

ENTRYPOINT ["dotnet", "NeoAgenda.Api.dll"]
