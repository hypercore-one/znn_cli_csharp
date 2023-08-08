using CommandLine;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using Zenon;
using Zenon.Crypto;
using Zenon.Model.NoM;
using Zenon.Model.Primitives;
using Zenon.Utils;

namespace ZenonCli.Commands
{
    public class General
    {
        [Verb("version", HelpText = "Display version information.")]
        public class Version : CommandBase
        {
            protected override async Task ProcessAsync()
            {
                await Task.Run(() =>
                {
                    var cliVersion = Assembly.GetExecutingAssembly()!.GetName().Version!;
                    var sdkVersion = Assembly.GetAssembly(typeof(Constants))!.GetName().Version!;

                    WriteInfo($"{ThisAssembly.AssemblyName} v{cliVersion.ToString(3)} using Zenon SDK v{sdkVersion.ToString(3)}");
                });
            }
        }

        [Verb("send", HelpText = "Send tokens to an address.")]
        public class Send : KeyStoreAndConnectionCommand
        {
            [Value(0, Required = true, MetaName = "toAddress")]
            public string? ToAddress { get; set; }

            [Value(1, Required = true, MetaName = "amount")]
            public string? Amount { get; set; }

            [Value(2, Default = "ZNN", MetaName = "tokenStandard", MetaValue = "[ZNN/QSR/ZTS]")]
            public string? TokenStandard { get; set; }

            [Value(3, MetaName = "message")]
            public string? Message { get; set; }

            protected override async Task ProcessAsync()
            {
                var address = await Zdk!.DefaultWalletAccount.GetAddressAsync();
                var recipient = ParseAddress(ToAddress);
                var tokenStandard = ParseTokenStandard(TokenStandard);
                var token = await GetTokenAsync(tokenStandard);
                var amount = ParseAmount(Amount!, token.Decimals);

                if (amount <= 0)
                {
                    WriteError($"You cannot send that amount.");
                    return;
                }

                await AssertBalanceAsync(address, tokenStandard, amount);

                var data = this.Message != null ? Encoding.UTF8.GetBytes(Message) : null;

                if (data != null)
                {
                    WriteInfo($"Sending {FormatAmount(amount, token.Decimals)} {this.TokenStandard} to {this.ToAddress} with a message \"{this.Message}\"");
                }
                else
                {
                    WriteInfo($"Sending {FormatAmount(amount, token.Decimals)} {this.TokenStandard} to {this.ToAddress}");
                }

                await Zdk!.SendAsync(AccountBlockTemplate.Send(Zdk!.Client.ProtocolVersion, Zdk!.Client.ChainIdentifier, recipient, tokenStandard, amount, data));

                WriteInfo("Done");
            }
        }

        [Verb("receive", HelpText = "Manually receive a transaction by blockHash.")]
        public class Receive : KeyStoreAndConnectionCommand
        {
            [Value(0, MetaName = "blockHash", Required = true)]
            public string? BlockHash { get; set; }

            protected override async Task ProcessAsync()
            {
                var block = ParseHash(BlockHash);

                WriteInfo("Please wait ...");

                await Zdk!.SendAsync(AccountBlockTemplate.Receive(Zdk!.Client.ProtocolVersion, Zdk!.Client.ChainIdentifier, block));

                WriteInfo("Done");
            }
        }

