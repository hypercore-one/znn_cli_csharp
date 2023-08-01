using CommandLine;
using Newtonsoft.Json;
using Zenon;

namespace ZenonCli.Commands
{
    public partial class Bridge
    {
        [Verb("bridge.info", HelpText = "Get the bridge information.")]
        public class Info : ConnectionCommand
        {
            protected override async Task ProcessAsync()
            {
                var info = await ZnnClient.Embedded.Bridge.GetBridgeInfo();

                WriteInfo($"Bridge info:");
                WriteInfo($"   Admnistrator: {info.Administrator}");
                WriteInfo($"   Compressed TSS ECDSA public key: {info.CompressedTssECDSAPubKey}");
                WriteInfo($"   Decompressed TSS ECDSA public key: {info.DecompressedTssECDSAPubKey}");
                WriteInfo($"   Allow key generation: {info.AllowKeyGen}");
                WriteInfo($"   Is halted: {info.Halted}");
                WriteInfo($"   Unhalted at: {info.UnhaltedAt}");
                WriteInfo($"   Unhalt duration in momentums: {info.UnhaltDurationInMomentums}");
                WriteInfo($"   TSS nonce: {info.TssNonce}");
                WriteInfo($"   Metadata:");
                dynamic? metadata = JsonConvert.DeserializeObject(info.Metadata);
                if (metadata != null)
                {
                    WriteInfo($"      Party Timeout: {metadata.partyTimeout}");
                    WriteInfo($"      KeyGen Timeout: {metadata.keyGenTimeout}");
                    WriteInfo($"      KeySign Timeout: {metadata.keySignTimeout}");
                    WriteInfo($"      PreParam Timeout: {metadata.preParamTimeout}");
                    WriteInfo($"      KeyGen Version: {metadata.keyGenVersion}");
                    WriteInfo($"      Leader Block Height: {metadata.leaderBlockHeight}");
                    WriteInfo($"      Affiliate Program: {metadata.affiliateProgram}");
                }
            }
        }

        [Verb("bridge.security", HelpText = "Get the bridge security information.")]
        public class SecurityInfo : ConnectionCommand
        {
            protected override async Task ProcessAsync()
            {
                var info = await ZnnClient.Embedded.Bridge.GetSecurityInfo();

                WriteInfo($"Security info:");

                if (info.Guardians == null || info.Guardians.Length == 0)
                {
                    WriteInfo($"   Guardians: none");
                }
                else
                {
                    WriteInfo($"   Guardians: ");

                    foreach (var guardian in info.Guardians)
                    {
                        WriteInfo($"      {guardian}");
                    }
                }

                if (info.GuardiansVotes == null || info.GuardiansVotes.Length == 0)
                {
                    WriteInfo($"   Guardian votes: none");
                }
                else
                {
                    WriteInfo($"   Guardian votes: ");

                    foreach (var guardianVote in info.GuardiansVotes)
                    {
                        WriteInfo($"      {guardianVote}");
                    }
                }

                WriteInfo($"   Administrator delay: {info.AdministratorDelay}");
                WriteInfo($"   Soft delay: {info.SoftDelay}");
            }
        }

        [Verb("bridge.timeChallenges", HelpText = "List all bridge time challenges.")]
        public class TimeChallengesInfo : ConnectionCommand
        {
            protected override async Task ProcessAsync()
            {
                var list = await ZnnClient.Embedded.Bridge.GetTimeChallengesInfo();

                if (list == null || list.Count == 0)
                {
                    WriteInfo("No time challenges found.");
                    return;
                }

                WriteInfo($"Time challenges:");

                foreach (var info in list.List)
                {
                    WriteInfo($"   Method: {info.MethodName}");
                    WriteInfo($"   Start height: {info.ChallengeStartHeight}");
                    WriteInfo($"   Params hash: {info.ParamsHash}");
                    WriteInfo("");
                }
            }
        }

        [Verb("bridge.orchestratorInfo", HelpText = "Get the orchestrator information.")]
        public class OrchestratorInfo : ConnectionCommand
        {
            protected override async Task ProcessAsync()
            {
                var info = await ZnnClient.Embedded.Bridge.GetOrchestratorInfo();

                WriteInfo($"Orchestrator info:");
                WriteInfo($"   Window size: {info.WindowSize}");
                WriteInfo($"   Key generation threshold: {info.KeyGenThreshold}");
                WriteInfo($"   Confirmations to finality: {info.ConfirmationsToFinality}");
                WriteInfo($"   Estimated momentum time: {info.EstimatedMomentumTime}");
                WriteInfo($"   Allow key generation height: {info.AllowKeyGenHeight}");
            }
        }

        [Verb("bridge.fees", HelpText = "Display the accumulated wrapping fees for a ZTS.")]
        public class Fees : ConnectionCommand
        {
            [Value(0, MetaName = "tokenStandard", MetaValue = "[ZNN/QSR/ZTS]")]
            public string? TokenStandard { get; set; }

            protected override async Task ProcessAsync()
            {
                if (this.TokenStandard != null)
                {
                    var tokenStandard = ParseTokenStandard(this.TokenStandard);
                    var token = await GetTokenAsync(tokenStandard);
                    var info = await ZnnClient.Embedded.Bridge.GetFeeTokenPair(tokenStandard);

                    WriteInfo($"Fees accumulated for {token.Symbol}: ${FormatAmount(info.AccumulatedFee, token.Decimals)}");
                }
                else
                {
                    var znnInfo =
                        await ZnnClient.Embedded.Bridge.GetFeeTokenPair(Zenon.Model.Primitives.TokenStandard.ZnnZts);
                    var qsrInfo =
                        await ZnnClient.Embedded.Bridge.GetFeeTokenPair(Zenon.Model.Primitives.TokenStandard.QsrZts);

                    WriteInfo($"Fees accumulated for ZNN: ${FormatAmount(znnInfo.AccumulatedFee, Constants.CoinDecimals)}");

                    WriteInfo($"Fees accumulated for QSR: ${FormatAmount(qsrInfo.AccumulatedFee, Constants.CoinDecimals)}");
                }
            }
        }
    }
}
