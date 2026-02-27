using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace QuestionBuilderAI.Api.Models
{
    public class Job
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public string JobId { get; set; } = Guid.NewGuid().ToString();

        public string OwnerId { get; set; }
        public string FileHash { get; set; }
        public string FilePath { get; set; } // placeholder blob path
        public string Status { get; set; } = "queued"; // queued|processing|done|failed
        public int Attempts { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public DateTime? FinishedAt { get; set; }
        public string ResultPath { get; set; }
        public string Error { get; set; }
    }
}