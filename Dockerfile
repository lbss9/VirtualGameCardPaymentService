FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY VirtualGameCardPaymentService.slnx ./
COPY VirtualGameCard.PaymentService.Contracts/VirtualGameCard.PaymentService.Contracts.csproj VirtualGameCard.PaymentService.Contracts/
COPY VirtualGameCard.PaymentService.Worker/VirtualGameCard.PaymentService.Worker.csproj VirtualGameCard.PaymentService.Worker/
RUN dotnet restore VirtualGameCard.PaymentService.Worker/VirtualGameCard.PaymentService.Worker.csproj

COPY . .
RUN dotnet publish VirtualGameCard.PaymentService.Worker/VirtualGameCard.PaymentService.Worker.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV DOTNET_ENVIRONMENT=Production
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "VirtualGameCard.PaymentService.Worker.dll"]