        [Verb("receiveAll", HelpText = "Receive all pending transactions.")]
        public class ReceiveAll : KeyStoreAndConnectionCommand
        {
            protected override async Task ProcessAsync()
            {
                var address = await Zdk!.DefaultWalletAccount.GetAddressAsync();

                var unreceived = await Zdk!.Ledger
                    .GetUnreceivedBlocksByAddress(address, pageIndex: 0, pageSize: 5);

                if (unreceived.Count == 0)
                {
                    WriteInfo("Nothing to receive");
                    return;
                }
                else
                {
                    if (unreceived.More)
                    {
                        WriteInfo($"You have more than {unreceived.Count} transaction(s) to receive");
                    }
                    else
                    {
                        WriteInfo($"You have {unreceived.Count} transaction(s) to receive");
                    }
                }

                WriteInfo("Please wait ...");

                while (unreceived.Count > 0)
                {
                    foreach (var block in unreceived.List)
                    {
                        await Zdk!.SendAsync(AccountBlockTemplate.Receive(Zdk!.Client.ProtocolVersion, Zdk!.Client.ChainIdentifier, block.Hash));
                    }

                    unreceived = await Zdk!.Ledger
                        .GetUnreceivedBlocksByAddress(address, pageIndex: 0, pageSize: 5);
                }

                WriteInfo("Done");
            }
        }

        [Verb("autoreceive", HelpText = "Automaticly receive transactions.")]
        public class Autoreceive : KeyStoreAndConnectionCommand
        {
            protected override async Task ProcessAsync()
            {
                var address = await Zdk!.DefaultWalletAccount.GetAddressAsync();

                var queue = new BlockingCollection<Hash>();

                WriteInfo("Subscribing for account-block events ...");
                await Zdk!.Subscribe.ToAllAccountBlocks((json) =>
                {
                    for (var i = 0; i < json.Length; i += 1)
                    {
                        var tx = json[i];
                        if (tx.Value<string>("toAddress") != address.ToString())
                        {
                            continue;
                        }
                        var hash = ParseHash(tx.Value<string>("hash"));
                        WriteInfo($"receiving transaction with hash {hash}");
                        queue.Add(hash);
                    }
                });
                WriteInfo("Subscribed successfully!");

                while (true)
                {
                    Hash? hash;
                    if (queue.TryTake(out hash))
                    {
                        var template = await Zdk!.SendAsync(AccountBlockTemplate.Receive(Zdk!.Client.ProtocolVersion, Zdk!.Client.ChainIdentifier, hash));
                        WriteInfo($"successfully received {hash}. Receive-block-hash {template.Hash}");
                    }

                    await Task.Delay(1000);
                }
            }
        }

        [Verb("unreceived", HelpText = "List pending/unreceived transactions.")]
        public class Unreceived : KeyStoreAndConnectionCommand
        {
            protected override async Task ProcessAsync()
            {
                var address = await Zdk!.DefaultWalletAccount.GetAddressAsync();

                var unreceived = await Zdk!.Ledger
                    .GetUnreceivedBlocksByAddress(address, pageIndex: 0, pageSize: 5);

                if (unreceived.Count == 0)
                {
                    WriteInfo("Nothing to receive");
                    return;
                }
                else
                {
                    if (unreceived.More)
                    {
                        WriteInfo($"You have more than {unreceived.Count} transaction(s) to receive");
                    }
                    else
                    {
                        WriteInfo($"You have {unreceived.Count} transaction(s) to receive");
                    }
                    WriteInfo($"Showing the first {unreceived.List.Length}");
                }

                foreach (var block in unreceived.List)
                {
                    WriteInfo(
                        $"Unreceived {FormatAmount(block.Amount, block.Token.Decimals)} {block.Token.Symbol} from {block.Address}. Use the hash {block.Hash} to receive");
                }
            }
        }

        [Verb("unconfirmed", HelpText = "List unconfirmed transactions.")]
        public class Unconfirmed : KeyStoreAndConnectionCommand
        {
            protected override async Task ProcessAsync()
            {
                var address = await Zdk!.DefaultWalletAccount.GetAddressAsync();

                var unconfirmed = await Zdk!.Ledger
                    .GetUnconfirmedBlocksByAddress(address, pageIndex: 0, pageSize: 5);

                if (unconfirmed.Count == 0)
                {
                    WriteInfo("No unconfirmed transactions");
                }
                else
                {
                    WriteInfo($"You have {unconfirmed.Count} unconfirmed transaction(s)");
                    WriteInfo($"Showing the first {unconfirmed.List.Length}");
                }

                foreach (var block in unconfirmed.List)
                {
                    WriteInfo(JsonConvert.SerializeObject(block.ToJson(), Formatting.Indented));
                }
            }
        }

