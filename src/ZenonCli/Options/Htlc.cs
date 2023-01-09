using CommandLine;

namespace ZenonCli.Options
{
    public class Htlc
    {
        [Verb("htlc.get", HelpText = "List htlc by id")]
        public class Get : KeyStoreAndConnectionOptions
        {
            [Value(0, Required = true, MetaName = "id")]
            public string? Id { get; set; }
        }

        [Verb("htlc.timeLocked", HelpText = "List all time locked htlc's")]
        public class GetByTimeLocked : KeyStoreAndConnectionOptions
        {
            [Value(0, MetaName = "address")]
            public string? Address { get; set; }

            [Value(1, Default = 0, MetaName = "pageIndex")]
            public int? PageIndex { get; set; }

            [Value(2, Default = 25, MetaName = "pageSize")]
            public int? PageSize { get; set; }
        }

        [Verb("htlc.hashLocked", HelpText = "List all hash locked htlc's")]
        public class GetByHashLocked : KeyStoreAndConnectionOptions
        {
            [Value(0, MetaName = "address")]
            public string? Address { get; set; }

            [Value(1, Default = 0, MetaName = "pageIndex")]
            public int? PageIndex { get; set; }

            [Value(2, Default = 25, MetaName = "pageSize")]
            public int? PageSize { get; set; }
        }

        [Verb("htlc.create", HelpText = "Create htlc")]
        public class Create : KeyStoreAndConnectionOptions
        {
            [Value(0, Required = true, MetaName = "hashLockedAddress")]
            public string? HashLockedAddress { get; set; }

            [Value(1, Required = true, MetaName = "tokenStandard", MetaValue = "[ZNN/QSR/ZTS]")]
            public string? TokenStandard { get; set; }

            [Value(2, Required = true, MetaName = "amount")]
            public long Amount { get; set; }

            [Value(3, Required = true, MetaName = "expirationTime", HelpText = "Total seconds from now.")]
            public long ExpirationTime { get; set; }

            [Value(4, MetaName = "hashLock", HelpText = "The hash lock as a hexidecimal string.")]
            public string? HashLock { get; set; }

            [Value(5, MetaName = "hashType", Default = 0, HelpText = "0 = SHA3-256, 1 = SHA-256")]
            public int? HashType { get; set; }
        }

        [Verb("htlc.reclaim", HelpText = "Reclaim htlc")]
        public class Reclaim : KeyStoreAndConnectionOptions
        {
            [Value(0, Required = true, MetaName = "id", HelpText = "The id of the htlc to reclaim.")]
            public string? Id { get; set; }
        }

        [Verb("htlc.reclaimAll", HelpText = "Reclaim all htlc's")]
        public class ReclaimAll : KeyStoreAndConnectionOptions
        { }

        [Verb("htlc.unlock", HelpText = "Unlock htlc")]
        public class Unlock : KeyStoreAndConnectionOptions
        {
            [Value(0, Required = true, MetaName = "id", HelpText = "The id of the htlc to unlock.")]
            public string? Id { get; set; }

            [Value(1, MetaName = "preimage", HelpText = "The preimage as a hexidecimal string.")]
            public string? Preimage { get; set; }
        }

        [Verb("htlc.inspect", HelpText = "Inspect htlc account-block")]
        public class Inspect : ConnectionOptions
        {
            [Value(0, Required = true, MetaName = "blockHash", HelpText = "The hash of a htlc account-block.")]
            public string? BlockHash { get; set; }
        }

        [Verb("htlc.monitor", HelpText = "Monitor htlc by id.")]
        public class Monitor : KeyStoreAndConnectionOptions
        {
            [Value(0, Required = true, MetaName = "id", HelpText = "The id of the htlc to monitor.")]
            public string? Id { get; set; }

            [Value(1, MetaName = "unlock", Default = true, HelpText = "Unlock any associated htlc that are hashLocked to current address.")]
            public bool Unlock { get; set; }
        }

        [Verb("htlc.monitorAll", HelpText = "Monitor all htlc's.")]
        public class MonitorAll : KeyStoreAndConnectionOptions
        {
            [Value(0, MetaName = "unlock", Default = true, HelpText = "Unlock any associated htlc that are hashLocked to current address.")]
            public bool Unlock { get; set; }
        }
    }
}
