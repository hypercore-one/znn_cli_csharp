using ZenonCli.Options;

namespace ZenonCli.Commands
{
    public abstract class KeyStoreAndConnectionCommand : KeyStoreCommand, IConnectionOptions
    {
        public bool Verbose { get; set; }
        public string? Url { get; set; }
        public string? Chain { get; set; }
    }
}
