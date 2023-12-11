using CommandLine;
using Newtonsoft.Json;
using System.Numerics;
using Zenon;
using Zenon.Model.Primitives;

namespace ZenonCli.Commands
{
    public partial class Bridge
    {
        public class Admin
        {
            [Verb("bridge.admin.emergency", HelpText = "Put the bridge contract in emergency mode.")]
            public class Emergency : WalletAndConnectionCommand
            {
                protected override async Task ProcessAsync()
                {
                    await AssertBridgeAdminAsync();

                    WriteInfo("Initializing bridge emergency mode ...");
                    await SendAsync(Zdk!.Embedded.Bridge.Emergency());
                    WriteInfo("Done");
                }
            }

            [Verb("bridge.admin.halt", HelpText = "Halt bridge operations.")]
            public class Halt : WalletAndConnectionCommand
            {
                protected override async Task ProcessAsync()
                {
                    await AssertBridgeAdminAsync();

                    WriteInfo("Halting the bridge ...");
                    await SendAsync(Zdk!.Embedded.Bridge.Halt(""));
                    WriteInfo("Done");
                }
            }

            [Verb("bridge.admin.unhalt", HelpText = "Unhalt bridge operations.")]
            public class Unhalt : WalletAndConnectionCommand
            {
                protected override async Task ProcessAsync()
                {
                    await AssertBridgeAdminAsync();

                    WriteInfo("Unhalting the bridge ...");
                    await SendAsync(Zdk!.Embedded.Bridge.Unhalt());
                    WriteInfo("Done");
                }
            }

            [Verb("bridge.admin.enableKeyGen", HelpText = "Enable bridge key generation.")]
            public class EnableKeyGen : WalletAndConnectionCommand
            {
                protected override async Task ProcessAsync()
                {
                    await AssertBridgeAdminAsync();

                    WriteInfo("Enabling TSS key generation ...");
                    await SendAsync(Zdk!.Embedded.Bridge.SetAllowKeyGen(true));
                    WriteInfo("Done");
                }
            }

            [Verb("bridge.admin.disableKeyGen", HelpText = "Disable bridge key generation.")]
            public class DisableKeyGen : WalletAndConnectionCommand
            {
                protected override async Task ProcessAsync()
                {
                    await AssertBridgeAdminAsync();

                    WriteInfo("Disabling TSS key generation ...");
                    await SendAsync(Zdk!.Embedded.Bridge.SetAllowKeyGen(false));
                    WriteInfo("Done");
                }
            }

            [Verb("bridge.admin.setTokenPair", HelpText = "Set a token pair to enable bridging the asset")]
            public class SetTokenPair : WalletAndConnectionCommand
            {
                [Value(0, MetaName = "networkClass", Required = true)]
                public int? NetworkClass { get; set; }

                [Value(1, MetaName = "chainId", Required = true)]
                public int? ChainId { get; set; }

                [Value(2, MetaName = "tokenStandard", Required = true)]
                public string? TokenStandard { get; set; }

                [Value(3, MetaName = "tokenAddress", Required = true)]
                public string? TokenAddress { get; set; }

                [Value(4, MetaName = "bridgeable", Required = true)]
                public bool? Bridgeable { get; set; }

                [Value(5, MetaName = "redeemable", Required = true)]
                public bool? Redeemable { get; set; }

                [Value(6, MetaName = "owned", Required = true)]
                public bool? Owned { get; set; }

                [Value(7, MetaName = "minAmount", Required = true)]
                public string? MinAmount { get; set; }

                [Value(8, MetaName = "feePercentage", Required = true)]
                public int? FeePercentage { get; set; }

                [Value(9, MetaName = "redeemDelay", Required = true)]
                public long? RedeemDelay { get; set; }

                [Value(10, MetaName = "metadata", Required = true)]
                public string? Metadata { get; set; }

