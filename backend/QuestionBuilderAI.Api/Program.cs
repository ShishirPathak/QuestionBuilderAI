using QuestionBuilderAI.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Config from env or appsettings
string mongoConn = builder.Configuration["MONGO_CONN"] ?? "mongodb://localhost:27017";
string serviceBusConn = builder.Configuration["SERVICEBUS_CONN"] ?? "";
string serviceBusQueue = builder.Configuration["SERVICEBUS_QUEUE"] ?? "ingest-queue";


// Controllers
builder.Services.AddControllers();

builder.Services.AddSingleton(new JobStore(mongoConn));
builder.Services.AddSingleton(new QueueClientService(serviceBusConn, serviceBusQueue));

// PORT for Render
var port = Environment.GetEnvironmentVariable("PORT") ?? "5196";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");



// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Our internal services
builder.Services.AddSingleton<QuestionPaperService>();

// OCR client
builder.Services.AddHttpClient<OcrClient>();

// CORS: allow anywhere for now
var corsPolicyName = "AllowFrontend";
builder.Services.AddCors(options =>
{
    options.AddPolicy(corsPolicyName, policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// Swagger UI for all environments
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "QuestionBuilderAI API v1");
    c.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();
app.UseCors(corsPolicyName);

app.MapControllers();

app.Run();
