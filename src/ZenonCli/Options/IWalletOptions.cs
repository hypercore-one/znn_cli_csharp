using CommandLine;

namespace ZenonCli.Options
{
    public interface IWalletOptions
    {
        [Option('p', "passphrase", HelpText = "Use this passphrase for the keyStore or enter it manually in a secure way")]
        public string? Passphrase { get; set; }

        [Option('k', "keyStore", HelpText = "Select the local keyStore")]
        public string? KeyStore { get; set; }

        [Option('i', "index", Default = 0, HelpText = "Address index")]
        public int Index { get; set; }
    }
}
