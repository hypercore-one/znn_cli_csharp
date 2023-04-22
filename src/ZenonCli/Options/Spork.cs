using CommandLine;

namespace ZenonCli.Options
{
    public class Spork
    {
        [Verb("spork.list", HelpText = "List all sporks")]
        public class List : ConnectionOptions
        {
            [Value(0, Default = 0, MetaName = "pageIndex")]
            public int? PageIndex { get; set; }

            [Value(1, Default = 25, MetaName = "pageSize")]
            public int? PageSize { get; set; }
        }

        [Verb("spork.create", HelpText = "Create a new spork")]
        public class Create : KeyStoreAndConnectionOptions
        {
            [Value(0, Required = true, MetaName = "name")]
            public string? Name { get; set; }

            [Value(1, Required = true, MetaName = "description")]
            public string? Description { get; set; }
        }

        [Verb("spork.activate", HelpText = "Activate a spork")]
        public class Activate : KeyStoreAndConnectionOptions
        {
            [Value(0, Required = true, MetaName = "id", HelpText = "The id of the spork to activate.")]
            public string? Id { get; set; }
        }
    }
}
