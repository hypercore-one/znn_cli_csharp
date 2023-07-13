using CommandLine;
using Newtonsoft.Json;
using Zenon;
using Zenon.Model.Primitives;

namespace ZenonCli.Commands
{
    public partial class Liquidity
    {
        public class Admin
        {
            [Verb("liquidity.admin.emergency", HelpText = "Put the liquidity contract in emergency mode.")]
            public class Emergency : KeyStoreAndConnectionCommand
            {
                protected override async Task ProcessAsync()
                {
                    if (!await AssertLiquidityAdminAsync())
                        return;

                    WriteInfo("Initializing liquidity emergency mode ...");
                    await ZnnClient.Send(ZnnClient.Embedded.Liquidity.Emergency());
                    WriteInfo("Done");
                }
            }

            [Verb("liquidity.admin.halt", HelpText = "Halt liquidity operations.")]
            public class Halt : KeyStoreAndConnectionCommand
            {
                protected override async Task ProcessAsync()
                {
                    if (!await AssertLiquidityAdminAsync())
                        return;

                    WriteInfo("Halting the liquidity ...");
                    await ZnnClient.Send(ZnnClient.Embedded.Liquidity.SetIsHalted(true));
                    WriteInfo("Done");
                }
            }

            [Verb("liquidity.admin.unhalt", HelpText = "Unhalt liquidity operations.")]
            public class Unhalt : KeyStoreAndConnectionCommand
            {
                protected override async Task ProcessAsync()
                {
                    if (!await AssertLiquidityAdminAsync())
                        return;

                    WriteInfo("Unhalting the liquidity ...");
                    await ZnnClient.Send(ZnnClient.Embedded.Liquidity.SetIsHalted(false));
                    WriteInfo("Done");
                }
            }

            [Verb("liquidity.admin.changeAdmin", HelpText = "Change liquidity administrator.")]
            public class ChangeAdmin : KeyStoreAndConnectionCommand
            {
                [Value(0, MetaName = "address", Required = true)]
                public string? Address { get; set; }

                protected override async Task ProcessAsync()
                {
                    if (!await AssertLiquidityAdminAsync())
                        return;

                    var address = ZnnClient.DefaultKeyPair.Address;
                    var newAdmin = ParseAddress(this.Address);

                    if (!await AssertUserAddressAsync(newAdmin))
                        return;

                    if (address == newAdmin)
                    {
                        WriteInfo("The specified address is already liquidity administrator");
                        return;
                    }

                    if (!Confirm($"Are you sure you want to change the liquidity administrator to {newAdmin}"))
                        return;

                    WriteInfo("Changing liquidity administrator...");
                    await ZnnClient.Send(ZnnClient.Embedded.Liquidity.ChangeAdministrator(newAdmin));
                    WriteInfo("Done");
                }
            }

            [Verb("liquidity.admin.nominateGuardians", HelpText = "Nominate liquidity guardians.")]
            public class NominateGuardians : KeyStoreAndConnectionCommand
            {
                [Value(0, MetaName = "addresses", Required = true)]
                public IEnumerable<string>? Addresses { get; set; }

                protected override async Task ProcessAsync()
                {
                    if (!await AssertLiquidityAdminAsync())
                        return;

                    var address = ZnnClient.DefaultKeyPair.Address;

                    if (this.Addresses == null)
                    {
                        WriteInfo($"No addresses specified");
                        return;
                    }

                    Address[] guardians;

                    try
                    {
                        guardians = this.Addresses!
                            .Select(x => Address.Parse(x))
                            .Where(x => x != Address.EmptyAddress || x.IsEmbedded)
                            .Distinct()
                            .OrderBy(x => x.ToString())
                            .ToArray();
                    }
                    catch
                    {
                        WriteError($"Failed to parse addresses");
                        return;
                    }

                    if (guardians.Count() < Constants.BridgeMinGuardians)
                    {
                        WriteInfo($"Expected at least {Constants.BridgeMinGuardians} distinct user addresses");
                        return;
                    }

                    var tcList = await ZnnClient.Embedded.Liquidity
                        .GetTimeChallengesInfo();

                    var tc = tcList.List
                        .Where(x => x.MethodName == "NominateGuardians")
                        .FirstOrDefault();

                    if (tc != null && tc.ParamsHash != Hash.Empty)
                    {
                        var frontierMomentum = await ZnnClient.Ledger.GetFrontierMomentum();
                        var secInfo = await ZnnClient.Embedded.Liquidity.GetSecurityInfo();

                        if (tc.ChallengeStartHeight + secInfo.AdministratorDelay > frontierMomentum.Height)
                        {
                            WriteError($"Cannot nominate guardians; wait for time challenge to expire.");
                            return;
                        }

                        var paramsHash = Hash.Digest(Helper.Combine(guardians.Select(x => x.Bytes).ToArray()));

                        if (tc.ParamsHash == paramsHash)
                        {
                            WriteInfo("Committing guardians ...");
                        }
                        else
                        {
                            WriteWarning("Time challenge hash does not match nominated guardians");

                            if (!Confirm("Are you sure you want to nominate new guardians?"))
                                return;

                            WriteInfo("Nominating guardians ...");
                        }
                    }
                    else
                    {
                        WriteInfo("Nominating guardians ...");
                    }

                    await ZnnClient.Send(ZnnClient.Embedded.Liquidity.NominateGuardians(guardians));
                    WriteInfo("Done");
                }
            }

            [Verb("liquidity.admin.unlockStakeEntries", HelpText = "Allows all staked entries to be cancelled immediately.")]
            public class UnlockStakeEntries : KeyStoreAndConnectionCommand
            {
                protected override async Task ProcessAsync()
                {
                    var block =
                        ZnnClient.Embedded.Liquidity.UnlockLiquidityStakeEntries(TokenStandard.Parse("zts17d6yr02kh0r9qr566p7tg6"));
                    block = await ZnnClient.Send(block);
                    WriteInfo(JsonConvert.SerializeObject(block.ToJson(), Formatting.Indented));
                }
            }

            [Verb("liquidity.admin.setAdditionalReward", HelpText = "Set additional liquidity reward percentages.")]
            public class SetAdditionalReward : KeyStoreAndConnectionCommand
            {
                [Value(0, Required = true, MetaName = "znnRweard")]
                public long ZnnReward { get; set; }

                [Value(1, Required = true, MetaName = "qsrReward")]
                public long QsrReward { get; set; }

                protected override async Task ProcessAsync()
                {
                    if (!await AssertLiquidityAdminAsync())
                        return;

                    WriteInfo("Setting additional liquidity reward ...");
                    var block = ZnnClient.Embedded.Liquidity.SetAdditionalReward(this.ZnnReward, this.QsrReward);
                    await ZnnClient.Send(block);
                    WriteInfo("Done");
                }
            }

            [Verb("liquidity.admin.setTokenTuple", HelpText = "Configure token tuples that can be staked.")]
            public class SetTokenTuple : KeyStoreAndConnectionCommand
            {
                protected override async Task ProcessAsync()
                {
                    await Task.Run(() => throw new NotSupportedException());
                }
            }
        }
    }
}
