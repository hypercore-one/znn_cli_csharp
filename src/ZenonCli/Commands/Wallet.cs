using CommandLine;
using Zenon;

namespace ZenonCli.Commands
{
    public class Wallet
    {
        [Verb("wallet.list", HelpText = "List all wallets.")]
        public class List : CommandBase
        {
            protected override async Task ProcessAsync()
            {
                await Task.Run(() =>
                {
                    var stores = ZnnClient.KeyStoreManager.ListAllKeyStores();

                    if (stores.Length != 0)
                    {
                        WriteInfo("Available keyStores:");

                        foreach (var store in stores)
                        {
                            WriteInfo(Path.GetFileName(store));
                        }
                    }
                    else
                    {
                        WriteInfo("No keyStores found");
                    }
                });
            }
        }

        [Verb("wallet.createNew", HelpText = "Create a new wallet.")]
        public class CreateNew : CommandBase
        {
            [Value(0, MetaName = "passphrase", Required = true)]
            public string? Passphrase { get; set; }

            [Value(1, MetaName = "keyStoreName")]
            public string? KeyStoreName { get; set; }

            protected override async Task ProcessAsync()
            {
                await Task.Run(() =>
                {
                    var keyStore = ZnnClient.KeyStoreManager.CreateNew(this.Passphrase, this.KeyStoreName);

                    WriteInfo($"keyStore successfully created: {Path.GetFileName(keyStore)}");
                });
            }
        }

        [Verb("wallet.createFromMnemonic", HelpText = "Create a new wallet from a mnemonic.")]
        public class CreateFromMnemonic : CommandBase
        {
            [Value(0, MetaName = "mnemonic", MetaValue = "\"mnemonic\"", Required = true)]
            public string? Mnemonic { get; set; }

            [Value(1, MetaName = "passphrase", Required = true)]
            public string? Passphrase { get; set; }

            [Value(2, MetaName = "keyStoreName")]
            public string? KeyStoreName { get; set; }

            protected override async Task ProcessAsync()
            {
                await Task.Run(() =>
                {
                    var keyStore = ZnnClient.KeyStoreManager
                        .CreateFromMnemonic(this.Mnemonic, this.Passphrase, this.KeyStoreName);

                    WriteInfo($"keyStore successfully from mnemonic: {Path.GetFileName(keyStore)}");
                });
            }
        }

        [Verb("wallet.dumpMnemonic", HelpText = "Dump the mnemonic of a wallet.")]
        public class DumpMnemonic : KeyStoreCommand
        {
            protected override async Task ProcessAsync()
            {
                await Task.Run(() =>
                {
                    WriteInfo($"Mnemonic for keyStore File: {ZnnClient.DefaultKeyStorePath}");

                    WriteInfo(ZnnClient.DefaultKeyStore.Mnemonic);
                });
            }
        }

        [Verb("wallet.deriveAddresses", HelpText = "Derive one or more addresses of a wallet.")]
        public class DeriveAddresses : KeyStoreCommand
        {
            [Value(0, MetaName = "start", Required = true)]
            public int Start { get; set; }

            [Value(1, MetaName = "end", Required = true)]
            public int End { get; set; }

            protected override async Task ProcessAsync()
            {
                await Task.Run(() =>
                {
                    WriteInfo($"Addresses for keyStore File: {ZnnClient.DefaultKeyStorePath}");

                    var addresses = ZnnClient.DefaultKeyStore.DeriveAddressesByRange(this.Start, this.End);

                    for (int i = 0; i < this.End - this.Start; i += 1)
                    {
                        WriteInfo($"  {i + this.Start}\t{addresses[i]}");
                    }
                });
            }
        }

        [Verb("wallet.export", HelpText = "Export wallet.")]
        public class Export : KeyStoreCommand
        {
            [Value(0, MetaName = "filePath", Required = true)]
            public string? FilePath { get; set; }

            protected override async Task ProcessAsync()
            {
                await Task.Run(() =>
                {
                    File.Copy(ZnnClient.DefaultKeyStorePath, this.FilePath!);

                    WriteInfo("Done! Check the current directory");
                });
            }
        }
    }
}