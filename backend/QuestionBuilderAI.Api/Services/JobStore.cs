using System;
using System.Threading.Tasks;
using MongoDB.Driver;
using QuestionBuilderAI.Api.Models;

namespace QuestionBuilderAI.Api.Services
{
    public class JobStore
    {
        private readonly IMongoCollection<Job> _jobs;
        public JobStore(string mongoConnectionString, string databaseName = "ingestdb", string collectionName = "jobs")
        {
            var client = new MongoClient(mongoConnectionString);
            var db = client.GetDatabase(databaseName);
            _jobs = db.GetCollection<Job>(collectionName);

            // Ensure index on fileHash
            var idx = Builders<Job>.IndexKeys.Ascending(j => j.FileHash);
            _jobs.Indexes.CreateOne(new CreateIndexModel<Job>(idx));
        }

        public async Task<Job> FindByHashAsync(string fileHash)
        {
            var filter = Builders<Job>.Filter.Eq(j => j.FileHash, fileHash) &
                         Builders<Job>.Filter.Ne(j => j.Status, "failed");
            return await _jobs.Find(filter).SortByDescending(j => j.CreatedAt).FirstOrDefaultAsync();
        }

        public async Task InsertAsync(Job job)
        {
            await _jobs.InsertOneAsync(job);
        }

        public async Task<Job> GetByIdAsync(string jobId)
        {
            return await _jobs.Find(j => j.JobId == jobId).FirstOrDefaultAsync();
        }

        public async Task<bool> TryClaimJobAsync(string jobId)
        {
            var filter = Builders<Job>.Filter.And(
                Builders<Job>.Filter.Eq(j => j.JobId, jobId),
                Builders<Job>.Filter.Eq(j => j.Status, "queued")
            );
            var update = Builders<Job>.Update
                .Set(j => j.Status, "processing")
                .Set(j => j.StartedAt, DateTime.UtcNow)
                .Inc(j => j.Attempts, 1);

            var result = await _jobs.UpdateOneAsync(filter, update);
            return result.ModifiedCount == 1;
        }

        public async Task MarkDoneAsync(string jobId, string resultPath)
        {
            var update = Builders<Job>.Update
                .Set(j => j.Status, "done")
                .Set(j => j.ResultPath, resultPath)
                .Set(j => j.FinishedAt, DateTime.UtcNow);
            await _jobs.UpdateOneAsync(j => j.JobId == jobId, update);
        }

        public async Task MarkFailedAsync(string jobId, string error)
        {
            var update = Builders<Job>.Update
                .Set(j => j.Status, "failed")
                .Set(j => j.Error, error)
                .Set(j => j.FinishedAt, DateTime.UtcNow);
            await _jobs.UpdateOneAsync(j => j.JobId == jobId, update);
        }
    }
}