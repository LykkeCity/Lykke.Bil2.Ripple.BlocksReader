using System;
using Lykke.Bil2.Ripple.BlocksReader.Services;
using Lykke.Bil2.Ripple.Client.Api;
using NUnit.Framework;

namespace Lykke.Bil2.Ripple.BlocksReader.Tests
{
    public class TransactionExtensionsTests
    {
        [Test]
        // ReSharper disable once StringLiteralTypo
        [TestCase("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA", "C6A40F56127436DCD830B1B35FF939FD05B5747D30D6542572B7A835239817AF")]
        public void Test_suspicious_transactions_detection(string blockId, string transactionId)
        {
            var tx = new Transaction
            {
                Hash = transactionId
            };

            Assert.Throws<InvalidOperationException>(() => tx.GetId(blockId));
        }
    }
}