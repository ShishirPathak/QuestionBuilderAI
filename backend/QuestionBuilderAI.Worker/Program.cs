using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using QuestionBuilderAI.Api.Models;
using System.Text.Json;

var environmentName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
    ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
    ?? "Production";

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false)
    .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables()
    .Build();

string serviceBusConn = configuration["SERVICEBUS_CONN"]
    ?? throw new InvalidOperationException("Missing configuration value: SERVICEBUS_CONN");
string queueName = configuration["SERVICEBUS_QUEUE"]
    ?? throw new InvalidOperationException("Missing configuration value: SERVICEBUS_QUEUE");
string mongoConn = configuration["MONGO_CONN"]
    ?? throw new InvalidOperationException("Missing configuration value: MONGO_CONN");

var mongo = new MongoClient(mongoConn);
var db = mongo.GetDatabase("ingestdb");
var jobs = db.GetCollection<Job>("jobs");

var client = new ServiceBusClient(serviceBusConn);
var processor = client.CreateProcessor(queueName, new ServiceBusProcessorOptions
{
    AutoCompleteMessages = false,
    MaxConcurrentCalls = 1
});

processor.ProcessMessageAsync += HandleMessage;
processor.ProcessErrorAsync += ErrorHandler;

Console.WriteLine("Worker listening...");
await processor.StartProcessingAsync();

await Task.Delay(-1);

async Task HandleMessage(ProcessMessageEventArgs args)
{
    var body = args.Message.Body.ToString();
    var payload = JsonSerializer.Deserialize<QueuePayload>(body)!;

    Console.WriteLine($"Received job {payload.jobId}");

    // ATOMIC CLAIM
    var update = Builders<Job>.Update
        .Set(j => j.Status, "processing")
        .Set(j => j.StartedAt, DateTime.UtcNow);

    var job = await jobs.FindOneAndUpdateAsync(
        j => j.JobId == payload.jobId && j.Status == "queued",
        update,
        new FindOneAndUpdateOptions<Job> { ReturnDocument = ReturnDocument.After });

    if (job == null)
    {
        Console.WriteLine("Already claimed by another worker");
        await args.CompleteMessageAsync(args.Message);
        return;
    }

    // FAKE PROCESSING (for now)
    await Task.Delay(3000);

    await jobs.UpdateOneAsync(
        j => j.JobId == payload.jobId,
        Builders<Job>.Update
            .Set(j => j.Status, "done")
            .Set(j => j.CompletedAt, DateTime.UtcNow)
    );

    await args.CompleteMessageAsync(args.Message);
    Console.WriteLine($"Completed {payload.jobId}");
}

Task ErrorHandler(ProcessErrorEventArgs args)
{
    Console.WriteLine(args.Exception);
    return Task.CompletedTask;
}

record QueuePayload(string jobId, string filePath, string fileHash);