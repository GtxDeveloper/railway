FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["Tringelty_Backend.sln", "./"]
COPY ["Tringelty.Api/Tringelty.Api.csproj", "Tringelty.Api/"]
COPY ["Tringelty.Core/Tringelty.Core.csproj", "Tringelty.Core/"]
COPY ["Tringelty.Infrastructure/Tringelty.Infrastructure.csproj", "Tringelty.Infrastructure/"]

RUN dotnet restore "Tringelty.Api/Tringelty.Api.csproj"


COPY . .


WORKDIR "/src/Tringelty.Api"
RUN dotnet publish "Tringelty.Api.csproj" -c Release -o /app/publish


FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "Tringelty.Api.dll"]