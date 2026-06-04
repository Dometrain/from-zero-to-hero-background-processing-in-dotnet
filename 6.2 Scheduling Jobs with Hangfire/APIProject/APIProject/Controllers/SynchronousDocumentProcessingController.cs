using APIProject.Models;
using APIProject.Services;
using Microsoft.AspNetCore.Mvc;

namespace APIProject.Controllers
{
    public class SynchronousDocumentProcessingController(DocumentService documentService, 
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
            
            var result = await documentProcessor.ProcessAsync(
                document.Id,
                cancellationToken);

            await documentService.SaveResultAsync(
                document.Id,
                result,
                cancellationToken);

            await notificationService.SendProcessingCompleteAsync(
                document.Id,
                cancellationToken);

            return Ok(new
            {
                DocumentId = document.Id,
                Status = "Processed"
            });
        }
    }
}
