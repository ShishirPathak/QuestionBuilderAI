using Microsoft.AspNetCore.Mvc;
using QuestionBuilderAI.Api.Services;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System;
using QuestionBuilderAI.Api.Models;

namespace QuestionBuilderAI.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UploadsController : ControllerBase
    {
        private readonly JobStore _jobStore;
        private readonly QueueClientService _queue;

        public UploadsController(JobStore jobStore, QueueClientService queue)
        {
            _jobStore = jobStore;
            _queue = queue;
        }

        [HttpPost]
        [RequestSizeLimit(200_000_000)] // 200 MB example
        public async Task<IActionResult> Upload()
        {
            if (Request.Form.Files.Count == 0)
                return BadRequest(new { error = "no file provided" });

            var file = Request.Form.Files[0];
            string ownerId = Request.Form["ownerId"];

            // Compute SHA256 streaming
            string fileHash;
            using (var sha = SHA256.Create())
            using (var stream = file.OpenReadStream())
            {
                var buffer = new byte[81920];
                int read;
                while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    sha.TransformBlock(buffer, 0, read, null, 0);
                }
                sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                fileHash = BitConverter.ToString(sha.Hash).Replace("-", "").ToLowerInvariant();
            }

            // Idempotency check
            var existing = await _jobStore.FindByHashAsync(fileHash);
            if (existing != null)
            {
                return Accepted(new
                {
                    jobId = existing.JobId,
                    statusUrl = $"/api/jobs/{existing.JobId}"
                });
            }

            // NOTE: in real impl upload file to blob/storage and set filePath
            var filePath = $"uploads/{Guid.NewGuid()}-{file.FileName}";

            var job = new Job
            {
                JobId = Guid.NewGuid().ToString(),
                OwnerId = ownerId,
                FileHash = fileHash,
                FilePath = filePath,
                Status = "queued",
                CreatedAt = DateTime.UtcNow
            };

            await _jobStore.InsertAsync(job);

            // Enqueue
            await _queue.EnqueueAsync(new { jobId = job.JobId, filePath = job.FilePath, fileHash = job.FileHash });

            return Accepted(new { jobId = job.JobId, statusUrl = $"/api/jobs/{job.JobId}" });
        }

        [HttpGet("/api/jobs/{jobId}")]
        public async Task<IActionResult> GetJob(string jobId)
        {
            var job = await _jobStore.GetByIdAsync(jobId);
            if (job == null) return NotFound();
            return Ok(new
            {
                jobId = job.JobId,
                status = job.Status,
                attempts = job.Attempts,
                resultUrl = job.ResultPath,
                error = job.Error,
                createdAt = job.CreatedAt,
                startedAt = job.StartedAt,
                finishedAt = job.FinishedAt
            });
        }
    }
}