namespace ZenonCli.Options
{
    public abstract class KeyStoreAndConnectionOptions : KeyStoreOptions, IConnectionOptions
    {
        public bool Verbose { get; set; }
        public string? Url { get; set; }
    }
}
