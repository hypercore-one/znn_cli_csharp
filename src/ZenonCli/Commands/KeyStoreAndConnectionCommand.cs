﻿using ZenonCli.Options;

namespace ZenonCli.Commands
{
    public abstract class KeyStoreAndConnectionCommand : KeyStoreCommand, IClientOptions
    {
        public bool Verbose { get; set; }
        public string? Url { get; set; }
        public string? Chain { get; set; }
    }
}
