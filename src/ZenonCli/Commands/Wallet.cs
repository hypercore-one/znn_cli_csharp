using CommandLine;
using Zenon.Model.Primitives;
using Zenon.Wallet;

namespace ZenonCli.Commands
{
    public class Wallet
    {
        [Verb("wallet.list", HelpText = "List all wallets.")]
        public class List : CommandBase
        {
            protected override async Task ProcessAsync()
            {
                var walletDefinitions = await GetAllWalletDefinitionsAsync();

                if (walletDefinitions.Count() != 0)
                {
                    WriteInfo("Available wallets:");

                    foreach (var walletDefinition in walletDefinitions)
                    {
                        WriteInfo(walletDefinition.WalletName);
                    }
                }
                else
                {
                    WriteInfo("No wallets found");
                }
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
                    var keyStoreManager = new KeyStoreManager();
                    var keyStoreDefinition = keyStoreManager!.CreateNew(Passphrase, KeyStoreName);

                    WriteInfo($"keyStore successfully created: {keyStoreDefinition.WalletId}");
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
                    var keyStoreManager = new KeyStoreManager();
                    var keyStoreDefinition = keyStoreManager!.CreateFromMnemonic(Mnemonic, Passphrase, KeyStoreName);

                    WriteInfo($"Wallet successfully created from mnemonic: {keyStoreDefinition.WalletId}");
                });
            }
        }

        [Verb("wallet.dumpMnemonic", HelpText = "Dump the mnemonic of a wallet.")]
        public class DumpMnemonic : WalletCommand
        {
            protected override async Task ProcessAsync()
            {
                await Task.Run(() =>
                {
                    var keyStore = Wallet as KeyStore;

                    if (keyStore == null)
                    {
                        WriteError("This command is not supported by this wallet");
                    }

                    WriteInfo($"Mnemonic for wallet: {WalletDefinition!.WalletId}");

                    WriteInfo(keyStore!.Mnemonic);
                });
            }
        }

        [Verb("wallet.deriveAddresses", HelpText = "Derive one or more addresses of a wallet.")]
        public class DeriveAddresses : WalletCommand
        {
            [Value(0, MetaName = "start", Required = true)]
            public int Start { get; set; }

            [Value(1, MetaName = "end", Required = true)]
            public int End { get; set; }

            protected override async Task ProcessAsync()
            {
                WriteInfo($"Addresses for wallet: {WalletDefinition!.WalletName}");

                var addresses = new List<Address>();
                for (var i = Start; i < End; i++)
                {
                    var signer = await Wallet!.GetAccountAsync(i);
                    addresses.Add(await signer.GetAddressAsync());
                }

                for (int i = 0; i < End - Start; i += 1)
                {
                    WriteInfo($"  {i + Start}\t{addresses[i]}");
                }
            }
        }

        [Verb("wallet.export", HelpText = "Export wallet.")]
        public class Export : WalletCommand
        {
            [Value(0, MetaName = "filePath", Required = true)]
            public string? FilePath { get; set; }

            protected override async Task ProcessAsync()
            {
                await Task.Run(() =>
                {
                    var keyStoreManager = WalletManager as KeyStoreManager;

                    if (keyStoreManager == null)
                    {
                        WriteError("This command is not supported by this wallet");
                    }

                    File.Copy(WalletDefinition!.WalletId, FilePath!);

                    WriteInfo("Done! Check the current directory");
                });
            }
        }
    }
}