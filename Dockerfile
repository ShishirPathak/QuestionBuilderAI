# ------------ Build stage ------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY backend/QuestionBuilderAI.Api/QuestionBuilderAI.Api.csproj backend/QuestionBuilderAI.Api/
RUN dotnet restore backend/QuestionBuilderAI.Api/QuestionBuilderAI.Api.csproj

# Copy everything
COPY . .

# Publish the API project
WORKDIR /src/backend/QuestionBuilderAI.Api
RUN dotnet publish QuestionBuilderAI.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# ------------ Runtime stage ------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Copy published output
COPY --from=build /app/publish .

# Render sets PORT env var; Program.cs uses it
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT}

ENTRYPOINT ["dotnet", "QuestionBuilderAI.Api.dll"]
