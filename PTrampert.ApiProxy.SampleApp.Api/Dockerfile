#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=80
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["PTrampert.ApiProxy.SampleApp.Api/PTrampert.ApiProxy.SampleApp.Api.csproj", "PTrampert.ApiProxy.SampleApp.Api/"]
RUN dotnet restore "PTrampert.ApiProxy.SampleApp.Api/PTrampert.ApiProxy.SampleApp.Api.csproj"
COPY . .
WORKDIR "/src/PTrampert.ApiProxy.SampleApp.Api"
RUN dotnet build "PTrampert.ApiProxy.SampleApp.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PTrampert.ApiProxy.SampleApp.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PTrampert.ApiProxy.SampleApp.Api.dll"]