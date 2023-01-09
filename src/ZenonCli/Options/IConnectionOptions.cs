using CommandLine;

namespace ZenonCli.Options
{
    interface IConnectionOptions : IFlags
    {
        [Option('u', "url", Required = false, Default = "ws://127.0.0.1:35998", HelpText = "Provide a websocket znnd connection URL with a port")]
        public string? Url { get; set; }
    }
}
