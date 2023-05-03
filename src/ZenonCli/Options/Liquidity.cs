using CommandLine;

namespace ZenonCli.Options
{
    public class Liquidity
    {
        [Verb("liquidity.info", HelpText = "Get the liquidity information")]
        public class Info : ConnectionOptions
        { }

        [Verb("liquidity.collect", HelpText = "Collect liquidity rewards")]
        public class Collect : KeyStoreAndConnectionOptions
        {
        }

        [Verb("liquidity.nominate", HelpText = "Nominate liquidity guardians. Can only be called by administrator.")]
        public class NominateGuardians : KeyStoreAndConnectionOptions
        {
            [Value(0, MetaName = "guardians", Required = true)]
            public IEnumerable<string>? Guardians { get; set; }
        }

        [Verb("liquidity.propose", HelpText = "Propose liquidity administrator. Propose bridge administrator. Can only be called by a guardian if the liquidity contract is in emergency mode.")]
        public class ProposeAdministrator : KeyStoreAndConnectionOptions
        {
            [Value(0, MetaName = "admin", Required = true)]
            public string? Administrator { get; set; }
        }

        [Verb("liquidity.change", HelpText = "Change liquidity administrator. Can only be called by administrator.")]
        public class ChangeAdministrator : KeyStoreAndConnectionOptions
        {
            [Value(0, MetaName = "admin", Required = true)]
            public string? Administrator { get; set; }
        }

        [Verb("liquidity.emercency", HelpText = "Put the liquidity contract in emercency mode. Can only be called by administrator.")]
        public class Emercency : KeyStoreAndConnectionOptions
        { }
    }
}
