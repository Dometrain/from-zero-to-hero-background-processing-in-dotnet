using APIProject.Interfaces;
using APIProject.Models;
using APIProject.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace APIProject.Controllers
{
    [Route("api/async-document-processing")]

    public class AsyncDocument_ProcessingController(DocumentService documentService,
        DocumentProcessorService documentProcessor, 
        IBackgroundJobQueue backgroundJobQueue) : Controller
    {
        [HttpPost("process")]
        public async Task<IActionResult> ProcessDocument(
                DocumentUpload upload,
                CancellationToken cancellationToken)
        {
            var document = await documentService.SaveAsync(
                upload,
                cancellationToken);

            var jobId = Guid.NewGuid();

            var payload = JsonSerializer.Serialize(new ProcessDocumentPayload()
            {
                DocumentId = document.Id,
                JobId = jobId
            });

            var job = new BackgroundJob()
            {
                Id = jobId,
                Type = "ProcessDocument",
                Payload = payload,
                CreatedAt = DateTime.UtcNow
            };

            await backgroundJobQueue.QueueAsync(job, cancellationToken);

            return Accepted(new
            {
                DocumentId = document.Id,
                JobId = jobId,
                Status = "Queued"
            });
        }

        [HttpPost]
        public async Task<IActionResult> ProcessDocumentAsTask(
                DocumentUpload upload,
                CancellationToken cancellationToken)
        {
            var document = await documentService.SaveAsync(
                upload,
                cancellationToken);

            _ = Task.Run(async() => 
            {
                await documentProcessor.ProcessAsync(document.Id, cancellationToken);
            });

            return Accepted();
        }
    }
}
