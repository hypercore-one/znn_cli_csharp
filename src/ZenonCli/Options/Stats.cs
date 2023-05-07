using CommandLine;

namespace ZenonCli.Options
{
    public class Stats
    {
        [Verb("os.info", HelpText = "Get the os info")]
        public class OsInfo : ConnectionOptions
        { }

        [Verb("process.info", HelpText = "Get the process info")]
        public class ProcessInfo : ConnectionOptions
        { }

        [Verb("network.info", HelpText = "Get the network info")]
        public class NetworkInfo : ConnectionOptions
        { }

        [Verb("sync.info", HelpText = "Get the sync info")]
        public class SyncInfo : ConnectionOptions
        { }
    }
}
