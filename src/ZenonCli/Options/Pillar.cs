using CommandLine;

namespace ZenonCli.Options
{
    public class Pillar
    {
        [Verb("pillar.list", HelpText = "List all pillars")]
        public class List : KeyStoreAndConnectionOptions
        { }

        [Verb("pillar.register", HelpText = "Register pillar")]
        public class Register : KeyStoreAndConnectionOptions
        {
            [Value(0, Required = true, MetaName = "name")]
            public string? Name { get; set; }

            [Value(1, Required = true, MetaName = "producerAddress")]
            public string? ProducerAddress { get; set; }

            [Value(2, Required = true, MetaName = "rewardAddress")]
            public string? RewardAddress { get; set; }

            [Value(3, Required = true, MetaName = "giveBlockRewardPercentage")]
            public int GiveBlockRewardPercentage { get; set; }

            [Value(4, Required = true, MetaName = "giveDelegateRewardPercentage")]
            public int GiveDelegateRewardPercentage { get; set; }
        }

        [Verb("pillar.revoke", HelpText = "Revoke pillar")]
        public class Revoke : KeyStoreAndConnectionOptions
        {
            [Value(0, Required = true, MetaName = "name")]
            public string? Name { get; set; }
        }

        [Verb("pillar.delegate", HelpText = "Delegate to pillar")]
        public class Delegate : KeyStoreAndConnectionOptions
        {
            [Value(0, Required = true, MetaName = "name")]
            public string? Name { get; set; }
        }

        [Verb("pillar.undelegate", HelpText = "Undelegate pillar")]
        public class Undelegate : KeyStoreAndConnectionOptions
        {
        }

        [Verb("pillar.collect", HelpText = "Collect pillar rewards")]
        public class Collect : KeyStoreAndConnectionOptions
        {
        }

        [Verb("pillar.depositQsr", HelpText = "Deposit QSR to the pillar contract")]
        public class DepositQsr : KeyStoreAndConnectionOptions
        {
            [Value(0, Required = true, MetaName = "amount")]
            public long Amount { get; set; }
        }

        [Verb("pillar.withdrawQsr", HelpText = "Withdraw deposited QSR from the pillar contract")]
        public class WithdrawQsr : KeyStoreAndConnectionOptions
        {
        }
    }
}
