# Imagen base
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Instalar dependencias
COPY *.csproj ./
RUN dotnet restore

# Copiar todos los archivos del proyecto
COPY . ./
RUN dotnet publish -c Release -o ./publish

# "No hace nada":
EXPOSE 8080


ENTRYPOINT ["dotnet","./publish/StatsServer.dll"]
