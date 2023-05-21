namespace CountryService.Services
{
    public class BackgroundWorkerService : BackgroundService
    {
        private readonly ILogger<BackgroundWorkerService> _logger;

        public BackgroundWorkerService(ILogger<BackgroundWorkerService> logger)
        {
            _logger = logger;
        }

        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Working running at : {time}", DateTimeOffset.Now);
                await Task.Delay(2000, stoppingToken);
            }
        }
    }
}
