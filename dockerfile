
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .


RUN dotnet restore ./StudentManagement.csproj


RUN dotnet publish ./StudentManagement.csproj -c Release -o /app/out


FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/out .
EXPOSE 80


ENTRYPOINT ["dotnet", "StudentManagement.dll"]
