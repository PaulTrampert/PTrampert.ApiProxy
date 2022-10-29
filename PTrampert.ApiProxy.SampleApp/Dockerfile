#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM node:lts AS build-npm
WORKDIR /src
COPY PTrampert.ApiProxy.SampleApp/client-app/package.json ./
RUN npm install
COPY PTrampert.ApiProxy.SampleApp/client-app/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["PTrampert.ApiProxy.SampleApp/PTrampert.ApiProxy.SampleApp.csproj", "PTrampert.ApiProxy.SampleApp/"]
RUN dotnet restore "PTrampert.ApiProxy.SampleApp/PTrampert.ApiProxy.SampleApp.csproj"
COPY . .
WORKDIR "/src/PTrampert.ApiProxy.SampleApp"
RUN dotnet build "PTrampert.ApiProxy.SampleApp.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PTrampert.ApiProxy.SampleApp.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=build-npm /src/build/ ./wwwroot/
ENTRYPOINT ["dotnet", "PTrampert.ApiProxy.SampleApp.dll"]