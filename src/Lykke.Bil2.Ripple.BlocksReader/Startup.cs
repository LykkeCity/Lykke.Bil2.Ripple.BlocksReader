using System;
using JetBrains.Annotations;
using Lykke.Bil2.Ripple.BlocksReader.Services;
using Lykke.Bil2.Ripple.BlocksReader.Settings;
using Lykke.Bil2.Ripple.Client;
using Lykke.Bil2.Sdk.BlocksReader;
using Lykke.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Lykke.Bil2.Ripple.BlocksReader
{
    [UsedImplicitly]
    public class Startup
    {
        private const string IntegrationName = "Ripple";

        [UsedImplicitly]
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            return services.BuildBlockchainBlocksReaderServiceProvider<AppSettings>(options =>
            {
                options.IntegrationName = IntegrationName;
#if DEBUG
                options.RabbitVhost = AppEnvironment.EnvInfo;
#endif

                // Register required service implementations:

                options.BlockReaderFactory = ctx =>
                    new BlockReader
                    (
                        ctx.Services.GetRequiredService<IRippleApi>()
                    );

                // Register irreversible block retrieving strategy

                options.AddIrreversibleBlockPulling(ctx =>
                    new IrreversibleBlockProvider
                    (
                        ctx.Services.GetRequiredService<IRippleApi>()
                    ));

                // Register additional services

                options.UseSettings = (serviceCollection, settings) =>
                {
                    serviceCollection.AddRippleClient
                    (
                        settings.CurrentValue.NodeRpcUrl,
                        settings.CurrentValue.NodeRpcUsername,
                        settings.CurrentValue.NodeRpcPassword
                    );
                };

                options.UseTransferAmountTransactionsModel();
            });
        }

        [UsedImplicitly]
        public void Configure(IApplicationBuilder app)
        {
            app.UseBlockchainBlocksReader(options =>
            {
                options.IntegrationName = IntegrationName;
            });
        }
    }
}