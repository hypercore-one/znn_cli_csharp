using CommandLine;

namespace ZenonCli.Options
{
    public class Bridge
    {
        [Verb("bridge.info", HelpText = "Get the bridge info")]
        public class Info : ConnectionOptions
        { }

        [Verb("bridge.orchestratorInfo", HelpText = "Get the orchestrator info")]
        public class OrchestratorInfo : ConnectionOptions
        {
        }

        [Verb("bridge.securityInfo", HelpText = "Get the security info")]
        public class SecurityInfo : ConnectionOptions
        {
        }

        [Verb("bridge.timeChallengesInfo", HelpText = "Get the time challenges info")]
        public class TimeChallengesInfo : ConnectionOptions
        {
        }

        [Verb("bridge.networkInfo", HelpText = "Get network info")]
        public class NetworkInfo : ConnectionOptions
        {
            [Value(0, MetaName = "networkClass", Required = true)]
            public int? NetworkClass { get; set; }

            [Value(1, MetaName = "chainId", Required = true)]
            public int? ChainId { get; set; }
        }

        [Verb("bridge.setNetwork", HelpText = "Set a network")]
        public class SetNetwork : KeyStoreAndConnectionOptions
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
        }

        [Verb("bridge.nominate", HelpText = "Nominate bridge guardians")]
        public class NominateGuardians : KeyStoreAndConnectionOptions
        {
            [Value(0, MetaName = "guardians", Required = true)]
            public IEnumerable<string>? Guardians { get; set; }
        }

        [Verb("bridge.propose", HelpText = "Propose bridge administrator")]
        public class ProposeAdministrator : KeyStoreAndConnectionOptions
        {
            [Value(0, MetaName = "admin", Required = true)]
            public string? Administrator { get; set; }
        }

        [Verb("bridge.change", HelpText = "Change bridge administrator")]
        public class ChangeAdministrator : KeyStoreAndConnectionOptions
        {
            [Value(0, MetaName = "admin", Required = true)]
            public string? Administrator { get; set; }
        }
    }
}
