using CommandLine;

namespace ZenonCli.Options
{
    public class Token
    {
        [Verb("token.list", HelpText = "List all tokens")]
        public class List : ConnectionOptions
        {
            [Value(0, Default = 0, MetaName = "pageIndex")]
            public int? PageIndex { get; set; }

            [Value(1, Default = 25, MetaName = "PageSize")]
            public int? PageSize { get; set; }
        }

        [Verb("token.getByStandard", HelpText = "List tokens by standard")]
        public class GetByStandard : ConnectionOptions
        {
            [Value(0, Required = true, MetaName = "tokenStandard")]
            public string? TokenStandard { get; set; }
        }

        [Verb("token.getByOwner", HelpText = "List tokens by owner")]
        public class GetByOwner : ConnectionOptions
        {
            [Value(0, Required = true, MetaName = "ownerAddress")]
            public string? OwnerAddress { get; set; }
        }

        [Verb("token.issue", HelpText = "Issue token")]
        public class Issue : KeyStoreAndConnectionOptions
        {
            [Value(0, Required = true, MetaName = "name")]
            public string? Name { get; set; }

            [Value(1, Required = true, MetaName = "symbol")]
            public string? Symbol { get; set; }

            [Value(2, Required = true, MetaName = "domain")]
            public string? Domain { get; set; }

            [Value(3, Required = true, MetaName = "totalSupply")]
            public long TotalSupply { get; set; }

            [Value(4, Required = true, MetaName = "maxSupply")]
            public long MaxSupply { get; set; }

            [Value(5, Required = true, MetaName = "decimals")]
            public int Decimals { get; set; }

            [Value(6, Required = true, MetaName = "isMintable")]
            public string? IsMintable { get; set; }

            [Value(7, Required = true, MetaName = "isBurnable")]
            public string? IsBurnable { get; set; }

            [Value(8, Required = true, MetaName = "isUtility")]
            public string? IsUtility { get; set; }
        }

        [Verb("token.mint", HelpText = "Mint token")]
        public class Mint : KeyStoreAndConnectionOptions
        {
            [Value(0, Required = true, MetaName = "tokenStandard")]
            public string? TokenStandard { get; set; }

            [Value(1, Required = true, MetaName = "amount")]
            public long Amount { get; set; }

            [Value(2, Required = true, MetaName = "receiveAddress")]
            public string? ReceiveAddress { get; set; }
        }

        [Verb("token.burn", HelpText = "Burn token")]
        public class Burn : KeyStoreAndConnectionOptions
        {
            [Value(0, Required = true, MetaName = "tokenStandard")]
            public string? TokenStandard { get; set; }

            [Value(1, Required = true, MetaName = "amount")]
            public long Amount { get; set; }
        }

        [Verb("token.transferOwnership", HelpText = "")]
        public class TransferOwnership : KeyStoreAndConnectionOptions
        {
            [Value(0, Required = true, MetaName = "tokenStandard")]
            public string? TokenStandard { get; set; }

            [Value(1, Required = true, MetaName = "newOwnerAddress")]
            public string? NewOwnerAddress { get; set; }
        }

        [Verb("token.disableMint", HelpText = "")]
        public class DisableMint : KeyStoreAndConnectionOptions
        {
            [Value(0, Required = true, MetaName = "tokenStandard")]
            public string? TokenStandard { get; set; }
        }
    }
}
