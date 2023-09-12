FROM mcr.microsoft.com/dotnet/sdk:7.0 AS prepare-restore
ENV PATH="$PATH:/root/.dotnet/tools"
RUN dotnet tool install --global --no-cache dotnet-subset
WORKDIR ./src
COPY ./PublicApi .
RUN dotnet subset restore ./PublicApi.csproj --root-directory /src --output restore_subset/

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR ./src
COPY --from=prepare-restore /src/restore_subset/ .
RUN dotnet restore "PublicApi.csproj"

COPY ./PublicApi .
RUN dotnet publish "PublicApi.csproj" -c Release -o /app/public /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS runtime
WORKDIR /app
COPY --from=build /app/public .
EXPOSE 5166
ENTRYPOINT ["dotnet", "PublicApi.dll"]