FROM mcr.microsoft.com/dotnet/sdk:7.0 AS prepare-restore
ENV PATH="$PATH:/root/.dotnet/tools"
RUN dotnet tool install --global --no-cache dotnet-subset
WORKDIR ./src
COPY ./InternalApi/ .
RUN dotnet subset restore ./InternalApi.Api/InternalApi.Api.csproj --root-directory /src --output restore_subset/

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR ./src
COPY --from=prepare-restore /src/restore_subset/ .
RUN dotnet restore "InternalApi.Api\InternalApi.Api.csproj"

COPY ./InternalApi/ .
RUN dotnet publish "InternalApi.Api\InternalApi.Api.csproj" -c Release -o /app/internal /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS runtime
WORKDIR /app
COPY --from=build /app/internal .
EXPOSE 5000
EXPOSE 5050
ENTRYPOINT ["dotnet", "InternalApi.Api.dll"]