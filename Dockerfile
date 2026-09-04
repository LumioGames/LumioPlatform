FROM node:22-alpine AS web
WORKDIR /src
COPY web/package.json web/pnpm-lock.yaml web/
RUN corepack enable && pnpm install --frozen-lockfile
COPY web/ web/
COPY src/ src/
RUN pnpm -C web build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
COPY --from=web /src/src/Lumio.Platform.App/wwwroot ./src/Lumio.Platform.App/wwwroot
RUN dotnet restore build.proj --locked-mode
RUN dotnet publish src/Lumio.Platform.App/Lumio.Platform.App.csproj -c Release --no-restore -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /out .
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENTRYPOINT ["dotnet", "lumio-platform.dll"]
