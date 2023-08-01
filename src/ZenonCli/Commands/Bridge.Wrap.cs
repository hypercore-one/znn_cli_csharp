using CommandLine;
using Zenon.Model.Embedded;

namespace ZenonCli.Commands
{
    public partial class Bridge
    {
        public class Wrap
        {
            [Verb("bridge.wrap.token", HelpText = "Wrap assets for an EVM-compatible network.")]
            public class Token : KeyStoreAndConnectionCommand
            {
                [Value(0, MetaName = "networkClass", Required = true, HelpText = "The class of the destination network")]
                public int? NetworkClass { get; set; }

                [Value(1, MetaName = "chainId", Required = true, HelpText = "The chain identifier of the destination network")]
                public int? ChainId { get; set; }

                [Value(2, MetaName = "toAddress", Required = true, HelpText = "The address that can redeem the funds on the destination network")]
                public string? ToAddress { get; set; }

                [Value(3, MetaName = "amount", Required = true, HelpText = "The amount used in the send block")]
                public string? Amount { get; set; }

                [Value(4, MetaName = "tokenStandard", Required = true, HelpText = "The ZTS used in the send block")]
                public string? TokenStandard { get; set; }

                protected override async Task ProcessAsync()
                {
                    var address = ZnnClient.DefaultKeyPair.Address;
                    var tokenStandard = ParseTokenStandard(this.TokenStandard);
                    var token = await GetTokenAsync(tokenStandard);
                    var amount = ParseAmount(this.Amount!, token.Decimals);

                    if (amount <= 0)
                    {
                        WriteError($"amount must be greater than 0");
                        return;
                    }

                    await AssertBalanceAsync(address, tokenStandard, amount);

                    var info =
                        await ZnnClient.Embedded.Bridge.GetNetworkInfo(this.NetworkClass!.Value, this.ChainId!.Value);

                    if (info.NetworkClass == 0 || info.ChainId == 0)
                    {
                        WriteError("The bridge network does not exist");
                        return;
                    }

                    var tokenPair = info.TokenPairs.FirstOrDefault(x => x.TokenStandard == tokenStandard);

                    if (tokenPair != null)
                    {
                        WriteError("That token cannot be wrapped");
                        return;
                    }

                    if (amount < tokenPair!.MinAmount)
                    {
                        WriteError($"Invalid amount. Must be at least {FormatAmount(tokenPair.MinAmount, token.Decimals)} ${token.Symbol}");
                        return;
                    }

                    WriteInfo("Wrapping token ...");
                    var wrapToken = ZnnClient.Embedded.Bridge
                        .WrapToken(NetworkClass!.Value, ChainId!.Value, ToAddress, amount, tokenStandard);
                    await ZnnClient.Send(wrapToken);
                    WriteInfo("Done");
                }
            }

            [Verb("bridge.wrap.list", HelpText = "List all wrap token requests.")]
            public class List : ConnectionCommand
            {
                protected override async Task ProcessAsync()
                {
                    var list = await ZnnClient.Embedded.Bridge.GetAllWrapTokenRequests();
                    WriteInfo("All wrap token requests:");
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

            [Verb("bridge.wrap.listByAddress", HelpText = "List all wrap token requests made by EVM address.")]
            public class ListByAddress : ConnectionCommand
            {
                [Value(0, MetaName = "Address", Required = true, HelpText = "The address")]
                public string? Address { get; set; }

                [Value(1, MetaName = "networkClass", HelpText = "The class of the network")]
                public int? NetworkClass { get; set; }

                [Value(2, MetaName = "chainId", HelpText = "The chain identifier of the network")]
                public int? ChainId { get; set; }

                protected override async Task ProcessAsync()
                {
                    WrapTokenRequestList list;

                    if (this.NetworkClass.HasValue && this.ChainId.HasValue)
                    {
                        list = await ZnnClient.Embedded.Bridge
                            .GetAllWrapTokenRequestsByToAddressNetworkClassAndChainId(
                                Address, NetworkClass!.Value, ChainId!.Value);
                    }
                    else
                    {
                        list = await ZnnClient.Embedded.Bridge
                            .GetAllWrapTokenRequestsByToAddress(Address);
                    }

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
                        WriteInfo($"No wrap requests found for {Address}");
                    }
                }
            }

            [Verb("bridge.wrap.listUnsigned", HelpText = "List all unsigned wrap token requests.")]
            public class ListUnsigned : ConnectionCommand
            {
                protected override async Task ProcessAsync()
                {
                    var list = await ZnnClient.Embedded.Bridge
                        .GetAllUnsignedWrapTokenRequests();

                    WriteInfo("All unsigned wrap token requests:");
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

            [Verb("bridge.wrap.get", HelpText = "Get wrap token request by id.")]
            public class Get : ConnectionCommand
            {
                [Value(0, MetaName = "id", Required = true)]
                public string? Id { get; set; }

                protected override async Task ProcessAsync()
                {
                    var id = ParseHash(this.Id, "id");

                    var request =
                        await ZnnClient.Embedded.Bridge.GetWrapTokenRequestById(id);

                    await WriteAsync(request);
                }
            }
        }
    }
}
