using CommandLine;

namespace ZenonCli.Commands
{
    public class Stats
    {
        [Verb("os.info", HelpText = "Get the os info.")]
        public class OsInfo : ConnectionCommand
        {
            protected override async Task ProcessAsync()
            {
                var osInfo = await ZnnClient.Stats.OsInfo();

                WriteInfo($"os: {osInfo.os}");
                WriteInfo($"platform: {osInfo.platform}");
                WriteInfo($"platformFamily: {osInfo.platformFamily}");
                WriteInfo($"platformVersion: {osInfo.platformVersion}");
                WriteInfo($"kernelVersion: {osInfo.kernelVersion}");
                WriteInfo($"memoryTotal: {osInfo.memoryTotal} ({FormatMemory(osInfo.memoryTotal)})");
                WriteInfo($"memoryFree: ${osInfo.memoryFree} ({FormatMemory(osInfo.memoryFree)})");
                WriteInfo($"numCPU: {osInfo.numCPU}");
                WriteInfo($"numGoroutine: {osInfo.numGoroutine}");
            }

            private string FormatMemory(ulong size)
            {
                var sizeUnits = new string[] { "B", "kB", "MB", "GB", "TB" };

                int i = size == 0 ? 0 : (int)Math.Floor(Math.Log(size) / Math.Log(1024));
                return (size / Math.Pow(1024, i) * 1).ToString("00") + $" {sizeUnits[i]}";
            }
        }

        [Verb("process.info", HelpText = "Get the process info.")]
        public class ProcessInfo : ConnectionCommand
        {
            protected override async Task ProcessAsync()
            {
                var processInfo = await ZnnClient.Stats.ProcessInfo();

                WriteInfo($"version: {processInfo.version}");
                WriteInfo($"commit: {processInfo.commit}");
            }
        }

        [Verb("network.info", HelpText = "Get the network info.")]
        public class NetworkInfo : ConnectionCommand
        {
            protected override async Task ProcessAsync()
            {
                var networkInfo = await ZnnClient.Stats.NetworkInfo();

                WriteInfo($"numPeers: {networkInfo.numPeers}");
                foreach (var peer in networkInfo.peers)
                {
                    WriteInfo($"    publicKey: {peer.publicKey}");
                    WriteInfo($"    ip: {peer.ip}");
                }
                WriteInfo($"self.publicKey: {networkInfo.self.publicKey}");
            }
        }

        [Verb("sync.info", HelpText = "Get the sync info.")]
        public class SyncInfo : ConnectionCommand
        {
            protected override async Task ProcessAsync()
            {
                var syncInfo = await ZnnClient.Stats.SyncInfo();

                WriteInfo($"state: {syncInfo.state}");
                WriteInfo($"currentHeight: {syncInfo.currentHeight}");
                WriteInfo($"targetHeight: {syncInfo.targetHeight}");
            }
        }
    }
}
