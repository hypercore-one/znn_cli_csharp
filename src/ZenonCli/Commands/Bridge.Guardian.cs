using CommandLine;
using Zenon;

namespace ZenonCli.Commands
{
    public partial class Bridge
    {
        public class Guardian
        {
            [Verb("bridge.guardian.proposeAdmin", 
                HelpText = "Participate in a vote to elect a new bridge administrator when the bridge is in Emergency mode.")]
            public class ProposeAdministrator : KeyStoreAndConnectionCommand
            {
                [Value(0, MetaName = "address", Required = true)]
                public string? Address { get; set; }

                protected override async Task ProcessAsync()
                {
                    if (!await AssertBridgeGuardianAsync())
                        return;

                    var newAdmin = ParseAddress(this.Address);

                    if (!await AssertUserAddressAsync(newAdmin))
                        return;

                    var currentAdmin = (await Znn.Instance.Embedded.Bridge.GetBridgeInfo()).Administrator;

                    if (currentAdmin == Zenon.Model.Primitives.Address.EmptyAddress)
                    {
                        WriteInfo("Proposing new Bridge administrator ...");
                        var block =
                            Znn.Instance.Embedded.Bridge.ProposeAdministrator(newAdmin);
                        await Znn.Instance.Send(block);
                        WriteInfo("Done");
                    }
                    else
                    {
                        WriteDenied("Bridge contract is not in emergency mode");
                    }
                }
            }
        }
    }
}
