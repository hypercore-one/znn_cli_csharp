namespace ZenonCli.Options
{
    public abstract class ConnectionOptions : IConnectionOptions
    {
        public bool Verbose { get; set; }
        public string? Url { get; set; }
    }
}
