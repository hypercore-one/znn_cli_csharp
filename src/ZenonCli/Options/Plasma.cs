using CommandLine;

namespace ZenonCli.Options
{
    public class Plasma
    {
        [Verb("plasma.list", HelpText = "List plasma fusion entries.")]
        public class List : KeyStoreAndConnectionOptions
        {
            [Value(0, Default = 0, MetaName = "pageIndex")]
            public int? PageIndex { get; set; }

            [Value(1, Default = 25, MetaName = "PageSize")]
            public int? PageSize { get; set; }
        }

        [Verb("plasma.get")]
        public class Get : KeyStoreAndConnectionOptions
        { }

        [Verb("plasma.fuse", HelpText = "Fuse QSR to an address to generate plasma.")]
        public class Fuse : KeyStoreAndConnectionOptions
        {
            [Value(0, Required = true, MetaName = "toAddress")]
            public string? ToAddress { get; set; }

            [Value(1, Required = true, MetaName = "amount")]
            public long Amount { get; set; }
        }

        [Verb("plasma.cancel", HelpText = "")]
        public class Cancel : KeyStoreAndConnectionOptions
        {
            [Value(0, Required = true, MetaName = "id")]
            public string? Id { get; set; }
        }
    }
}
