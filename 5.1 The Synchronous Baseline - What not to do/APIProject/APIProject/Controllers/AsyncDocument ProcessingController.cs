using APIProject.Models;
using APIProject.Services;
using Microsoft.AspNetCore.Mvc;

namespace APIProject.Controllers
{
    public class AsyncDocument_ProcessingController(DocumentService documentService,
        DocumentProcessorService documentProcessor,
        NotificationService notificationService) : Controller
    {
        [HttpPost]
        public async Task<IActionResult> ProcessDocument(
                DocumentUpload upload,
                CancellationToken cancellationToken)
        {
            var document = await documentService.SaveAsync(
                upload,
                cancellationToken);

            var jobId = await documentProcessor.CreateDocProcessingJob(document.Id, cancellationToken);

            return Accepted(new
            {
                DocumentId = document.Id,
                JobId = jobId,
                Status = "Queued"
            });
        }
    }
}