                protected override async Task ProcessAsync()
                {
                    await AssertBridgeAdminAsync();

                    var tokenStandard = ParseTokenStandard(TokenStandard);
                    var feePercentage = FeePercentage!.Value * 100;
                    var minAmount = BigInteger.Parse(MinAmount!);
                    JsonConvert.DeserializeObject(Metadata!);

                    if (feePercentage > Constants.BridgeMaximumFee)
                    {
                        WriteError($"Fee percentage may not exceed {Constants.BridgeMaximumFee / 100}");
                        return;
                    }

                    if (RedeemDelay!.Value == 0)
                    {
                        WriteError("Redeem delay cannot be 0");
                        return;
                    }

                    WriteInfo("Setting token pair ...");

                    var setTokenPair = Zdk!.Embedded.Bridge.SetTokenPair(
                        (uint)NetworkClass!.Value,
                        (uint)ChainId!.Value,
                        tokenStandard,
                        TokenAddress,
                        Bridgeable!.Value,
                        Redeemable!.Value,
                        Owned!.Value,
                        minAmount,
                        feePercentage,
                        (ulong)RedeemDelay!.Value,
                        Metadata);
                    await SendAsync(setTokenPair);

                    WriteInfo("Done");
                }
            }

            [Verb("bridge.admin.removeTokenPair", HelpText = "Remove a token pair to disable bridging the asset")]
            public class RemoveTokenPair : WalletAndConnectionCommand
            {
                [Value(0, MetaName = "networkClass", Required = true)]
                public int? NetworkClass { get; set; }

                [Value(1, MetaName = "chainId", Required = true)]
                public int? ChainId { get; set; }

                [Value(2, MetaName = "tokenStandard", Required = true)]
                public string? TokenStandard { get; set; }

                [Value(3, MetaName = "tokenAddress", Required = true)]
                public string? TokenAddress { get; set; }

                protected override async Task ProcessAsync()
                {
                    await AssertBridgeAdminAsync();

                    var tokenStandard = ParseTokenStandard(TokenStandard);

                    WriteInfo("Removing token pair ...");

                    var removeTokenPair = Zdk!.Embedded.Bridge
                        .RemoveTokenPair((uint)NetworkClass!.Value, (uint)ChainId!.Value, tokenStandard, TokenAddress);
                    await SendAsync(removeTokenPair);

                    WriteInfo("Done");
                }
            }

            [Verb("bridge.admin.revokeUnwrapRequest", HelpText = "Revoke an unwrap request to prevent it from being redeemed.")]
            public class Revoke : WalletAndConnectionCommand
            {
                [Value(0, MetaName = "transactionHash", Required = true, HelpText = "The hash of the transaction")]
                public string? TransactionHash { get; set; }

                [Value(1, MetaName = "logIndex", Required = true, HelpText = "The log index in the block of the transaction that locked/burned the funds")]
                public long? LogIndex { get; set; }

                protected override async Task ProcessAsync()
                {
                    await AssertBridgeAdminAsync();

                    var transactionHash = ParseHash(TransactionHash);

                    WriteInfo("Revoking unwrap request ...");

                    var revokeUnwrapRequest =
                        Zdk!.Embedded.Bridge.RevokeUnwrapRequest(transactionHash, (uint)LogIndex!.Value);
                    await SendAsync(revokeUnwrapRequest);

                    WriteInfo("Done");
                }
            }

            [Verb("bridge.admin.nominateGuardians", HelpText = "Nominate bridge guardians.")]
            public class NominateGuardians : WalletAndConnectionCommand
            {
                [Value(0, MetaName = "addresses", Required = true)]
                public IEnumerable<string>? Addresses { get; set; }

