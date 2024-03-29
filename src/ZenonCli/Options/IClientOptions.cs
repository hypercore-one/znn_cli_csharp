﻿using CommandLine;

namespace ZenonCli.Options
{
    public interface IClientOptions : IFlags
    {
        [Option('u', "url", Required = false, Default = "ws://127.0.0.1:35998", HelpText = "Provide a websocket znnd connection URL with a port")]
        public string? Url { get; set; }

        [Option('c', "chain", HelpText = "Chain Identifier for the connected node")]
        public string? Chain { get; set; }
    }
}
