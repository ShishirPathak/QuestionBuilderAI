# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore
COPY QuestionBuilderAI.Api/QuestionBuilderAI.Api.csproj QuestionBuilderAI.Api/
RUN dotnet restore QuestionBuilderAI.Api/QuestionBuilderAI.Api.csproj

# Copy everything and publish
COPY . .
WORKDIR /src/QuestionBuilderAI.Api
RUN dotnet publish QuestionBuilderAI.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

# Render will set PORT env var; Program.cs already binds to it
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT}

ENTRYPOINT ["dotnet", "QuestionBuilderAI.Api.dll"]