        [Verb("balance", HelpText = "List account balance.")]
        public class Balance : ConnectionCommand
        {
            [Value(0, Required = true, MetaName = "address")]
            public string? Address { get; set; }

            protected override async Task ProcessAsync()
            {
                var address = ParseAddress(Address);

                var info = await Zdk!.Ledger
                    .GetAccountInfoByAddress(address);

                WriteInfo($"Balance for account-chain {info.Address} having height {info.BlockCount}");
                if (info.BalanceInfoList.Length == 0)
                {
                    WriteInfo($"  No coins or tokens at address {address}");
                }

                foreach (var entry in info.BalanceInfoList)
                {
                    WriteInfo($"  {FormatAmount(entry.Balance!.Value, entry.Token.Decimals)} {entry.Token.Symbol} {entry.Token.Domain} {entry.Token.TokenStandard}");
                }
            }
        }

        [Verb("frontierMomentum", HelpText = "Display frontier momentum.")]
        public class FrontierMomentum : ConnectionCommand
        {
            protected override async Task ProcessAsync()
            {
                var currentFrontierMomentum =
                    await Zdk!.Ledger.GetFrontierMomentum();

                WriteInfo($"Momentum height: {currentFrontierMomentum.Height}");
                WriteInfo($"Momentum hash: {currentFrontierMomentum.Hash}");
                WriteInfo($"Momentum previousHash: {currentFrontierMomentum.PreviousHash}");
                WriteInfo($"Momentum timestamp: {currentFrontierMomentum.Timestamp}");
            }
        }

        [Verb("createHash", HelpText = "Create hash digests by using the stated algorithm.")]
        public class CreateHash : CommandBase
        {
            [Value(0, MetaName = "hashType", Default = 0, HelpText = "0 = SHA3-256, 1 = SHA2-256")]
            public int? Type { get; set; }

            [Value(1, MetaName = "keySize", Default = 32, HelpText = "The size of the preimage.")]
            public int? KeySize { get; set; }

            protected override async Task ProcessAsync()
            {
                await Task.Run(() =>
                {
                    if (!this.Type.HasValue)
                    {
                        this.Type = Constants.HtlcHashTypeSha3;
                    }

                    if (this.Type != Constants.HtlcHashTypeSha3 && this.Type != Constants.HtlcHashTypeSha256)
                    {
                        WriteError($"Invalid hash type. Hash type {this.Type} is not supported.");
                        return;
                    }

                    if (!this.KeySize.HasValue)
                    {
                        this.KeySize = Constants.HtlcPreimageDefaultLength;
                    }

                    if (this.KeySize > Constants.HtlcPreimageMaxLength || this.KeySize < Constants.HtlcPreimageMinLength)
                    {
                        WriteInfo($"Invalid key size. Preimage size must be {Constants.HtlcPreimageMaxLength} bytes or less.");
                        return;
                    }

                    if (this.KeySize < Constants.HtlcPreimageDefaultLength)
                    {
                        WriteWarning($"Key size is less than {Constants.HtlcPreimageDefaultLength} and may be insecure.");
                    }

                    var preimage = Helper.GeneratePreimage(this.KeySize.Value);
                    WriteInfo($"Preimage: {BytesUtils.ToHexString(preimage)}");

                    byte[] digest;

                    switch (this.Type)
                    {
                        case Constants.HtlcHashTypeSha256:
                            digest = Helper.ComputeSha256Hash(preimage);
                            WriteInfo($"SHA2-256 Hash: {BytesUtils.ToHexString(digest)}");
                            break;

                        default:
                            digest = Crypto.Digest(preimage);
                            WriteInfo($"SHA3-256 Hash: {BytesUtils.ToHexString(digest)}");
                            break;
                    }
                });
            }
        }
    }
}