#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["test/TestWebApp/TestWebApp.csproj", "test/TestWebApp/"]
RUN dotnet restore "test/TestWebApp/TestWebApp.csproj"
COPY . .
WORKDIR "/src/test/TestWebApp"
RUN dotnet build "TestWebApp.csproj" -c Release -o /app/build -v detailed

FROM build AS publish
RUN dotnet publish "TestWebApp.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TestWebApp.dll"]