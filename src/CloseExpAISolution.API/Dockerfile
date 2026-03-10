FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["CloseExpAISolution.sln", "."]
COPY ["src/CloseExpAISolution.API/CloseExpAISolution.API.csproj", "src/CloseExpAISolution.API/"]
COPY ["src/CloseExpAISolution.Application/CloseExpAISolution.Application.csproj", "src/CloseExpAISolution.Application/"]
COPY ["src/CloseExpAISolution.Infrastructure/CloseExpAISolution.Infrastructure.csproj", "src/CloseExpAISolution.Infrastructure/"]
COPY ["src/CloseExpAISolution.Domain/CloseExpAISolution.Domain.csproj", "src/CloseExpAISolution.Domain/"]
RUN dotnet restore "src/CloseExpAISolution.API/CloseExpAISolution.API.csproj"
COPY src/ src/
RUN dotnet build "src/CloseExpAISolution.API/CloseExpAISolution.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "src/CloseExpAISolution.API/CloseExpAISolution.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CloseExpAISolution.API.dll"]
