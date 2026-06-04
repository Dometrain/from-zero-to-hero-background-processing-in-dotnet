using Microsoft.EntityFrameworkCore;

namespace APIProject.Services
{
    public class NotificationService
    {
        private readonly AppDbContext _dbContext;
        public NotificationService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task SendProcessingCompleteAsync(Guid id, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        private async Task SendEmailAsync(Guid documentId, 
            string recipient,
            CancellationToken cancellation)
        {
            throw new NotImplementedException();
        }

        public async Task SendEmailProcessingCompleteAsync(Guid id, CancellationToken cancellationToken)
        {
            var notifications = await _dbContext.EmailNotifications
                    .Where(n => !n.Sent)
                    .OrderBy(n => n.CreatedAt)
                    .Take(50)
                    .ToListAsync(cancellationToken); 
            
            foreach (var notification in notifications)
            {
                await SendEmailAsync(
                    notification.DocumentId,
                    notification.Recipient,
                    cancellationToken);

                notification.Sent = true;
                
                notification.SentAt = DateTime.UtcNow;
            }
            await _dbContext.SaveChangesAsync(cancellationToken);
        }


        public async Task SendPendingEmailNotifications(CancellationToken cancellationToken)
        {

        }
    }
}
