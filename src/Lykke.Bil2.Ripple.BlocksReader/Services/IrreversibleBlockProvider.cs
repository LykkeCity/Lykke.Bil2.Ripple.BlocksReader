using System.Threading.Tasks;
using Lykke.Bil2.Contract.BlocksReader.Events;
using Lykke.Bil2.Ripple.Client;
using Lykke.Bil2.Ripple.Client.Api.ServerState;
using Lykke.Bil2.Sdk.BlocksReader.Services;
using Lykke.Bil2.Sdk.Exceptions;

namespace Lykke.Bil2.Ripple.BlocksReader.Services
{
    public class IrreversibleBlockProvider : IIrreversibleBlockProvider
    {
        private readonly IRippleApi _rippleApi;

        public IrreversibleBlockProvider(IRippleApi rippleApi)
        {
            _rippleApi = rippleApi;
        }

        public async Task<LastIrreversibleBlockUpdatedEvent> GetLastAsync()
        {
            var serverStateResponse = await _rippleApi.Post(new ServerStateRequest());

            serverStateResponse.Result.ThrowIfError();

            if (serverStateResponse.Result.State.ValidatedLedger == null)
            {
                throw new BlockchainIntegrationException(
                    $"Node didn't return last validated ledger, retry required. Node state: {serverStateResponse.Result.State.ServerState}");
            }

            return new LastIrreversibleBlockUpdatedEvent(
                serverStateResponse.Result.State.ValidatedLedger.Seq,
                serverStateResponse.Result.State.ValidatedLedger.Hash
            );
        }
    }
}