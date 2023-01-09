using CommandLine;

namespace ZenonCli.Options
{
    public class Wallet
    {
        [Verb("wallet.list", HelpText = "List all wallets")]
        public class List
        { }

        [Verb("wallet.createNew", HelpText = "Create a new wallet")]
        public class CreateNew
        {
            [Value(0, MetaName = "passphrase", Required = true)]
            public string? Passphrase { get; set; }

            [Value(1, MetaName = "keyStoreName")]
            public string? KeyStoreName { get; set; }
        }

        [Verb("wallet.createFromMnemonic", HelpText = "Create a new wallet from a mnemonic")]
        public class CreateFromMnemonic
        {
            [Value(0, MetaName = "mnemonic", MetaValue = "\"mnemonic\"", Required = true)]
            public string? Mnemonic { get; set; }

            [Value(1, MetaName = "passphrase", Required = true)]
            public string? Passphrase { get; set; }

            [Value(2, MetaName = "keyStoreName")]
            public string? KeyStoreName { get; set; }
        }

        [Verb("wallet.dumpMnemonic", HelpText = "Dump the mnemonic of a wallet")]
        public class DumpMnemonic : KeyStoreOptions
        { }

        [Verb("wallet.deriveAddresses", HelpText = "Derive one or more addresses of a wallet")]
        public class DeriveAddresses : KeyStoreOptions
        {
            [Value(0, MetaName = "start", Required = true)]
            public int Start { get; set; }

            [Value(1, MetaName = "end", Required = true)]
            public int End { get; set; }
        }

        [Verb("wallet.export", HelpText = "Export wallet")]
        public class Export : KeyStoreOptions
        {
            [Value(0, MetaName = "filePath", Required = true)]
            public string? FilePath { get; set; }
        }
    }
}
