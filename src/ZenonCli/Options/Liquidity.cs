using CommandLine;

namespace ZenonCli.Options
{
    public class Liquidity
    {
        [Verb("liquidity.info", HelpText = "Get the liquidity info")]
        public class Info : ConnectionOptions
        { }

        [Verb("liquidity.collect", HelpText = "Collect liquidity rewards")]
        public class Collect : KeyStoreAndConnectionOptions
        {
        }

        [Verb("liquidity.nominate", HelpText = "Nominate liquidity guardians")]
        public class NominateGuardians : KeyStoreAndConnectionOptions
        {
            [Value(0, MetaName = "guardians", Required = true)]
            public IEnumerable<string>? Guardians { get; set; }
        }

        [Verb("liquidity.propose", HelpText = "Propose liquidity administrator")]
        public class ProposeAdministrator : KeyStoreAndConnectionOptions
        {
            [Value(0, MetaName = "admin", Required = true)]
            public string? Administrator { get; set; }
        }

        [Verb("liquidity.change", HelpText = "Change liquidity administrator")]
        public class ChangeAdministrator : KeyStoreAndConnectionOptions
        {
            [Value(0, MetaName = "admin", Required = true)]
            public string? Administrator { get; set; }
        }
    }
}
