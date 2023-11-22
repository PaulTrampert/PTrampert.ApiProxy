#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=80
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["PTrampert.ApiProxy.SampleApp/PTrampert.ApiProxy.SampleApp.csproj", "PTrampert.ApiProxy.SampleApp/"]
RUN dotnet restore "PTrampert.ApiProxy.SampleApp/PTrampert.ApiProxy.SampleApp.csproj"
COPY . .
WORKDIR "/src/PTrampert.ApiProxy.SampleApp"
RUN dotnet build "PTrampert.ApiProxy.SampleApp.csproj" -c Release -o /app/build

FROM node:lts AS node-build
WORKDIR /src
COPY ["PTrampert.ApiProxy.SampleApp/sample-app/package.json", "PTrampert.ApiProxy.SampleApp/sample-app/package-lock.json", "./"]
RUN npm install
COPY PTrampert.ApiProxy.SampleApp/sample-app/ ./
RUN npm run build

FROM build AS publish
RUN dotnet publish "PTrampert.ApiProxy.SampleApp.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=node-build /src/build/ ./wwwroot/
ENTRYPOINT ["dotnet", "PTrampert.ApiProxy.SampleApp.dll"]