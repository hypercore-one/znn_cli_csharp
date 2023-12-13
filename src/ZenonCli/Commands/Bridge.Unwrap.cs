using CommandLine;
using Zenon.Model.Embedded;

namespace ZenonCli.Commands
{
    public partial class Bridge
    {
        public class Unwrap
        {
            [Verb("bridge.unwrap.redeem", HelpText = "Redeem a pending unwrap request for any recipient.")]
            public class Redeem : WalletAndConnectionCommand
            {
                [Value(0, MetaName = "hash", Required = true, HelpText = "The transaction hash")]
                public string? Hash { get; set; }

                [Value(1, MetaName = "logIndex", Required = true, HelpText = "The log index")]
                public long? LogIndex { get; set; }

                protected override async Task ProcessAsync()
                {
                    var address = await Zdk!.DefaultWalletAccount.GetAddressAsync();
                    var transactionHash = ParseHash(Hash);

                    var request = await Zdk!.Embedded.Bridge
                        .GetUnwrapTokenRequestByHashAndLog(transactionHash, (uint)LogIndex!.Value);

                    if (request.Redeemed == 0 && request.Revoked == 0)
                    {
                        await WriteRedeemAsync(request);

                        var redeem = Zdk!.Embedded.Bridge
                            .Redeem(request.TransactionHash, request.LogIndex);
                        await SendAsync(redeem);

                        WriteInfo("Done");
                        if (request.ToAddress == address)
                        {
                            WriteInfo("Use receiveAll to collect your unwrapped tokens after 2 momentums");
                        }
                    }
                    else
                    {
                        WriteInfo("The unwrap request cannot be redeemed");
                    }
                }
            }

            [Verb("bridge.unwrap.redeemAll", HelpText = "Redeem all pending unwrap requests for yourself or all addresses.")]
            public class RedeemAll : WalletAndConnectionCommand
            {
                [Value(0, MetaName = "redeem", Default = false, HelpText = "If the boolean is true, all unredeemed transactions will be redeemed")]
                public bool? Redeem { get; set; }

                protected override async Task ProcessAsync()
                {
                    var address = await Zdk!.DefaultWalletAccount.GetAddressAsync();

                    var redeemAllGlobally = Redeem.HasValue ? Redeem.Value : false;

                    var allUnwrapRequests =
                        await Zdk!.Embedded.Bridge.GetAllUnwrapTokenRequests();

                    int redeemedSelf = 0;
                    int redeemedTotal = 0;

                    foreach (var request in allUnwrapRequests.List)
                    {
                        if (request.Redeemed == 0 && request.Revoked == 0)
                        {
                            if (redeemAllGlobally ||
                                (!Redeem.HasValue && request.ToAddress == address))
                            {
                                await WriteRedeemAsync(request);
                                var redeem = Zdk!.Embedded.Bridge
                                    .Redeem(request.TransactionHash, request.LogIndex);
                                await SendAsync(redeem);
                                if (request.ToAddress == address)
                                {
                                    redeemedSelf += 1;
                                }
                                redeemedTotal += 1;
                            }
                        }
                    }
                    if (redeemedTotal > 0)
                    {
                        WriteInfo("Done");
                        if (redeemedSelf > 0)
                        {
                            WriteInfo($"Use receiveAll to collect your unwrapped tokens after 2 momentums");
                        }
                    }
                    else
                    {
                        WriteInfo("No redeemable unwrap requests were found");
                    }
                }
            }

            [Verb("bridge.unwrap.list", HelpText = "List all unwrap token requests by NoM address.")]
            public class List : ConnectionCommand
            {
                protected override async Task ProcessAsync()
                {
                    var list = await Zdk!.Embedded.Bridge.GetAllUnwrapTokenRequests();

                    WriteInfo("All unwrap token requests:");
                    WriteInfo($"Count: {list.Count}");

                    if (list.Count > 0)
                    {
                        foreach (var request in list.List)
                        {
                            await WriteAsync(request);
                        }
                    }
                }
            }

            [Verb("bridge.unwrap.listByAddress", HelpText = "List all unwrap token requests by NoM address.")]
            public class ListByAddress : ConnectionCommand
            {
                [Value(0, MetaName = "toAddress", Required = true, HelpText = "The NoM address")]
                public string? ToAddress { get; set; }

                protected override async Task ProcessAsync()
                {
                    var toAddress = ParseAddress(ToAddress);

                    var list = await Zdk!.Embedded.Bridge
                        .GetAllUnwrapTokenRequestsByToAddress(toAddress.ToString());

                    if (list.Count > 0)
                    {
                        WriteInfo($"Count: {list.Count}");
                        foreach (var request in list.List)
                        {
                            await WriteAsync(request);
                        }
                    }
                    else
                    {
                        WriteInfo($"No unwrap requests found for {toAddress}");
                    }
                }
            }

            [Verb("bridge.unwrap.listUnredeemed", HelpText = "List all unredeemed unwrap token requests.")]
            public class ListUnredeemed : ConnectionCommand
            {
                [Value(0, MetaName = "toAddress", HelpText = "The NoM address")]
                public string? ToAddress { get; set; }

                protected override async Task ProcessAsync()
                {
                    var allUnwrapRequests =
                        await Zdk!.Embedded.Bridge.GetAllUnwrapTokenRequests();

                    var unredeemed = new List<UnwrapTokenRequest>();

                    foreach (var request in allUnwrapRequests.List)
                    {
                        if (request.Redeemed == 0 && request.Revoked == 0)
                        {
                            if ((ToAddress != null && request.ToAddress == ParseAddress(ToAddress)) ||
                                (ToAddress == null))
                            {
                                unredeemed.Add(request);
                            }
                        }
                    }

                    WriteInfo($"All unredeemed unwrap token requests{(ToAddress != null ? $" for {ToAddress}:" : ":")}");
                    WriteInfo($"Count: {unredeemed.Count}");

                    if (unredeemed.Count > 0)
                    {
                        foreach (var request in unredeemed)
                        {
                            await WriteAsync(request);
                        }
                    }
                }
            }

            [Verb("bridge.unwrap.get", HelpText = "Get unwrap token request by hash and log index.")]
            public class Get : ConnectionCommand
            {
                [Value(0, MetaName = "hash", Required = true, HelpText = "The transaction hash")]
                public string? Hash { get; set; }

                [Value(1, MetaName = "logIndex", Required = true, HelpText = "The log index")]
                public long? LogIndex { get; set; }

                protected override async Task ProcessAsync()
                {
                    var transactionHash = ParseHash(Hash);

                    var request = await Zdk!.Embedded.Bridge
                        .GetUnwrapTokenRequestByHashAndLog(transactionHash, (uint)LogIndex!.Value);

                    await WriteAsync(request);
                }
            }
        }
    }
}
