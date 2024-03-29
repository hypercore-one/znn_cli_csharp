﻿using CommandLine;

namespace ZenonCli.Commands
{
    public partial class Liquidity
    {
        public class Guardian
        {
            [Verb("liquidity.guardian.proposeAdmin",
                HelpText = "Participate in a vote to elect a new liquidity administrator when the contract is in emergency mode")]
            public class ProposeAdmin : WalletAndConnectionCommand
            {
                [Value(0, MetaName = "address", Required = true)]
                public string? Address { get; set; }

                protected override async Task ProcessAsync()
                {
                    await AssertLiquidityGuardianAsync();

                    var newAdmin = ParseAddress(Address);

                    await AssertUserAddressAsync(newAdmin);

                    var currentAdmin = (await Zdk!.Embedded.Liquidity.GetLiquidityInfo()).Administrator;

                    if (currentAdmin == Zenon.Model.Primitives.Address.EmptyAddress)
                    {
                        WriteInfo("Proposing new liquidity administrator ...");
                        var block =
                            Zdk!.Embedded.Liquidity.ProposeAdministrator(newAdmin);
                        await SendAsync(block);
                        WriteInfo("Done");
                    }
                    else
                    {
                        WriteDenied("Liquidity contract is not in emergency mode");
                    }
                }
            }
        }
    }
}