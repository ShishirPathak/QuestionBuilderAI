using QuestionBuilderAI.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(corsPolicyName);

app.MapControllers();

app.Run();
