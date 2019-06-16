using System;
using System.Collections.Generic;
using Lykke.Bil2.Ripple.Client.Api;
using Lykke.Bil2.SharedDomain;

namespace Lykke.Bil2.Ripple.BlocksReader.Services
{
    public static class TransactionExtensions
    {
        // Well known tx hash duplications
        private static readonly IReadOnlyDictionary<(BlockId blockId, string transactionHash), long?>  Violations = new Dictionary<(BlockId blockId, string transactHash), long?>
        {
            {("3BACFC2C30CF4B124FCFF2B2539C8B400B11249A3C3D8AC80F7E558F0E7B062F", "C6A40F56127436DCD830B1B35FF939FD05B5747D30D6542572B7A835239817AF"), null},
            {("644732C14FA656B17770EC50653B5B1D5892D237ED3254A4ABB574AA64F0D82A", "C6A40F56127436DCD830B1B35FF939FD05B5747D30D6542572B7A835239817AF"), 0},
            {("843F6CAF48A3CC6507EEC4FE7E5C442F8F424F322A5A5DF7FDECE317E2F5B954", "1C15FEA3E1D50F96B6598607FC773FF1F6E0125F30160144BE0C5CBC52F5151B"), null},
            {("CC4195013CAD3B20FD87D74FD638A4F0583E961159A4D6288D449471CD778C77", "1C15FEA3E1D50F96B6598607FC773FF1F6E0125F30160144BE0C5CBC52F5151B"), 0}
        };

        private static readonly ISet<string> SuspiciousTransactionHashes = new HashSet<string>
        {
            "C6A40F56127436DCD830B1B35FF939FD05B5747D30D6542572B7A835239817AF"
        };

        public static TransactionId GetId(this Transaction tx, BlockId blockId)
        {
            var txHash = tx.Hash;

            if (Violations.TryGetValue((blockId, txHash), out var suffix))
            {
                return suffix.HasValue ? $"{txHash}_{suffix}" : txHash;
            }

            if (SuspiciousTransactionHashes.Contains(txHash))
            {
                throw new InvalidOperationException($"Looks like not well known transaction duplicate is detected. Block {blockId}, transaction {txHash}");
            }

            return new TransactionId(txHash);
        }
    }
}