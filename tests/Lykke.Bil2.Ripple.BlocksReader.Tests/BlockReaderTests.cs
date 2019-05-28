using System.Threading.Tasks;
using System.Linq;
using Lykke.Bil2.Contract.BlocksReader.Events;
using Lykke.Bil2.Ripple.BlocksReader.Services;
using Lykke.Bil2.Ripple.Client;
using Lykke.Bil2.Ripple.Client.Api.Ledger;
using Lykke.Bil2.Sdk.BlocksReader.Services;
using Lykke.Bil2.SharedDomain;
using Lykke.Numerics;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using Refit;

namespace BlockReaderTests
{
    public class Tests
    {
        private Mock<IRippleApi> _rippleApi;
        private Mock<IBlockListener> _blockListenerMock;
        private BlockReader _blockReader;
        private Mock<IBlockTransactionsListener> _transactionsListenerMock;

        [SetUp]
        public void Setup()
        {
            _rippleApi = new Mock<IRippleApi>();
            _blockListenerMock = new Mock<IBlockListener>();
            _blockReader = new BlockReader(_rippleApi.Object);
            _transactionsListenerMock = new Mock<IBlockTransactionsListener>();
            _blockListenerMock
                .Setup(x => x.StartBlockTransactionsHandling(It.IsNotNull<BlockHeaderReadEvent>()))
                .Returns<BlockHeaderReadEvent>(evt => _transactionsListenerMock.Object);
        }

        [Test]
        public async Task ShouldEmitExecutedTransactionEvent()
        {
            // Arrange

            _rippleApi
                .Setup(x => x.Post(It.IsAny<BinaryLedgerWithTransactionsRequest>()))
                .ReturnsAsync
                (
                    new RippleResponse<BinaryLedgerWithTransactionsResult>
                    {
                        Result = new BinaryLedgerWithTransactionsResult
                        {
                            Ledger = new BinaryLedger
                            {
                                Closed = true,
                                LedgerData = "02B616D101633DE70AD949432444528DC116BCED649E7BC5274D0486893D4E34A1E1B401BB2BA4EC1D03FC45E53BF5220C1B781106D98E44785090F85AA45F75D08B6144F00504D0B96CCA8C941120D8A61BEE498A557192802A3A21E90E206ED383CF1B484D7CEF3B6A992F240BE268240BE2700A00",
                                Transactions = new[]
                                {
                                    new BinaryTransaction
                                    {
                                        Meta = "201C00000000F8E51100722502B616AE55C643B304E9DD1789D3C91ACB1BEEB62FDD102672B13DD39A6E3227AD43E3E68156A19518364F4A719048CF362E33427BDDEE1B751F48F6CB87F6BCA4D28C48B33FE6629459374E52A9C180000000000000000000000000434E5900000000000000000000000000000000000000000000000001E1E72200220000370000000000000002380000000000000245629590E06FEEA0375C000000000000000000000000434E5900000000000000000000000000000000000000000000000001668000000000000000000000000000000000000000434E59000000000040D20DA7A5CF1D2B45D2E6ACC5EF8B92E0ABA98467D6C38D7EA4C68000000000000000000000000000434E590000000000EA39E2DEFDE7756E1685C239097606D5CD723BB7E1E1E51100612502B5F6975557FEAD5AE561340C0AD5C5047DD6CCD70105F0E1E259669F37FC2EC50CB2260B56ACFAD680BDF44E2CD86A6174423DFD181416DAB0982725FC1873F072DCAC7090E6240000007D624000000003562E03E1E72200800000240000007E2D00000002624000000003562DF7811440D20DA7A5CF1D2B45D2E6ACC5EF8B92E0ABA984E1E1F1031000",
                                        TxBlob = "1200002280000000240000007D201B02B616D361D590E05F68139800000000000000000000000000434E590000000000EA39E2DEFDE7756E1685C239097606D5CD723BB768400000000000000C69D5910B938F846E00000000000000000000000000434E59000000000040D20DA7A5CF1D2B45D2E6ACC5EF8B92E0ABA984732102479D7FBB169ACAFFEEFA1D1FFEF682A5558A9FD7F2FDDE334F129689B4C9AAF674463044022038AF9AA12E2616AC5A1E4F8C88CD49577E6DB02F599CBC372BC2A5F3E55B87D202204D986ABB65D8D4A41AB847AC9B2766B5167279E97859E45DFADF64F683D9CC00811440D20DA7A5CF1D2B45D2E6ACC5EF8B92E0ABA9848314EA39E2DEFDE7756E1685C239097606D5CD723BB7F9EA7C06636C69656E747D207274312E312E33322D6275676669782D322D67653135323239372D6469727479E1F1",
                                    }
                                }
                            },
                            LedgerHash = "0C3261AB65F318BC1727F5EC1EE768EA08E11657C328D872C387FAF0CAF3870D",
                            LedgerIndex = 45487825,
                            Validated = true,
                            Status = "success"
                        }
                    }
                );

            // Act

            await _blockReader.ReadBlockAsync(45487825, _blockListenerMock.Object);

            // Assert

            _blockListenerMock.Verify
            (
                x => x.StartBlockTransactionsHandling
                (
                    It.Is<BlockHeaderReadEvent>(e =>
                        e.BlockNumber == 45487825 &&
                        e.BlockId == "0C3261AB65F318BC1727F5EC1EE768EA08E11657C328D872C387FAF0CAF3870D" &&
                        e.BlockMiningMoment == 604758640L.FromRippleEpoch()
                    )
                ),
                Times.Once
            );

            _blockListenerMock.Verify
            (
                x => x.HandleRawBlock(It.IsAny<Base64String>(), "0C3261AB65F318BC1727F5EC1EE768EA08E11657C328D872C387FAF0CAF3870D"),
                Times.Once
            );


            _transactionsListenerMock.Verify
            (
                x => x.HandleRawTransactionAsync
                (
                    It.IsAny<Base64String>(),
                    It.Is<TransactionId>(id => id == "97A020A130F8291A2DECD187AC5DA6D636EE7B3442175D30F9E42C082FD8EB13")
                ),
                Times.Once
            );
            _transactionsListenerMock.Verify
            (
                x => x.HandleExecutedTransaction
                (
                    It.Is<TransferAmountExecutedTransaction>(e =>
                        e.BalanceChanges.Count(y => y.Address == "raujtsGHt5u8pv23VWFbhxhpJKHYmzp376" && y.Asset.Id == "CNY" && y.Value == Money.Create(-47503M)) == 1 &&
                        e.IsIrreversible == true &&
                        e.TransactionId == "97A020A130F8291A2DECD187AC5DA6D636EE7B3442175D30F9E42C082FD8EB13" &&
                        e.TransactionNumber == 1
                    )
                ),
                Times.Once
            );
        }

