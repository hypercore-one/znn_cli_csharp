using ZenonCli.Options;

namespace ZenonCli.Commands
{
    public abstract class KeyStoreCommand : CommandBase, IWalletOptions
    {
        public string? Passphrase { get; set; }
        public string? KeyStore { get; set; }
        public int Index { get; set; }
    }
}
