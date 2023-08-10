using ZenonCli.Options;

namespace ZenonCli.Commands
{
    public abstract class WalletCommand : CommandBase, IWalletOptions
    {
        public string? Passphrase { get; set; }
        public string? WalletName { get; set; }
        public int Index { get; set; }
    }
}
