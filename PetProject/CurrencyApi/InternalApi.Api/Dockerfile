FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["InternalApi/InternalApi.csproj", "InternalApi/"]
RUN dotnet restore "InternalApi/InternalApi.csproj"
COPY . .
WORKDIR "/src/InternalApi"
RUN dotnet build "InternalApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "InternalApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "InternalApi.dll"]
