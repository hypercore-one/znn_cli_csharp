using CommandLine;
using Cryptography.ECDSA;
using Zenon.Model.Primitives;
using Zenon.Utils;

namespace ZenonCli.Commands
{
    public class Orchestrator
    {
        [Verb("orchestrator.changePubKey", HelpText = "Change bridge TSS ECDSA public key. Can only be called by the administrator.")]
        public class ChangePubKey : WalletAndConnectionCommand
        {
            [Value(0, MetaName = "pubKey", Required = true)]
            public string? PubKey { get; set; }

            protected override async Task ProcessAsync()
            {
                await AssertBridgeAdminAsync();

                var tcList = await Zdk!.Embedded.Bridge
                    .GetTimeChallengesInfo();

                var tc = tcList.List
                    .Where(x => x.MethodName == "ChangeTssECDSAPubKey")
                    .FirstOrDefault();

                if (tc != null && tc.ParamsHash != Hash.Empty)
                {
                    var frontierMomentum = await Zdk!.Ledger.GetFrontierMomentum();
                    var secInfo = await Zdk!.Embedded.Bridge.GetSecurityInfo();

                    if (tc.ChallengeStartHeight + secInfo.AdministratorDelay > frontierMomentum.Height)
                    {
                        WriteError($"Cannot change public key; wait for time challenge to expire.");
                        return;
                    }

                    var decompressedPublicKey =
                        Secp256K1Manager.PublicKeyDecompress(BytesUtils.FromBase64String(this.PubKey));

                    var paramsHash = Hash.Digest(decompressedPublicKey);

                    if (tc.ParamsHash == paramsHash)
                    {
                        WriteInfo("Committing public key ...");
                    }
                    else
                    {
                        WriteWarning("Hash does not match the changed public key");

                        if (!Confirm($"Are you sure you want to change the public key to {this.PubKey}?"))
                            return;

                        WriteInfo("Changing public key ...");
                    }
                }
                else
                {
                    WriteInfo("Changing public key...");
                }

                await SendAsync(Zdk!.Embedded.Bridge.ChangeTssECDSAPubKey(this.PubKey, "", ""));
                WriteInfo("Done");
            }
        }

        [Verb("orchestrator.haltBridge", HelpText = "Halt bridge operations.")]
        public class HaltBridge : WalletAndConnectionCommand
        {
            [Value(0, MetaName = "signature", HelpText = "Only non administrators needs a TSS signature with the current tssNonce.")]
            public string? Signature { get; set; }

            protected override async Task ProcessAsync()
            {
                if (Signature == null)
                {
                    await AssertBridgeAdminAsync();
                }

                WriteInfo("Halting bridge operations ...");
                await SendAsync(Zdk!.Embedded.Bridge.Halt(Signature));
                WriteInfo("Done");
            }
        }

        [Verb("orchestrator.updateWrapRequest", HelpText = "Update wrap token request.")]
        public class UpdateWrapRequest : WalletAndConnectionCommand
        {
            [Value(0, MetaName = "id", Required = true)]
            public string? Id { get; set; }

            [Value(0, MetaName = "signature", Required = true,
                HelpText = "The base64 encoded ECDSA signature used to redeem funds on the destination network.")]
            public string? Signature { get; set; }

            protected override async Task ProcessAsync()
            {
                await Task.Run(() => throw new NotSupportedException());
            }
        }

        [Verb("orchestrator.unwrapToken", HelpText = "Unwrap assets.")]
        public class UnwrapToken : WalletAndConnectionCommand
        {
            [Value(0, MetaName = "networkClass", Required = true, HelpText = "The class of the source network")]
            public int? NetworkClass { get; set; }

            [Value(1, MetaName = "chainId", Required = true, HelpText = "The chain identifier of the source network")]
            public int? ChainId { get; set; }

            [Value(2, MetaName = "transactionHash", Required = true, HelpText = "The hash of the transaction on the source network")]
            public string? TransactionHash { get; set; }

            [Value(3, MetaName = "logIndex", Required = true, HelpText = "The log index in the block of the transaction that locked/burned the funds on the source network; together with txHash it creates a unique identifier for a transaction")]
            public long? LogIndex { get; set; }

            [Value(4, MetaName = "address", Required = true, HelpText = "The destination NoM address")]
            public string? Address { get; set; }

            [Value(5, MetaName = "tokenStandard", Required = true, HelpText = "The address of the locked/burned token on the source network")]
            public string? TokenStandard { get; set; }

            [Value(6, MetaName = "amount", Required = true, HelpText = "The amount of token that was locked/burned")]
            public long? Amount { get; set; }

            [Value(7, MetaName = "signature", Required = true, HelpText = "The signature as base64 encoded string of the unwrap request")]
            public string? Signature { get; set; }

            protected override async Task ProcessAsync()
            {
                await Task.Run(() => throw new NotSupportedException());
            }
        }
    }
}
