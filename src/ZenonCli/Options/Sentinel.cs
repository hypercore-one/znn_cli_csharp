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

        [Verb("sentinel.withdrawQsr", HelpText = "")]
        public class WithdrawQsr : KeyStoreAndConnectionOptions
        {
        }
    }
}