        [Test]
        public async Task ShouldEmitBlockNotFoundEvent()
        {
            // Arrange

            _rippleApi
                .Setup(x => x.Post(It.IsAny<BinaryLedgerWithTransactionsRequest>()))
                .ReturnsAsync
                (
                    new RippleResponse<BinaryLedgerWithTransactionsResult>
                    {
                        Result = new BinaryLedgerWithTransactionsResult
                        {
                            Error = "lgrNotFound",
                            Status = "error"
                        }
                    }
                );

            // Act

            await _blockReader.ReadBlockAsync(45487825, _blockListenerMock.Object);

            // Assert

            _blockListenerMock.Verify
            (
                x => x.HandleNotFoundBlock(It.Is<BlockNotFoundEvent>(e => e.BlockNumber == 45487825)),
                Times.Once
            );
        }

        [Test]
        public async Task ShouldEmitFailedTransactionEvent()
        {
            // Arrange

            _rippleApi
                .Setup(x => x.Post(It.IsAny<BinaryLedgerWithTransactionsRequest>()))
                .ReturnsAsync
                (
                    new RippleResponse<BinaryLedgerWithTransactionsResult>
                    {
                        Result = new BinaryLedgerWithTransactionsResult
                        {
                            Ledger = new BinaryLedger
                            {
                                Closed = true,
                                LedgerData = "02B616D101633DE70AD949432444528DC116BCED649E7BC5274D0486893D4E34A1E1B401BB2BA4EC1D03FC45E53BF5220C1B781106D98E44785090F85AA45F75D08B6144F00504D0B96CCA8C941120D8A61BEE498A557192802A3A21E90E206ED383CF1B484D7CEF3B6A992F240BE268240BE2700A00",
                                Transactions = new BinaryTransaction[]
                                {
                                    new BinaryTransaction
                                    {
                                        Meta = "201C00000017F8E51100612502B616CB558AE11DD43E6DA61A54E8931F221459541C60F612191EFB59D7F8161A9B4986B156F15602AFF83A7716A543602DBF043E232782706F680EB9F53ABAC50E52655928E624000661A662400000014B0358E2E1E7220000000024000661A72D0000000062400000014B0358D881141695B6DD94F344C206BB6192AAFEFFC97A17CB6FE1E1F103107D",
                                        TxBlob = "120000228000000024000661A66140000000009BB2EE68400000000000000A73210343719FFA04287BE4C7EC431543A068E60D15AB2DCEC8F8775601C3920761684D74473045022100E146B9B85350F2008B4C0D9B7CE9180FBFDC37C1F386CED3928336C0CA1347B502200DD50856187188BA16ADFCCCCB16A939192053E55FBE6D513D5E4A3DF5E103FB81141695B6DD94F344C206BB6192AAFEFFC97A17CB6F83146F7ECF78515020A2BB86B02C84C85632AD7D6DA6",
                                    }
                                }
                            },
                            LedgerHash = "0C3261AB65F318BC1727F5EC1EE768EA08E11657C328D872C387FAF0CAF3870D",
                            LedgerIndex = 45487825,
                            Validated = true,
                            Status = "success"
                        }
                    }
                );

            // Act

            await _blockReader.ReadBlockAsync(45487825, _blockListenerMock.Object);

            // Assert

            _transactionsListenerMock.Verify
            (
                x => x.HandleRawTransactionAsync
                (
                    It.IsAny<Base64String>(),
                    It.IsAny<TransactionId>()
                ),
                Times.Once
            );
            _transactionsListenerMock.Verify
            (
                x => x.HandleFailedTransaction
                (
                    It.Is<FailedTransaction>(e =>
                        e.TransactionId == "2B0CD5BD2D9898EB6F59E222325DA5D4F361843D6725DC81CDB21F39D134E140" &&
                        e.ErrorMessage == "tecNO_DST_INSUF_XRP" &&
                        e.TransactionNumber == 24
                    )
                ),
                Times.Once
            );
        }
    
