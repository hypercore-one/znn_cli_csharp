﻿using CommandLine;

namespace ZenonCli.Options
{
    interface IFlags
    {
        [Option('v', "verbose", Required = false, HelpText = "Prints detailed information about the action that it performs")]
        public bool Verbose { get; set; }
    }
}
