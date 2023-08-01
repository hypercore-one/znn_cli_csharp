using CommandLine;

namespace ZenonCli.Commands
{
    public partial class Bridge
    {
        public class Network
        {
            [Verb("bridge.network.list", HelpText = "List all available bridge netwoks.")]
            public class List : ConnectionCommand
            {
                protected override async Task ProcessAsync()
                {
                    var networkList = await ZnnClient.Embedded.Bridge.GetAllNetworks();

                    if (networkList == null || networkList.Count == 0)
                    {
                        WriteInfo("No bridge networks found");
                        return;
                    }

                    foreach (var network in networkList.List)
                    {
                        Write(network);
                    }
                }
            }

            [Verb("bridge.network.get", HelpText = "Get the information for a network class and chain id.")]
            public class Get : ConnectionCommand
            {
                [Value(0, MetaName = "networkClass", Required = true)]
                public int? NetworkClass { get; set; }

                [Value(1, MetaName = "chainId", Required = true)]
                public int? ChainId { get; set; }

                protected override async Task ProcessAsync()
                {
                    if (this.NetworkClass == 0 || this.ChainId == 0)
                    {
                        WriteInfo("The bridge network does not exist");
                        return;
                    }

                    var network = await ZnnClient.Embedded.Bridge.GetNetworkInfo(
                        this.NetworkClass!.Value,
                        this.ChainId!.Value);

                    if (network.NetworkClass == 0 || network.ChainId == 0)
                    {
                        WriteInfo("The bridge network does not exist");
                        return;
                    }

                    Write(network);
                }
            }
        }
    }
}
