using System;

namespace APIProject.Models
{
    public sealed class BackgroundJob
    {
        public Guid Id { get; set; }

        public string Type { get; set; } = string.Empty;

        public string Payload { get; set; } = string.Empty;

        public string Status { get; set; } = JobStatuses.Pending;

        public DateTime CreatedAt { get; set; }

        public DateTime? StartedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        public int RetryCount { get; set; }
        public DateTimeOffset NextAttemptAt { get; set; }

        public string? LastError { get; set; }
        public string? LastErrorType { get; set; }
    }

    public static class JobStatuses
    {
        public const string Pending = "Pending";
        public const string Processing = "Processing";
        public const string Completed = "Completed";
        public const string Failed = "Failed";
    }



}
