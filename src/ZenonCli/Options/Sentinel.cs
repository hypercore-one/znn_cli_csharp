using CommandLine;

namespace ZenonCli.Options
{
    public class Sentinel
    {
        [Verb("sentinel.list", HelpText = "List all sentinels")]
        public class List : ConnectionOptions
        {
        }

        [Verb("sentinel.register", HelpText = "Register a sentinel")]
        public class Register : KeyStoreAndConnectionOptions
        {
        }

        [Verb("sentinel.revoke", HelpText = "Revoke a sentinel")]
        public class Revoke : KeyStoreAndConnectionOptions
        {
        }

        [Verb("sentinel.collect", HelpText = "Collect sentinel rewards")]
        public class Collect : KeyStoreAndConnectionOptions
        {
        }

        [Verb("sentinel.depositQsr", HelpText = "Deposit QSR to the sentinel contract")]
        public class DepositQsr : KeyStoreAndConnectionOptions
        {
            [Value(0, Required = true, MetaName = "amount")]
            public long Amount { get; set; }
        }

        [Verb("sentinel.withdrawQsr", HelpText = "Withdraw deposited QSR from the sentinel contract")]
        public class WithdrawQsr : KeyStoreAndConnectionOptions
        {
        }
    }
}
