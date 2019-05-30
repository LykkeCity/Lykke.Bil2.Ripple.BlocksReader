using System;
using System.Collections.Generic;
using Lykke.Bil2.Ripple.Client.Api;
using Lykke.Bil2.SharedDomain;

namespace Lykke.Bil2.Ripple.BlocksReader.Services
{
    public static class TransactionExtensions
    {
        // Well known tx hash duplications
        private static readonly IReadOnlyDictionary<(BlockId blockId, string transactionHash), long>  Violations = new Dictionary<(BlockId blockId, string transactHash), long>
        {
            {("644732C14FA656B17770EC50653B5B1D5892D237ED3254A4ABB574AA64F0D82A", "C6A40F56127436DCD830B1B35FF939FD05B5747D30D6542572B7A835239817AF"), 0},
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
                return $"{txHash}_{suffix}";
            }

            if (SuspiciousTransactionHashes.Contains(txHash))
            {
                throw new InvalidOperationException($"Looks like not well known transaction duplicate is detected. Block {blockId}, transaction {txHash}");
            }

            return new TransactionId(txHash);
        }
    }
}