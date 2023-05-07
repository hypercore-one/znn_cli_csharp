using CommandLine;

namespace ZenonCli.Options
{
    public class Stake
    {
        [Verb("stake.list", HelpText = "List all stakes")]
        public class List : ConnectionOptions
        {
            [Value(0, Default = 0, MetaName = "pageIndex")]
            public int? PageIndex { get; set; }

            [Value(1, Default = 25, MetaName = "PageSize")]
            public int? PageSize { get; set; }
        }

        [Verb("stake.register", HelpText = "Register stake")]
        public class Register : KeyStoreAndConnectionOptions
        {
            [Value(0, Required = true, MetaName = "amount")]
            public long Amount { get; set; }

            [Value(1, Required = true, MetaName = "duration", HelpText = "Duration in months")]
            public long Duration { get; set; }
        }

        [Verb("stake.revoke", HelpText = "Revoke stake")]
        public class Revoke : KeyStoreAndConnectionOptions
        {
            [Value(0, Required = true, MetaName = "id")]
            public string? Id { get; set; }
        }

        [Verb("stake.collect", HelpText = "Collect staking rewards")]
        public class Collect : KeyStoreAndConnectionOptions
        {
        }
    }
}
