using System.ComponentModel;
using System;
using JetBrains.Annotations;
using Lykke.Bil2.Lykke.Bil2.Ripple.BlocksReader.BlocksReader.Services;
using Lykke.Bil2.Lykke.Bil2.Ripple.BlocksReader.BlocksReader.Settings;
using Lykke.Bil2.Sdk.BlocksReader;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Lykke.Bil2.Sdk.BlocksReader.Services;
using Lykke.Bil2.Ripple.Client;

namespace Lykke.Bil2.Lykke.Bil2.Ripple.BlocksReader.BlocksReader
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
                        settings.CurrentValue.NodeUrl,
                        settings.CurrentValue.NodeRpcUsername,
                        settings.CurrentValue.NodeRpcPassword
                    );
                };
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