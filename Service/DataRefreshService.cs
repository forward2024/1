public class DataRefreshService : BackgroundService
{
    private readonly IServiceProvider services;

    public DataRefreshService(IServiceProvider services)
    {
        this.services = services;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using (var scope = services.CreateScope())
            {
                var noteManager = scope.ServiceProvider.GetRequiredService<NoteManager>();

                await noteManager.RefreshDataAsync();
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}