namespace ZenonCli.Options
{
    public abstract class KeyStoreOptions : IKeyStoreOptions
    {
        public string? Passphrase { get; set; }
        public string? KeyStore { get; set; }
        public int Index { get; set; }
    }
}