        [Test]
        public async Task ShouldReadBlock()
        {
            var serviceProvider = new ServiceCollection()
                .AddRippleClient("http://s2.ripple.com:51234", logRequestErrors: false)
                .BuildServiceProvider();

            var api = serviceProvider.GetRequiredService<IRippleApi>();

            var blockReader = new BlockReader(api);

            try
            {
                await blockReader.ReadBlockAsync(46127240, _blockListenerMock.Object);
            }
            catch (ApiException)
            {
                Assert.Ignore("Public Ripple node is not available");
            }

            _blockListenerMock.Verify
            (
                x => x.StartBlockTransactionsHandling
                (
                    It.Is<BlockHeaderReadEvent>(e =>
                        e.BlockNumber == 46127240 &&
                        e.BlockId == "2B92A3025761FE68709129C979A29929D41C858EE70C20F58BA5E4D9BDC46D4D" &&
                        e.BlockTransactionsCount == 23
                    )
                ),
                Times.Once
            );

            _blockListenerMock.Verify
            (
                x => x.HandleRawBlock(It.IsAny<Base64String>(), "2B92A3025761FE68709129C979A29929D41C858EE70C20F58BA5E4D9BDC46D4D"),
                Times.Once
            );

            _transactionsListenerMock.Verify
            (
                x => x.HandleRawTransactionAsync
                (
                    It.IsAny<Base64String>(),
                    It.IsAny<TransactionId>()
                ),
                Times.Exactly(23)
            );

            _transactionsListenerMock.Verify
            (
                x => x.HandleExecutedTransaction
                (
                    It.IsAny<TransferAmountExecutedTransaction>()
                ),
                Times.Exactly(17)
            );

            _transactionsListenerMock.Verify
            (
                x => x.HandleFailedTransaction
                (
                    It.IsAny<FailedTransaction>()
                ),
                Times.Exactly(6)
            );
        }

        [Test]
        public void Test_block_multiple_balance_changes_for_the_same_address_asset_and_transfer()
        {
            var serviceProvider = new ServiceCollection()
                .AddRippleClient("http://s2.ripple.com:51234", logRequestErrors: false)
                .BuildServiceProvider();

            var api = serviceProvider.GetRequiredService<IRippleApi>();

            var blockReader = new BlockReader(api);

            Assert.DoesNotThrowAsync(() => blockReader.ReadBlockAsync(556049, _blockListenerMock.Object));
        }
    }
}