                protected override async Task ProcessAsync()
                {
                    await AssertBridgeAdminAsync();

                    var address = await Zdk!.DefaultWalletAccount.GetAddressAsync();

                    if (this.Addresses == null)
                    {
                        WriteInfo($"No addresses specified");
                        return;
                    }

                    Address[] guardians;

                    try
                    {
                        guardians = this.Addresses!
                            .Select(x => ParseAddress(x))
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

                    var tcList = await Zdk!.Embedded.Bridge
                        .GetTimeChallengesInfo();

                    var tc = tcList.List
                        .Where(x => x.MethodName == "NominateGuardians")
                        .FirstOrDefault();

                    if (tc != null && tc.ParamsHash != Hash.Empty)
                    {
                        var frontierMomentum = await Zdk!.Ledger.GetFrontierMomentum();
                        var secInfo = await Zdk!.Embedded.Bridge.GetSecurityInfo();

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

                    await SendAsync(Zdk!.Embedded.Bridge.NominateGuardians(guardians));
                    WriteInfo("Done");
                }
            }

            [Verb("bridge.admin.changeAdmin", HelpText = "Change bridge administrator.")]
            public class ChangeAdministrator : WalletAndConnectionCommand
            {
                [Value(0, MetaName = "address", Required = true)]
                public string? Address { get; set; }

                protected override async Task ProcessAsync()
                {
                    await AssertBridgeAdminAsync();

                    var address = await Zdk!.DefaultWalletAccount.GetAddressAsync();
                    var newAdmin = ParseAddress(this.Address);

                    await AssertUserAddressAsync(newAdmin);

                    if (address == newAdmin)
                    {
                        WriteInfo("The specified address is already bridge administrator");
                        return;
                    }

                    if (!Confirm($"Are you sure you want to change the bridge administrator to {newAdmin}"))
                        return;

                    WriteInfo("Changing bridge administrator...");
                    await SendAsync(Zdk!.Embedded.Bridge.ChangeAdministrator(newAdmin));
                    WriteInfo("Done");
                }
            }

            [Verb("bridge.admin.setMetadata", HelpText = "Set the bridge metadata.")]
            public class SetMetadata : WalletAndConnectionCommand
            {
                [Value(0, MetaName = "metadata")]
                public string? Metadata { get; set; }

                protected override async Task ProcessAsync()
                {
                    await AssertBridgeAdminAsync();

                    JsonConvert.DeserializeObject(Metadata!);

                    WriteInfo("Setting bridge metadata ...");
                    await SendAsync(Zdk!.Embedded.Bridge.SetBridgeMetadata(Metadata!));
                    WriteInfo("Done");
                }
            }

            [Verb("bridge.admin.setOrchestratorInfo", HelpText = "Get the bridge information.")]
            public class SetOrchestratorInfo : WalletAndConnectionCommand
            {
                [Value(0, MetaName = "windowSize", Required = true, HelpText = "Size in momentums of a window used in the orchestrator to determine which signing ceremony should occur, wrap or unwrap request and to determine the key sign ceremony timeout")]
                public long? WindowSize { get; set; }

                [Value(1, MetaName = "keyGenThreshold", Required = true, HelpText = "Minimum number of participants of a key generation ceremony")]
                public int? KeyGenThreshold { get; set; }

                [Value(2, MetaName = "confirmationsToFinality", Required = true, HelpText = "Minimum number of momentums to consider a wrap request confirmed")]
                public int? ConfirmationsToFinality { get; set; }

                [Value(3, MetaName = "estimatedMomentumTime", Required = true, HelpText = "Time in seconds between momentums")]
                public int? EstimatedMomentumTime { get; set; }

                protected override async Task ProcessAsync()
                {
                    await AssertBridgeAdminAsync();

                    WriteInfo("Setting orchestrator information...");
                    await SendAsync(Zdk!.Embedded.Bridge.SetOrchestratorInfo((ulong)WindowSize!.Value,
                        (uint)KeyGenThreshold!.Value,
                        (uint)ConfirmationsToFinality!.Value,
                        (uint)EstimatedMomentumTime!.Value));
                    WriteInfo("Done");
                }
            }

            [Verb("bridge.admin.setNetwork", HelpText = "Configure network parameters to allow bridging.")]
            public class SetNetwork : WalletAndConnectionCommand
            {
                [Value(0, MetaName = "networkClass", Required = true)]
                public int? NetworkClass { get; set; }

                [Value(1, MetaName = "chainId", Required = true)]
                public int? ChainId { get; set; }

                [Value(2, MetaName = "name", Required = true)]
                public string? Name { get; set; }

                [Value(3, MetaName = "contractAddress", Required = true)]
                public string? ContractAddress { get; set; }

                [Value(4, MetaName = "metadata")]
                public string? Metadata { get; set; }

                protected override async Task ProcessAsync()
                {
                    await AssertBridgeAdminAsync();

                    var networkClass = NetworkClass!.Value;
                    var chainId = ChainId!.Value;
                    var name = Name!;
                    var contractAddress = ContractAddress!;

                    if (networkClass == 0)
                    {
                        WriteInfo($"The bridge network class cannot be zero");
                        return;
                    }

                    if (chainId == 0)
                    {
                        WriteInfo($"The bridge network chain id cannot be zero");
                        return;
                    }

                    if (string.IsNullOrEmpty(name))
                    {
                        WriteInfo($"The bridge network name cannot be empty");
                        return;
                    }

                    if (string.IsNullOrEmpty(contractAddress))
                    {
                        WriteInfo($"The bridge network contract address cannot be empty");
                        return;
                    }

                    JsonConvert.DeserializeObject(Metadata!);

                    WriteInfo("Setting bridge network...");
                    await SendAsync(Zdk!.Embedded.Bridge.SetNetwork(
                        (uint)networkClass, (uint)chainId, name, contractAddress, Metadata));
                    WriteInfo("Done");
                }
            }

            [Verb("bridge.admin.removeNetwork", HelpText = "Remove a network to disable bridging.")]
            public class Remove : WalletAndConnectionCommand
            {
                [Value(0, MetaName = "networkClass", Required = true)]
                public int? NetworkClass { get; set; }

                [Value(1, MetaName = "chainId", Required = true)]
                public int? ChainId { get; set; }

                protected override async Task ProcessAsync()
                {
                    await AssertBridgeAdminAsync();

                    var networkClass = NetworkClass!.Value;
                    var chainId = ChainId!.Value;

                    if (networkClass == 0)
                    {
                        WriteInfo($"The bridge network class cannot be zero");
                        return;
                    }

                    if (chainId == 0)
                    {
                        WriteInfo($"The bridge network chain id cannot be zero");
                        return;
                    }


                    WriteInfo("Removing bridge network...");
                    await SendAsync(Zdk!.Embedded.Bridge.RemoveNetwork((uint)networkClass, (uint)chainId));
                    WriteInfo("Done");
                }
            }

            [Verb("bridge.admin.setNetworkMetadata", HelpText = "Set network metadata.")]
            public class SetNetworkMetadata : WalletAndConnectionCommand
            {
                [Value(0, MetaName = "networkClass", Required = true)]
                public int? NetworkClass { get; set; }

                [Value(1, MetaName = "chainId", Required = true)]
                public int? ChainId { get; set; }

                [Value(2, MetaName = "metadata")]
                public string? Metadata { get; set; }

                protected override async Task ProcessAsync()
                {
                    await AssertBridgeAdminAsync();

                    var networkClass = NetworkClass!.Value;
                    var chainId = ChainId!.Value;

                    if (networkClass == 0)
                    {
                        WriteInfo($"The bridge network class cannot be zero");
                        return;
                    }

                    if (chainId == 0)
                    {
                        WriteInfo($"The bridge network chain id cannot be zero");
                        return;
                    }

                    JsonConvert.DeserializeObject(Metadata!);

                    WriteInfo("Setting bridge network metadata...");
                    await SendAsync(Zdk!.Embedded.Bridge.SetNetworkMetadata((uint)networkClass, (uint)chainId, Metadata!));
                    WriteInfo("Done");
                }
            }
        }
    }
}
