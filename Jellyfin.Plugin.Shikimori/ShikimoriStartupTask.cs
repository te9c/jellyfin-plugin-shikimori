using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Shikimori
{
    public class ShikimoriStartupTask : IStartupTask
    {
        private readonly ILogger<ShikimoriStartupTask> _logger;

        public ShikimoriStartupTask(ILogger<ShikimoriStartupTask> logger)
        {
            _logger = logger;
        }

        public Task ExecuteAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Shikimori plugin started successfully");
            return Task.CompletedTask;
        }

        public string Name => "Shikimori Plugin Startup";
        public string Description => "Initializes the Shikimori metadata provider";
        public string Category => "Metadata";
    }
}
