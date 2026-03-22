FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar proyectos y restaurar dependencias
COPY ["API/BarberiaApi.csproj", "API/"]
COPY ["Domain/BarberiaApi.Domain.csproj", "Domain/"]
COPY ["Application/BarberiaApi.Application.csproj", "Application/"]
COPY ["Infrastructure/BarberiaApi.Infrastructure.csproj", "Infrastructure/"]

RUN dotnet restore "API/BarberiaApi.csproj"

# Copiar el resto del código y compilar
COPY . .
WORKDIR "/src/API"
RUN dotnet build "BarberiaApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BarberiaApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Etapa final de ejecución
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENTRYPOINT ["dotnet", "BarberiaApi.dll"]
