# ============================================
# ETAPA 1: BUILD (compilar el código)
# ============================================
# Usamos la imagen con el SDK completo de .NET 10.
# El SDK sabe compilar; esta imagen es pesada pero solo se usa para construir.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiamos SOLO el .csproj primero y restauramos dependencias.
# ¿Por qué solo el .csproj y no todo? Por la caché de Docker: si no cambian
# tus dependencias, Docker reutiliza este paso y no vuelve a descargar todo
# cada vez que cambiás una línea de código. Acelera muchísimo los builds.
COPY LicitApp.Api/*.csproj ./LicitApp.Api/
RUN dotnet restore LicitApp.Api/LicitApp.Api.csproj

# Ahora sí copiamos todo el código fuente y compilamos en modo Release.
COPY LicitApp.Api/. ./LicitApp.Api/
RUN dotnet publish LicitApp.Api/LicitApp.Api.csproj -c Release -o /app

# ============================================
# ETAPA 2: RUNTIME (ejecutar la app compilada)
# ============================================
# Imagen mucho más liviana: solo el runtime de ASP.NET, sin el SDK.
# Esta es la que termina corriendo en producción.
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

# Copiamos los archivos ya compilados desde la etapa de build.
# El "--from=build" es la magia: trae el resultado de la etapa anterior
# sin arrastrar el SDK ni el código fuente.
COPY --from=build /app ./

# Documentamos que la app escucha en el 8080 (puerto interno del contenedor).
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Comando que arranca la app cuando el contenedor se levanta.
ENTRYPOINT ["dotnet", "LicitApp.Api.dll"]