using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Common;
using Lykke.Bil2.Contract.BlocksReader.Events;
using Lykke.Bil2.Contract.Common.Exceptions;
using Lykke.Bil2.Ripple.Client;
using Lykke.Bil2.Ripple.Client.Api.Ledger;
using Lykke.Bil2.Sdk.BlocksReader.Services;
using Lykke.Bil2.SharedDomain;
using Lykke.Numerics;

namespace Lykke.Bil2.Ripple.BlocksReader.Services
{
    public class BlockReader : IBlockReader
    {
        private readonly IRippleApi _rippleApi;

        public BlockReader(IRippleApi rippleApi)
        {
            _rippleApi = rippleApi;
        }

        public async Task ReadBlockAsync(long blockNumber, IBlockListener listener)
        {
            if (blockNumber <= 0)
            {
                throw new RequestValidationException("Invalid block number, must be greater than 0", nameof(blockNumber));
            }

            var ledgerResponse = await _rippleApi.Post(new BinaryLedgerWithTransactionsRequest((uint)blockNumber));

            if (ledgerResponse.Result.Error == "lgrNotFound")
            {
                listener.HandleNotFoundBlock(new BlockNotFoundEvent(blockNumber));
                return;
            }

            ledgerResponse.Result.ThrowIfError();

            listener.HandleRawBlock
            (
                Base64String.Encode(ledgerResponse.Result.Ledger.LedgerData),
                ledgerResponse.Result.LedgerHash
            );

            var header = ledgerResponse.Result.Ledger.Parse(headerOnly: true);

            var transactionsListener = listener.StartBlockTransactionsHandling(new BlockHeaderReadEvent
            (
                blockNumber,
                ledgerResponse.Result.LedgerHash,
                header.CloseTime.FromRippleEpoch(),
                ledgerResponse.Result.Ledger.LedgerData.GetHexStringToBytes().Length,
                ledgerResponse.Result.Ledger.Transactions.Length,
                header.ParentHash
            ));

            // emit transactions events

            foreach (var binaryTx in ledgerResponse.Result.Ledger.Transactions)
            {
                var tx = binaryTx.Parse();
                var txNumber = (int)(tx.Metadata.TransactionIndex + 1);
                var txRaw = Base64String.Encode(binaryTx.TxBlob);
                var txFee = new[]
                {
                    new Fee
                    (
                        new Asset("XRP"),
                        new UMoney(BigInteger.Parse(tx.Fee), 6)
                    )
                };

                await transactionsListener.HandleRawTransactionAsync(txRaw, tx.Hash);

                if (tx.Metadata.TransactionResult == "tesSUCCESS")
                {
                    transactionsListener.HandleExecutedTransaction
                    (
                        new TransferAmountExecutedTransaction
                        (
                            txNumber,
                            tx.Hash,
                            tx.Metadata
                                .GetBalanceChanges()
                                .SelectMany(pair => pair.Value.Select(amount => (address: pair.Key, amount: amount)))
                                .Select(pair => new BalanceChange
                                (
                                    tx.Metadata.TransactionIndex.ToString(),
                                    new Asset(pair.amount.Currency, pair.amount.Counterparty),
                                    Money.Parse(pair.amount.Value),
                                    pair.address,
                                    pair.address == tx.Destination ? tx.DestinationTag?.ToString("D") : null,
                                    pair.address == tx.Destination && tx.DestinationTag != null ? AddressTagType.Number : (AddressTagType?)null,
                                    pair.address == tx.Account ? tx.Sequence : (long?)null
                                ))
                                .ToArray(),
                            txFee,
                            ledgerResponse.Result.Validated ?? false
                        )
                    );
                }
                else
                {
                    transactionsListener.HandleFailedTransaction
                    (
                        new FailedTransaction
                        (
                            txNumber,
                            tx.Hash,
                            tx.Metadata.TransactionResult == "tecUNFUNDED" || tx.Metadata.TransactionResult == "tecUNFUNDED_PAYMENT"
                                ? TransactionBroadcastingError.NotEnoughBalance
                                : TransactionBroadcastingError.Unknown,
                            tx.Metadata.TransactionResult,
                            txFee
                        )
                    );
                }
            }
        }
    }
}