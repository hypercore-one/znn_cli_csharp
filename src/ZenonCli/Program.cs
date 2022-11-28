using CommandLine;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Zenon;
using Zenon.Abi;
using Zenon.Crypto;
using Zenon.Embedded;
using Zenon.Model.Embedded;
using Zenon.Model.NoM;
using Zenon.Model.Primitives;
using Zenon.Utils;
using Zenon.Wallet;

namespace ZenonCli
{
    class Program
    {
        public const int HashTypeSha3256 = 0;
        public const int HashTypeSha2256 = 1;

        #region Options

        interface IFlags
        {
            [Option('v', "verbose", Required = false, HelpText = "Prints detailed information about the action that it performs")]
            public bool Verbose { get; set; }
        }

        interface IKeyStoreOptions
        {
            [Option('p', "passphrase", HelpText = "Use this passphrase for the keyStore or enter it manually in a secure way")]
            public string? Passphrase { get; set; }

            [Option('k', "keyStore", HelpText = "Select the local keyStore\n(defaults to \"available keyStore if only one is present\")")]
            public string? KeyStore { get; set; }

            [Option('i', "index", Default = 0, HelpText = "Address index")]
            public int Index { get; set; }
        }

        interface IConnectionOptions : IFlags
        {
            [Option('n', "netId", Default = Constants.NetId, HelpText = "Specify the network id to use")]
            public int NetId { get; set; }

            [Option('u', "url", Required = false, Default = "ws://127.0.0.1:35998", HelpText = "Provide a websocket znnd connection URL with a port")]
            public string? Url { get; set; }
        }

        public abstract class KeyStoreOptions : IKeyStoreOptions
        {
            public string? Passphrase { get; set; }
            public string? KeyStore { get; set; }
            public int Index { get; set; }
        }

        public abstract class ConnectionOptions : IConnectionOptions
        {
            public bool Verbose { get; set; }
            public string? Url { get; set; }
            public int NetId { get; set; }
        }

        public abstract class KeyStoreAndConnectionOptions : KeyStoreOptions, IConnectionOptions
        {
            public bool Verbose { get; set; }
            public string? Url { get; set; }
            public int NetId { get; set; }
        }

        public class General
        {
            [Verb("version", HelpText = "Display version information.")]
            public class Version : ConnectionOptions
            { }

            [Verb("send", HelpText = "Send tokens to an address.")]
            public class Send : KeyStoreAndConnectionOptions
            {
                [Value(0, Required = true, MetaName = "toAddress")]
                public string? ToAddress { get; set; }

                [Value(1, Required = true, MetaName = "amount")]
                public long Amount { get; set; }

                [Value(2, Default = "ZNN", MetaName = "tokenStandard", MetaValue = "[ZNN/QSR/ZTS]")]
                public string? TokenStandard { get; set; }

                [Value(3, MetaName = "message")]
                public string? Message { get; set; }
            }

            [Verb("receive", HelpText = "Receive a specified unreceived transaction by blockHash.")]
            public class Receive : KeyStoreAndConnectionOptions
            {
                [Value(0, MetaName = "blockHash", Required = true)]
                public string? BlockHash { get; set; }
            }

            [Verb("receiveAll", HelpText = "Receives all unreceived transactions.")]
            public class ReceiveAll : KeyStoreAndConnectionOptions
            { }

            [Verb("unreceived", HelpText = "List unreceived transactions.")]
            public class Unreceived : KeyStoreAndConnectionOptions
            { }

            [Verb("autoreceive", HelpText = "Automaticly receive transactions.")]
            public class Autoreceive : KeyStoreAndConnectionOptions
            { }

            [Verb("unconfirmed", HelpText = "List unconfirmed transactions.")]
            public class Unconfirmed : KeyStoreAndConnectionOptions
            { }

            [Verb("balance", HelpText = "List account balance.")]
            public class Balance : KeyStoreAndConnectionOptions
            { }

            [Verb("frontierMomentum", HelpText = "List frontier momentum.")]
            public class FrontierMomentum : KeyStoreAndConnectionOptions
            { }

            [Verb("createHash", HelpText = "Create hash digests by using the stated algorithm.")]
            public class CreateHash
            {
                [Value(0, MetaName = "message")]
                public string? Message { get; set; }

                [Value(1, MetaName = "hashType", Default = 0, HelpText = "0 = SHA3-256, 1 = SHA-256")]
                public int? HashType { get; set; }
            }
        }

        public class Plasma
        {
            [Verb("plasma.list", HelpText = "List plasma fusion entries.")]
            public class List : KeyStoreAndConnectionOptions
            {
                [Value(0, Default = 0, MetaName = "pageIndex")]
                public int? PageIndex { get; set; }

                [Value(1, Default = 25, MetaName = "PageSize")]
                public int? PageSize { get; set; }
            }

            [Verb("plasma.get")]
            public class Get : KeyStoreAndConnectionOptions
            { }

            [Verb("plasma.fuse", HelpText = "Fuse QSR to an address to generate plasma.")]
            public class Fuse : KeyStoreAndConnectionOptions
            {
                [Value(0, Required = true, MetaName = "toAddress")]
                public string? ToAddress { get; set; }

                [Value(1, Required = true, MetaName = "amount")]
                public long Amount { get; set; }
            }

            [Verb("plasma.cancel", HelpText = "")]
            public class Cancel : KeyStoreAndConnectionOptions
            {
                [Value(0, Required = true, MetaName = "id")]
                public string? Id { get; set; }
            }
        }

        public class Sentinel
        {
            [Verb("sentinel.list", HelpText = "List all sentinels")]
            public class List : KeyStoreAndConnectionOptions
            {
            }

            [Verb("sentinel.register", HelpText = "Register a sentinel")]
            public class Register : KeyStoreAndConnectionOptions
            {
            }

            [Verb("sentinel.revoke", HelpText = "Revoke a sentinel")]
            public class Revoke : KeyStoreAndConnectionOptions
            {
            }

            [Verb("sentinel.collect", HelpText = "Collect sentinel rewards")]
            public class Collect : KeyStoreAndConnectionOptions
            {
            }

            [Verb("sentinel.withdrawQsr", HelpText = "")]
            public class WithdrawQsr : KeyStoreAndConnectionOptions
            {
            }
        }

        public class Stake
        {
            [Verb("stake.list", HelpText = "List all stakes")]
            public class List : KeyStoreAndConnectionOptions
            {
                [Value(0, Default = 0, MetaName = "pageIndex")]
                public int? PageIndex { get; set; }

                [Value(1, Default = 25, MetaName = "PageSize")]
                public int? PageSize { get; set; }
            }

            [Verb("stake.register", HelpText = "Register stake")]
            public class Register : KeyStoreAndConnectionOptions
            {
                [Value(0, Required = true, MetaName = "amount")]
                public long Amount { get; set; }

                [Value(1, Required = true, MetaName = "duration", HelpText = "Duration in months")]
                public long Duration { get; set; }
            }

            [Verb("stake.revoke", HelpText = "Revoke stake")]
            public class Revoke : KeyStoreAndConnectionOptions
            {
                [Value(0, Required = true, MetaName = "id")]
                public string? Id { get; set; }
            }

            [Verb("stake.collect", HelpText = "Collect staking rewards")]
            public class Collect : KeyStoreAndConnectionOptions
            {
            }
        }

        public class Pillar
        {
            [Verb("pillar.list", HelpText = "List all pillars")]
            public class List : KeyStoreAndConnectionOptions
            { }

            [Verb("pillar.register", HelpText = "Register pillar")]
            public class Register : KeyStoreAndConnectionOptions
            {
                [Value(0, Required = true, MetaName = "name")]
                public string? Name { get; set; }

                [Value(1, Required = true, MetaName = "producerAddress")]
                public string? ProducerAddress { get; set; }

                [Value(2, Required = true, MetaName = "rewardAddress")]
                public string? RewardAddress { get; set; }

                [Value(3, Required = true, MetaName = "giveBlockRewardPercentage")]
                public int GiveBlockRewardPercentage { get; set; }

                [Value(4, Required = true, MetaName = "giveDelegateRewardPercentage")]
                public int GiveDelegateRewardPercentage { get; set; }
            }

            [Verb("pillar.revoke", HelpText = "Revoke pillar")]
            public class Revoke : KeyStoreAndConnectionOptions
            {
                [Value(0, Required = true, MetaName = "name")]
                public string? Name { get; set; }
            }

            [Verb("pillar.delegate", HelpText = "Delegate to pillar")]
            public class Delegate : KeyStoreAndConnectionOptions
            {
                [Value(0, Required = true, MetaName = "name")]
                public string? Name { get; set; }
            }

            [Verb("pillar.undelegate", HelpText = "Undelegate pillar")]
            public class Undelegate : KeyStoreAndConnectionOptions
            {
            }

            [Verb("pillar.collect", HelpText = "Collect pillar rewards")]
            public class Collect : KeyStoreAndConnectionOptions
            {
            }

            [Verb("pillar.withdrawQsr", HelpText = "")]
            public class WithdrawQsr : KeyStoreAndConnectionOptions
            {
            }
        }

        public class Token
        {
            [Verb("token.list", HelpText = "List all tokens")]
            public class List : KeyStoreAndConnectionOptions
            {
                [Value(0, Default = 0, MetaName = "pageIndex")]
                public int? PageIndex { get; set; }

                [Value(1, Default = 25, MetaName = "PageSize")]
                public int? PageSize { get; set; }
            }

            [Verb("token.getByStandard", HelpText = "List tokens by standard")]
            public class GetByStandard : KeyStoreAndConnectionOptions
            {
                [Value(0, Required = true, MetaName = "tokenStandard")]
                public string? TokenStandard { get; set; }
            }

            [Verb("token.getByOwner", HelpText = "List tokens by owner")]
            public class GetByOwner : KeyStoreAndConnectionOptions
            {
                [Value(0, Required = true, MetaName = "ownerAddress")]
                public string? OwnerAddress { get; set; }
            }

            [Verb("token.issue", HelpText = "Issue token")]
            public class Issue : KeyStoreAndConnectionOptions
            {
                [Value(0, Required = true, MetaName = "name")]
                public string? Name { get; set; }

                [Value(1, Required = true, MetaName = "symbol")]
                public string? Symbol { get; set; }

                [Value(2, Required = true, MetaName = "domain")]
                public string? Domain { get; set; }

                [Value(3, Required = true, MetaName = "totalSupply")]
                public long TotalSupply { get; set; }

                [Value(4, Required = true, MetaName = "maxSupply")]
                public long MaxSupply { get; set; }

                [Value(5, Required = true, MetaName = "decimals")]
                public int Decimals { get; set; }

                [Value(6, Required = true, MetaName = "isMintable")]
                public string? IsMintable { get; set; }

                [Value(7, Required = true, MetaName = "isBurnable")]
                public string? IsBurnable { get; set; }

                [Value(8, Required = true, MetaName = "isUtility")]
                public string? IsUtility { get; set; }
            }

            [Verb("token.mint", HelpText = "Undelegate pillar")]
            public class Mint : KeyStoreAndConnectionOptions
            {
                [Value(0, Required = true, MetaName = "tokenStandard")]
                public string? TokenStandard { get; set; }

                [Value(1, Required = true, MetaName = "amount")]
                public long Amount { get; set; }

                [Value(2, Required = true, MetaName = "receiveAddress")]
                public string? ReceiveAddress { get; set; }
            }

            [Verb("token.burn", HelpText = "Collect pillar rewards")]
            public class Burn : KeyStoreAndConnectionOptions
            {
                [Value(0, Required = true, MetaName = "tokenStandard")]
                public string? TokenStandard { get; set; }

                [Value(1, Required = true, MetaName = "amount")]
                public long Amount { get; set; }
            }

            [Verb("token.transferOwnership", HelpText = "")]
            public class TransferOwnership : KeyStoreAndConnectionOptions
            {
                [Value(0, Required = true, MetaName = "tokenStandard")]
                public string? TokenStandard { get; set; }

                [Value(1, Required = true, MetaName = "newOwnerAddress")]
                public string? NewOwnerAddress { get; set; }
            }

            [Verb("token.disableMint", HelpText = "")]
            public class DisableMint : KeyStoreAndConnectionOptions
            {
                [Value(0, Required = true, MetaName = "tokenStandard")]
                public string? TokenStandard { get; set; }
            }
        }

        public class Htlc
        {
            [Verb("htlc.get", HelpText = "List htlc by id")]
            public class Get : KeyStoreAndConnectionOptions
            {
                [Value(0, Required = true, MetaName = "id")]
                public string? Id { get; set; }
            }

            [Verb("htlc.timeLocked", HelpText = "List all time locked htlc's")]
            public class GetByTimeLocked : KeyStoreAndConnectionOptions
            {
                [Value(1, Default = 0, MetaName = "pageIndex")]
                public int? PageIndex { get; set; }

                [Value(2, Default = 25, MetaName = "PageSize")]
                public int? PageSize { get; set; }
            }

            [Verb("htlc.hashLocked", HelpText = "List all hash locked htlc's")]
            public class GetByHashLocked : KeyStoreAndConnectionOptions
            {
                [Value(1, Default = 0, MetaName = "pageIndex")]
                public int? PageIndex { get; set; }

                [Value(2, Default = 25, MetaName = "PageSize")]
                public int? PageSize { get; set; }
            }

            [Verb("htlc.create", HelpText = "Create htlc")]
            public class Create : KeyStoreAndConnectionOptions
            {
                [Value(1, Required = true, MetaName = "hashLockedAddress")]
                public string? HashLockedAddress { get; set; }

                [Value(2, Required = true, MetaName = "tokenStandard", MetaValue = "[ZNN/QSR/ZTS]")]
                public string? TokenStandard { get; set; }

                [Value(3, Required = true, MetaName = "amount")]
                public long Amount { get; set; }

                [Value(4, Required = true, MetaName = "expirationTime", HelpText = "Total seconds from now.")]
                public long ExpirationTime { get; set; }

                [Value(5, Required = true, MetaName = "hashLock", HelpText = "The hash lock as a hexidecimal string.")]
                public string? HashLock { get; set; }

                [Value(6, MetaName = "hashType", Default = 0, HelpText = "0 = SHA3-256, 1 = SHA-256")]
                public int? HashType { get; set; }

                [Value(7, MetaName = "keyMaxSize", Default = 32, HelpText = "Maximum size of the pre-image.")]
                public int? KeyMaxSize { get; set; }
            }

            [Verb("htlc.reclaim", HelpText = "Reclaim htlc")]
            public class Reclaim : KeyStoreAndConnectionOptions
            {
                [Value(0, Required = true, MetaName = "id", HelpText = "The id of the htlc to reclaim.")]
                public string? Id { get; set; }
            }

            [Verb("htlc.unlock", HelpText = "Unlock htlc")]
            public class Unlock : KeyStoreAndConnectionOptions
            {
                [Value(0, Required = true, MetaName = "id", HelpText = "The id of the htlc to unlock.")]
                public string? Id { get; set; }

                [Value(1, MetaName = "preimage")]
                public string? Preimage { get; set; }
            }

            [Verb("htlc.inspect", HelpText = "Inspect htlc received account-block")]
            public class Inspect : ConnectionOptions
            {
                [Value(0, Required = true, MetaName = "blockHash", HelpText = "The hash of a htlc account-block.")]
                public string? BlockHash { get; set; }
            }

            [Verb("htlc.monitor", HelpText = "Monitor htlc by id.")]
            public class Monitor : KeyStoreAndConnectionOptions
            {
                [Value(0, Required = true, MetaName = "id", HelpText = "The id of the htlc to monitor.")]
                public string? Id { get; set; }
            }

            [Verb("htlc.monitorAll", HelpText = "Monitor all htlc's.")]
            public class MonitorAll : KeyStoreAndConnectionOptions
            { }
        }

        public class Wallet
        {
            [Verb("wallet.list", HelpText = "List all wallets")]
            public class List
            { }

            [Verb("wallet.createNew", HelpText = "Create a new wallet")]
            public class CreateNew
            {
                [Value(0, MetaName = "passphrase", Required = true)]
                public string? Passphrase { get; set; }

                [Value(1, MetaName = "keyStoreName")]
                public string? KeyStoreName { get; set; }
            }

            [Verb("wallet.createFromMnemonic", HelpText = "Create a new wallet from a mnemonic")]
            public class CreateFromMnemonic
            {
                [Value(0, MetaName = "mnemonic", MetaValue = "\"mnemonic\"", Required = true)]
                public string? Mnemonic { get; set; }

                [Value(1, MetaName = "passphrase", Required = true)]
                public string? Passphrase { get; set; }

                [Value(2, MetaName = "keyStoreName")]
                public string? KeyStoreName { get; set; }
            }

            [Verb("wallet.dumpMnemonic", HelpText = "Dump the mnemonic of a wallet")]
            public class DumpMnemonic : KeyStoreOptions
            { }

            [Verb("wallet.deriveAddresses", HelpText = "Derive one or more addresses of a wallet")]
            public class DeriveAddresses : KeyStoreOptions
            {
                [Value(0, MetaName = "start", Required = true)]
                public int Start { get; set; }

                [Value(1, MetaName = "end", Required = true)]
                public int End { get; set; }
            }

            [Verb("wallet.export", HelpText = "Export wallet")]
            public class Export : KeyStoreOptions
            {
                [Value(0, MetaName = "filePath", Required = true)]
                public string? FilePath { get; set; }
            }
        }

        #endregion

        private static Type[] LoadVerbs()
        {
            return Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.GetCustomAttribute<VerbAttribute>() != null).ToArray();
        }

        public static async Task Main(string[] args)
        {
            var types = LoadVerbs();

            var parser = new Parser(config =>
            {
                config.AutoVersion = false;
                config.HelpWriter = Console.Out;
            });

            await parser.ParseArguments(args, types)
                .WithParsedAsync(RunAsync);
        }

        private static async Task RunAsync(object obj)
        {
            try
            {
                if (obj is IKeyStoreOptions)
                {
                    Process((IKeyStoreOptions)obj);
                }

                if (obj is IConnectionOptions)
                {
                    await StartConnectionAsync((IConnectionOptions)obj);
                }

                switch (obj)
                {
                    case General.Version gv:
                        await ProcessAsync(gv);
                        break;
                    case General.Send gs:
                        await ProcessAsync(gs);
                        break;
                    case General.Receive gr:
                        await ProcessAsync(gr);
                        break;
                    case General.ReceiveAll gra:
                        await ProcessAsync(gra);
                        break;
                    case General.Unreceived gur:
                        await ProcessAsync(gur);
                        break;
                    case General.Autoreceive gar:
                        await ProcessAsync(gar);
                        break;
                    case General.Unconfirmed guc:
                        await ProcessAsync(guc);
                        break;
                    case General.Balance gb:
                        await ProcessAsync(gb);
                        break;
                    case General.FrontierMomentum gfm:
                        await ProcessAsync(gfm);
                        break;
                    case General.CreateHash gch:
                        Process(gch);
                        break;

                    case Plasma.List pl:
                        await ProcessAsync(pl);
                        break;
                    case Plasma.Get pg:
                        await ProcessAsync(pg);
                        break;
                    case Plasma.Fuse pf:
                        await ProcessAsync(pf);
                        break;
                    case Plasma.Cancel pc:
                        await ProcessAsync(pc);
                        break;

                    case Sentinel.List sel:
                        await ProcessAsync(sel);
                        break;
                    case Sentinel.Register sereg:
                        await ProcessAsync(sereg);
                        break;
                    case Sentinel.Revoke ser:
                        await ProcessAsync(ser);
                        break;
                    case Sentinel.Collect sec:
                        await ProcessAsync(sec);
                        break;
                    case Sentinel.WithdrawQsr sewq:
                        await ProcessAsync(sewq);
                        break;

                    case Stake.List stl:
                        await ProcessAsync(stl);
                        break;
                    case Stake.Register streg:
                        await ProcessAsync(streg);
                        break;
                    case Stake.Revoke str:
                        await ProcessAsync(str);
                        break;
                    case Stake.Collect stc:
                        await ProcessAsync(stc);
                        break;

                    case Pillar.List pl:
                        await ProcessAsync(pl);
                        break;
                    case Pillar.Register pr:
                        await ProcessAsync(pr);
                        break;
                    case Pillar.Revoke pr:
                        await ProcessAsync(pr);
                        break;
                    case Pillar.Delegate pd:
                        await ProcessAsync(pd);
                        break;
                    case Pillar.Undelegate pu:
                        await ProcessAsync(pu);
                        break;
                    case Pillar.Collect pc:
                        await ProcessAsync(pc);
                        break;
                    case Pillar.WithdrawQsr pw:
                        await ProcessAsync(pw);
                        break;

                    case Token.List tl:
                        await ProcessAsync(tl);
                        break;
                    case Token.GetByStandard tgs:
                        await ProcessAsync(tgs);
                        break;
                    case Token.GetByOwner tgo:
                        await ProcessAsync(tgo);
                        break;
                    case Token.Issue ti:
                        await ProcessAsync(ti);
                        break;
                    case Token.Mint tm:
                        await ProcessAsync(tm);
                        break;
                    case Token.Burn tb:
                        await ProcessAsync(tb);
                        break;
                    case Token.TransferOwnership tt:
                        await ProcessAsync(tt);
                        break;
                    case Token.DisableMint td:
                        await ProcessAsync(td);
                        break;

                    case Htlc.Get hg:
                        await ProcessAsync(hg);
                        break;
                    case Htlc.GetByTimeLocked hgt:
                        await ProcessAsync(hgt);
                        break;
                    case Htlc.GetByHashLocked hgh:
                        await ProcessAsync(hgh);
                        break;
                    case Htlc.Create hc:
                        await ProcessAsync(hc);
                        break;
                    case Htlc.Reclaim hr:
                        await ProcessAsync(hr);
                        break;
                    case Htlc.Unlock hu:
                        await ProcessAsync(hu);
                        break;
                    case Htlc.Inspect hi:
                        await ProcessAsync(hi);
                        break;
                    case Htlc.Monitor hm:
                        await ProcessAsync(hm);
                        break;
                    case Htlc.MonitorAll hma:
                        await ProcessAsync(hma);
                        break;

                    case Wallet.List wl:
                        Process(wl);
                        break;
                    case Wallet.CreateNew wcn:
                        Process(wcn);
                        break;
                    case Wallet.CreateFromMnemonic wcm:
                        Process(wcm);
                        break;
                    case Wallet.DumpMnemonic wdm:
                        Process(wdm);
                        break;
                    case Wallet.DeriveAddresses wda:
                        Process(wda);
                        break;
                    case Wallet.Export we:
                        Process(we);
                        break;
                }

                if (obj is IConnectionOptions)
                {
                    await StopConnectionAsync((IConnectionOptions)obj);
                }
            }
            catch (Exception e)
            {
                WriteError(e.Message);
            }
        }

        #region IKeyStoreOptions

        static void Process(IKeyStoreOptions options)
        {
            var allKeyStores =
                Znn.Instance.KeyStoreManager.ListAllKeyStores();

            string? keyStorePath = null;
            if (allKeyStores == null || allKeyStores.Length == 0)
            {
                // Make sure at least one keyStore exists
                ThrowError("No keyStore in the default directory");
            }
            else if (options.KeyStore != null)
            {
                // Use user provided keyStore: make sure it exists
                keyStorePath = Path.Join(ZnnPaths.Default.Wallet, options.KeyStore);

                WriteInfo(keyStorePath);

                if (!File.Exists(keyStorePath))
                {
                    ThrowError($"The keyStore {options.KeyStore} does not exist in the default directory");
                }
            }
            else if (allKeyStores.Length == 1)
            {
                // In case there is just one keyStore, use it by default
                WriteInfo($"Using the default keyStore {Path.GetFileName(allKeyStores[0])}");
                keyStorePath = allKeyStores[0];
            }
            else
            {
                // Multiple keyStores present, but none is selected: action required
                ThrowError($"Please provide a keyStore or an address. Use wallet.list to list all available keyStores");
            }

            string? passphrase = options.Passphrase;

            if (passphrase == null)
            {
                WriteInfo("Insert passphrase:");
                passphrase = ReadPassword();
            }

            int index = options.Index;

            try
            {
                Znn.Instance.DefaultKeyStore = Znn.Instance.KeyStoreManager.ReadKeyStore(passphrase, keyStorePath);
                Znn.Instance.DefaultKeyStorePath = keyStorePath;
            }
            catch (IncorrectPasswordException)
            {
                ThrowError($"Invalid passphrase for keyStore {keyStorePath}");
            }

            Znn.Instance.DefaultKeyPair = Znn.Instance.DefaultKeyStore.GetKeyPair(index);
        }

        #endregion

        #region IConnectionOptions

        static async Task StartConnectionAsync(IConnectionOptions options)
        {
            Znn.Instance.NetId = options.NetId;

            if (options.Verbose)
                ((Zenon.Client.WsClient)Znn.Instance.Client.Value).TraceSourceLevels = System.Diagnostics.SourceLevels.Verbose;

            await Znn.Instance.Client.Value.StartAsync(new Uri(options.Url!), false);
        }

        static async Task StopConnectionAsync(IConnectionOptions options)
        {
            await Znn.Instance.Client.Value.StopAsync();
        }

        #endregion

        static async Task ProcessAsync(General.Version options)
        {
            var info =
                await Znn.Instance.Stats.ProcessInfo();

            WriteInfo($"Zenon Node {info.version} using Zenon .NET SDK v{Constants.ZnnSdkVersion}");
        }

        #region General

        static async Task ProcessAsync(General.Send options)
        {
            var newAddress = Address.Parse(options.ToAddress);
            TokenStandard tokenStandard;
            long amount = 0;

            if (String.Equals(options.TokenStandard, "ZNN", StringComparison.OrdinalIgnoreCase))
            {
                tokenStandard = TokenStandard.ZnnZts;
            }
            else if (String.Equals(options.TokenStandard, "QSR", StringComparison.OrdinalIgnoreCase))
            {
                tokenStandard = TokenStandard.QsrZts;
            }
            else
            {
                tokenStandard = TokenStandard.Parse(options.TokenStandard);
            }

            var info =
                await Znn.Instance.Ledger.GetAccountInfoByAddress(Znn.Instance.DefaultKeyPair.Address);

            bool ok = true;
            bool found = false;

            foreach (var item in info.BalanceInfoList)
            {
                if (item.Token.TokenStandard == tokenStandard)
                {
                    amount = options.Amount * item.Token.DecimalsExponent;

                    if (item.Balance < amount)
                    {
                        WriteError($"You only have {FormatAmount(item.Balance.Value, item.Token.Decimals)} {item.Token.Symbol} tokens");
                        ok = false;
                        break;
                    }
                    found = true;
                }
            }

            if (!ok) return;
            if (!found)
            {
                WriteError($"You only have {FormatAmount(0, 0)} {tokenStandard} tokens");
                return;
            }

            var data = options.Message != null ? Encoding.ASCII.GetBytes(options.Message) : null;
            var token = await Znn.Instance.Embedded.Token.GetByZts(tokenStandard);
            var block = AccountBlockTemplate.Send(newAddress, tokenStandard, amount, data);

            if (data != null)
            {
                WriteInfo($"Sending {FormatAmount(amount, token.Decimals)} {options.TokenStandard} to {options.ToAddress} with a message \"{options.Message}\"");
            }
            else
            {
                WriteInfo($"Sending {FormatAmount(amount, token.Decimals)} {options.TokenStandard} to {options.ToAddress}");
            }

            await Znn.Instance.Send(block);

            WriteInfo("Done");
        }

        static async Task ProcessAsync(General.Receive options)
        {
            Hash sendBlockHash = Hash.Parse(options.BlockHash);

            WriteInfo("Please wait ...");

            await Znn.Instance.Send(AccountBlockTemplate.Receive(sendBlockHash));

            WriteInfo("Done");
        }

        static async Task ProcessAsync(General.ReceiveAll options)
        {
            var address = Znn.Instance.DefaultKeyPair.Address;

            var unreceived = await Znn.Instance.Ledger
                .GetUnreceivedBlocksByAddress(address, pageIndex: 0, pageSize: 5);

            if (unreceived.Count == 0)
            {
                WriteInfo("Nothing to receive");
                return;
            }
            else
            {
                if (unreceived.More)
                {
                    WriteInfo($"You have \"more\" than {unreceived.Count} transaction(s) to receive");
                }
                else
                {
                    WriteInfo($"You have {unreceived.Count} transaction(s) to receive");
                }
            }

            WriteInfo("Please wait ...");

            while (unreceived.Count! > 0)
            {
                foreach (var block in unreceived.List)
                {
                    await Znn.Instance.Send(AccountBlockTemplate.Receive(block.Hash));
                }

                unreceived = await Znn.Instance.Ledger
                    .GetUnreceivedBlocksByAddress(address, pageIndex: 0, pageSize: 5);
            }

            WriteInfo("Done");
        }

        static async Task ProcessAsync(General.Unreceived options)
        {
            var address = Znn.Instance.DefaultKeyPair.Address;

            var unreceived = await Znn.Instance.Ledger
                .GetUnreceivedBlocksByAddress(address, pageIndex: 0, pageSize: 5);

            if (unreceived.Count == 0)
            {
                WriteInfo("Nothing to receive");
                return;
            }
            else
            {
                if (unreceived.More)
                {
                    WriteInfo($"You have \"more\" than {unreceived.Count} transaction(s) to receive");
                }
                else
                {
                    WriteInfo($"You have {unreceived.Count} transaction(s) to receive");
                }
                WriteInfo($"Showing the first {unreceived.List.Length}");
            }

            foreach (var block in unreceived.List)
            {
                WriteInfo(
                    $"Unreceived {FormatAmount(block.Amount, block.Token.Decimals)} {block.Token.Symbol} from {block.Address.ToString()}. Use the hash {block.Hash} to receive");
            }
        }

        static async Task ProcessAsync(General.Autoreceive options)
        {
            var address = Znn.Instance.DefaultKeyPair.Address;

            var queue = new BlockingCollection<Hash>();

            WriteInfo("Subscribing for account-block events ...");
            await Znn.Instance.Subscribe.ToAllAccountBlocks((json) =>
            {
                for (var i = 0; i < json.Length; i += 1)
                {
                    var tx = json[i];
                    if (tx.Value<string>("toAddress") != address.ToString())
                    {
                        continue;
                    }
                    var hash = Hash.Parse(tx.Value<string>("hash"));
                    WriteInfo($"receiving transaction with hash {hash}");
                    queue.Add(hash);
                }
            });
            WriteInfo("Subscribed successfully!");

            while (true)
            {
                Hash? hash;
                if (queue.TryTake(out hash))
                {
                    var template = await Znn.Instance.Send(AccountBlockTemplate.Receive(hash));
                    WriteInfo($"successfully received {hash}. Receive-block-hash {template.Hash}");
                }

                await Task.Delay(1000);
            }
        }

        static async Task ProcessAsync(General.Unconfirmed options)
        {
            var address = Znn.Instance.DefaultKeyPair.Address;

            var unconfirmed = await Znn.Instance.Ledger
                .GetUnconfirmedBlocksByAddress(address, pageIndex: 0, pageSize: 5);

            if (unconfirmed.Count == 0)
            {
                WriteInfo("No unconfirmed transactions");
            }
            else
            {
                WriteInfo($"You have {unconfirmed.Count} unconfirmed transaction(s)");
                WriteInfo($"Showing the first {unconfirmed.List.Length}");
            }

            foreach (var block in unconfirmed.List)
            {
                WriteInfo(JsonConvert.SerializeObject(block.ToJson(), Formatting.Indented));
            }
        }

        static async Task ProcessAsync(General.Balance options)
        {
            var address = Znn.Instance.DefaultKeyPair.Address;

            var info = await Znn.Instance.Ledger
                .GetAccountInfoByAddress(address);

            WriteInfo($"Balance for account-chain {info.Address} having height {info.BlockCount}");
            if (info.BalanceInfoList.Length == 0)
            {
                WriteInfo($"  No coins or tokens at address {address}");
            }

            foreach (var entry in info.BalanceInfoList)
            {
                WriteInfo($"  {FormatAmount(entry.Balance.Value, entry.Token.Decimals)} {entry.Token.Symbol} {entry.Token.Domain} {entry.Token.TokenStandard}");
            }
        }

        static async Task ProcessAsync(General.FrontierMomentum options)
        {
            var currentFrontierMomentum =
                await Znn.Instance.Ledger.GetFrontierMomentum();

            WriteInfo($"Momentum height: {currentFrontierMomentum.Height}");
            WriteInfo($"Momentum hash: {currentFrontierMomentum.Hash}");
            WriteInfo($"Momentum previousHash: {currentFrontierMomentum.PreviousHash}");
            WriteInfo($"Momentum timestamp: {currentFrontierMomentum.Timestamp}");
        }

        static void Process(General.CreateHash options)
        {
            if (!options.HashType.HasValue)
            {
                options.HashType = HashTypeSha3256;
            }

            if (String.IsNullOrEmpty(options.Message))
            {
                WriteInfo("Insert message:");
                options.Message = ReadPassword();
            }

            if (String.IsNullOrEmpty(options.Message))
            {
                WriteError("Invalid message. Cannot be empty.");
                return;
            }

            byte[]? digest = null;

            switch (options.HashType)
            {
                case HashTypeSha3256:
                    digest = Crypto.Digest(Encoding.UTF8.GetBytes(options.Message));
                    break;

                case HashTypeSha2256:
                    digest = Helper.ComputeStringToSha256Hash(options.Message);
                    break;

                default:
                    WriteError($"Invalid hash type. Value {options.HashType} not supported.");
                    return;
            }

            WriteInfo($"Hash: {BytesUtils.ToHexString(digest)}");
        }

        #endregion

        #region Plasma

        static async Task ProcessAsync(Plasma.List options)
        {
            if (!options.PageIndex.HasValue)
                options.PageIndex = 0;

            if (!options.PageSize.HasValue)
                options.PageSize = 25;

            var address = Znn.Instance.DefaultKeyPair.Address;
            var fusionEntryList = await Znn.Instance.Embedded.Plasma.GetEntriesByAddress(address,
                    options.PageIndex.Value, options.PageSize.Value);

            if (fusionEntryList.Count > 0)
            {
                WriteInfo($"Fusing {FormatAmount(fusionEntryList.QsrAmount, Constants.QsrDecimals)} QSR for Plasma in {fusionEntryList.Count} entries");
            }
            else
            {
                WriteInfo("No Plasma fusion entries found");
            }

            foreach (var entry in fusionEntryList.List)
            {
                WriteInfo($"  {FormatAmount(entry.QsrAmount, Constants.QsrDecimals)} QSR for {entry.Beneficiary}");
                WriteInfo($"Can be canceled at momentum height: {entry.ExpirationHeight}. Use id {entry.Id} to cancel");
            }
        }

        static async Task ProcessAsync(Plasma.Get options)
        {
            var address = Znn.Instance.DefaultKeyPair.Address;
            var plasmaInfo = await Znn.Instance.Embedded.Plasma.Get(address);

            WriteInfo($"{address} has {plasmaInfo.CurrentPlasma} / {plasmaInfo.MaxPlasma} plasma with {FormatAmount(plasmaInfo.QsrAmount, Constants.QsrDecimals)} QSR fused.");
        }

        static async Task ProcessAsync(Plasma.Fuse options)
        {
            var beneficiary = Address.Parse(options.ToAddress);
            var amount = options.Amount * Constants.OneQsr;

            if (amount < Constants.FuseMinQsrAmount)
            {
                WriteInfo($"Invalid amount: {FormatAmount(amount, Constants.QsrDecimals)} QSR. Minimum staking amount is {FormatAmount(Constants.FuseMinQsrAmount, Constants.QsrDecimals)}");
                return;
            }

            WriteInfo($"Fusing {FormatAmount(amount, Constants.QsrDecimals)} QSR to {beneficiary}");

            await Znn.Instance.Send(Znn.Instance.Embedded.Plasma.Fuse(beneficiary, amount));

            WriteInfo("Done");
        }

        static async Task ProcessAsync(Plasma.Cancel options)
        {
            var address = Znn.Instance.DefaultKeyPair.Address;
            var id = Hash.Parse(options.Id);

            int pageIndex = 0;
            bool found = false;
            bool gotError = false;

            var fusions =
                await Znn.Instance.Embedded.Plasma.GetEntriesByAddress(address);

            while (fusions.List.Length > 0)
            {
                var entry = fusions.List.FirstOrDefault((x) => x.Id == id);
                if (entry != null)
                {
                    found = true;
                    if (entry.ExpirationHeight >
                        (await Znn.Instance.Ledger.GetFrontierMomentum()).Height)
                    {
                        WriteError($"Fuse entry can not be cancelled yet");
                        gotError = true;
                    }
                    break;
                }
                pageIndex++;
                fusions = await Znn.Instance.Embedded.Plasma
                    .GetEntriesByAddress(address, pageIndex: pageIndex);
            }

            if (!found)
            {
                WriteError("Fuse entry was not found");
                return;
            }
            if (gotError)
            {
                return;
            }
            WriteInfo($"Canceling Plasma fuse entry with id {options.Id}");
            await Znn.Instance.Send(Znn.Instance.Embedded.Plasma.Cancel(id));
            WriteInfo("Done");
        }

        #endregion

        #region Sentinel

        static async Task ProcessAsync(Sentinel.List options)
        {
            var address = Znn.Instance.DefaultKeyPair.Address;
            var sentinels = await Znn.Instance.Embedded.Sentinel.GetAllActive();

            bool one = false;

            foreach (var entry in sentinels.List)
            {
                if (entry.Owner == address)
                {
                    if (entry.IsRevocable)
                    {
                        WriteInfo($"Revocation window will close in {FormatDuration(entry.RevokeCooldown)}");
                    }
                    else
                    {
                        WriteInfo($"Revocation window will open in {FormatDuration(entry.RevokeCooldown)}");
                    }
                    one = true;
                }
            }

            if (!one)
            {
                WriteInfo($"No Sentinel registered at address {address}");
            }
        }

        static async Task ProcessAsync(Sentinel.Register options)
        {
            var address = Znn.Instance.DefaultKeyPair.Address;

            var accountInfo =
                await Znn.Instance.Ledger.GetAccountInfoByAddress(address);
            var depositedQsr =
                await Znn.Instance.Embedded.Sentinel.GetDepositedQsr(address);

            WriteInfo($"You have {depositedQsr} QSR deposited for the Sentinel");

            if (accountInfo.Znn < Constants.SentinelRegisterZnnAmount ||
                accountInfo.Qsr < Constants.SentinelRegisterQsrAmount)
            {
                WriteInfo($"Cannot register Sentinel with address {address}");
                WriteInfo($"Required {FormatAmount(Constants.SentinelRegisterZnnAmount, Constants.ZnnDecimals)} ZNN and {FormatAmount(Constants.SentinelRegisterQsrAmount, Constants.QsrDecimals)} QSR");
                WriteInfo($"Available {FormatAmount(accountInfo.Znn.Value, Constants.ZnnDecimals)} ZNN and {FormatAmount(accountInfo.Qsr.Value, Constants.QsrDecimals)} QSR");
                return;
            }

            if (depositedQsr < Constants.SentinelRegisterQsrAmount)
            {
                await Znn.Instance.Send(Znn.Instance.Embedded.Sentinel
                    .DepositQsr(Constants.SentinelRegisterQsrAmount - depositedQsr));
            }
            await Znn.Instance.Send(Znn.Instance.Embedded.Sentinel.Register());
            WriteInfo("Done");
            WriteInfo($"Check after 2 momentums if the Sentinel was successfully registered using sentinel.list command");
        }

        static async Task ProcessAsync(Sentinel.Revoke options)
        {
            var address = Znn.Instance.DefaultKeyPair.Address;

            var entry =
                await Znn.Instance.Embedded.Sentinel.GetByOwner(address);

            if (entry == null)
            {
                WriteInfo($"No Sentinel found for address {address}");
                return;
            }

            if (!entry.IsRevocable)
            {
                WriteInfo($"Cannot revoke Sentinel. Revocation window will open in {FormatDuration(entry.RevokeCooldown)}");
                return;
            }

            await Znn.Instance.Send(Znn.Instance.Embedded.Sentinel.Revoke());

            WriteInfo("Done");
            WriteInfo($"Use receiveAll to collect back the locked amount of ZNN and QSR");
        }

        static async Task ProcessAsync(Sentinel.Collect options)
        {
            await Znn.Instance.Send(Znn.Instance.Embedded.Sentinel.CollectReward());

            WriteInfo("Done");
            WriteInfo($"Use receiveAll to collect your Sentinel reward(s) after 1 momentum");
        }

        static async Task ProcessAsync(Sentinel.WithdrawQsr options)
        {
            var address = Znn.Instance.DefaultKeyPair.Address;

            var depositedQsr =
                await Znn.Instance.Embedded.Sentinel.GetDepositedQsr(address);

            if (depositedQsr == 0)
            {
                WriteInfo($"No deposited QSR to withdraw");
                return;
            }

            WriteInfo($"Withdrawing {FormatAmount(depositedQsr, Constants.QsrDecimals)} QSR ...");

            await Znn.Instance.Send(Znn.Instance.Embedded.Sentinel.WithdrawQsr());

            WriteInfo("Done");
        }

        #endregion

        #region Stake

        static async Task ProcessAsync(Stake.List options)
        {
            var currentTime = DateTimeOffset.Now.ToUnixTimeSeconds();
            var address = Znn.Instance.DefaultKeyPair.Address;

            if (!options.PageIndex.HasValue)
                options.PageIndex = 0;

            if (!options.PageSize.HasValue)
                options.PageSize = 25;

            var stakeList = await Znn.Instance.Embedded.Stake.GetEntriesByAddress(
                address, options.PageIndex.Value, options.PageSize.Value);

            if (stakeList.Count > 0)
            {
                WriteInfo($"Showing {stakeList.List.Length} out of a total of {stakeList.Count} staking entries");
            }
            else
            {
                WriteInfo("No staking entries found");
            }

            foreach (var entry in stakeList.List)
            {
                WriteInfo($"Stake id {entry.Id} with amount {FormatAmount(entry.Amount, Constants.ZnnDecimals)} ZNN");

                if (entry.ExpirationTimestamp > currentTime)
                {
                    WriteInfo($"    Can be revoked in {FormatDuration(entry.ExpirationTimestamp - currentTime)}");
                }
                else
                {
                    WriteInfo("    Can be revoked now");
                }
            }
        }

        static async Task ProcessAsync(Stake.Register options)
        {
            var address = Znn.Instance.DefaultKeyPair.Address;

            var amount = options.Amount * Constants.OneZnn;
            var duration = options.Duration;

            if (duration < 1 || duration > 12)
            {
                WriteInfo($"Invalid duration: ({duration}) {Constants.StakeUnitDurationName}. It must be between 1 and 12");
                return;
            }
            if (amount < Constants.StakeMinZnnAmount)
            {
                WriteInfo($"Invalid amount: {FormatAmount(amount, Constants.ZnnDecimals)} ZNN. Minimum staking amount is {FormatAmount(Constants.StakeMinZnnAmount, Constants.ZnnDecimals)}");
                return;
            }

            AccountInfo balance =
                await Znn.Instance.Ledger.GetAccountInfoByAddress(address);

            if (balance.Znn! < amount)
            {
                WriteInfo("Not enough ZNN to stake");
                return;
            }

            WriteInfo($"Staking {FormatAmount(amount, Constants.ZnnDecimals)} ZNN for {duration} {Constants.StakeUnitDurationName}(s)");

            await Znn.Instance.Send(
                Znn.Instance.Embedded.Stake.Stake(Constants.StakeTimeUnitSec * duration, amount));

            WriteInfo("Done");
        }

        static async Task ProcessAsync(Stake.Revoke options)
        {
            var address = Znn.Instance.DefaultKeyPair.Address;

            var hash = Hash.Parse(options.Id);

            var currentTime = DateTimeOffset.Now.ToUnixTimeSeconds();
            int pageIndex = 0;
            bool one = false;
            bool gotError = false;

            var entries = await Znn.Instance.Embedded.Stake.GetEntriesByAddress(address, pageIndex);

            while (entries.List.Length != 0)
            {
                foreach (var entry in entries.List)
                {
                    if (entry.Id == hash)
                    {
                        if (entry.ExpirationTimestamp > currentTime)
                        {
                            WriteInfo($"Cannot revoke! Try again in {FormatDuration(entry.ExpirationTimestamp - currentTime)}");
                            gotError = true;
                        }
                        one = true;
                    }
                }
                pageIndex++;
                entries = await Znn.Instance.Embedded.Stake.GetEntriesByAddress(address, pageIndex);
            }

            if (gotError)
            {
                return;
            }
            else if (!one)
            {
                WriteError($"No stake entry found with id {hash}");
                return;
            }

            await Znn.Instance.Send(Znn.Instance.Embedded.Stake.Cancel(hash));
            WriteInfo("Done");
            WriteInfo($"Use receiveAll to collect your stake amount and uncollected reward(s) after 2 momentums");
        }

        static async Task ProcessAsync(Stake.Collect options)
        {
            await Znn.Instance.Send(Znn.Instance.Embedded.Stake.CollectReward());

            WriteInfo("Done");
            WriteInfo($"Use receiveAll to collect your stake reward(s) after 1 momentum");
        }

        #endregion

        #region Pillar

        static async Task ProcessAsync(Pillar.List options)
        {
            var pillarList = await Znn.Instance.Embedded.Pillar.GetAll();

            foreach (var pillar in pillarList.List)
            {
                WriteInfo($"#{pillar.Rank + 1} Pillar {pillar.Name} has a delegated weight of {FormatAmount(pillar.Weight, Constants.ZnnDecimals)} ZNN");
                WriteInfo($"    Producer address {pillar.ProducerAddress}");
                WriteInfo($"    Momentums {pillar.CurrentStats.ProducedMomentums} / expected {pillar.CurrentStats.ExpectedMomentums}");
            }
        }

        static async Task ProcessAsync(Pillar.Register options)
        {
            var address = Znn.Instance.DefaultKeyPair.Address;

            var balance =
                await Znn.Instance.Ledger.GetAccountInfoByAddress(address!);
            var qsrAmount =
                (await Znn.Instance.Embedded.Pillar.GetQsrRegistrationCost());
            var depositedQsr =
                await Znn.Instance.Embedded.Pillar.GetDepositedQsr(address);

            if ((balance.Znn < Constants.PillarRegisterZnnAmount ||
                balance.Qsr < qsrAmount) &&
                qsrAmount > depositedQsr)
            {
                WriteInfo($"Cannot register Pillar with address {address}");
                WriteInfo($"Required {FormatAmount(Constants.PillarRegisterZnnAmount, Constants.ZnnDecimals)} ZNN and {FormatAmount(qsrAmount, Constants.QsrDecimals)} QSR");
                WriteInfo($"Available {FormatAmount(balance.Znn.Value, Constants.ZnnDecimals)} ZNN and {FormatAmount(balance.Qsr.Value, Constants.QsrDecimals)} QSR");
                return;
            }

            WriteInfo($"Creating a new Pillar will burn the deposited QSR required for the Pillar slot");

            if (!Confirm("Do you want to proceed?"))
                return;

            var newName = options.Name;
            var ok =
                await Znn.Instance.Embedded.Pillar.CheckNameAvailability(newName);

            while (!ok)
            {
                newName = Ask("This Pillar name is already reserved. Please choose another name for the Pillar");
                ok = await Znn.Instance.Embedded.Pillar.CheckNameAvailability(newName);
            }

            if (depositedQsr < qsrAmount)
            {
                WriteInfo($"Depositing {FormatAmount(qsrAmount - depositedQsr, Constants.QsrDecimals)} QSR for the Pillar registration");
                await Znn.Instance.Send(Znn.Instance.Embedded.Pillar.DepositQsr(qsrAmount - depositedQsr));
            }

            WriteInfo("Registering Pillar ...");

            await Znn.Instance.Send(Znn.Instance.Embedded.Pillar.Register(
                newName,
                Address.Parse(options.ProducerAddress),
                Address.Parse(options.RewardAddress),
                options.GiveBlockRewardPercentage,
                options.GiveDelegateRewardPercentage));
            WriteInfo("Done");
            WriteInfo($"Check after 2 momentums if the Pillar was successfully registered using pillar.list command");
        }

        static async Task ProcessAsync(Pillar.Revoke options)
        {
            var pillarList = await Znn.Instance.Embedded.Pillar.GetAll();

            var ok = false;

            foreach (var pillar in pillarList.List)
            {
                if (String.Equals(options.Name, pillar.Name, StringComparison.OrdinalIgnoreCase))
                {
                    ok = true;

                    if (pillar.IsRevocable)
                    {
                        WriteInfo($"Revoking Pillar {pillar.Name} ...");

                        await Znn.Instance.Send(Znn.Instance.Embedded.Pillar.Revoke(options.Name));

                        WriteInfo($"Use receiveAll to collect back the locked amount of ZNN");
                    }
                    else
                    {
                        WriteInfo($"Cannot revoke Pillar {pillar.Name}. Revocation window will open in {FormatDuration(pillar.RevokeCooldown)}");
                    }
                }
            }

            if (ok)
            {
                WriteInfo("Done");
            }
            else
            {
                WriteInfo("There is no Pillar with this name");
            }
        }

        static async Task ProcessAsync(Pillar.Delegate options)
        {
            WriteInfo($"Delegating to Pillar {options.Name} ...");

            await Znn.Instance.Send(Znn.Instance.Embedded.Pillar.Delegate(options.Name));

            WriteInfo("Done");
        }

        static async Task ProcessAsync(Pillar.Undelegate options)
        {
            WriteInfo($"Delegating ...");

            await Znn.Instance.Send(Znn.Instance.Embedded.Pillar.Undelegate());

            WriteInfo("Done");
        }

        static async Task ProcessAsync(Pillar.Collect options)
        {
            await Znn.Instance.Send(Znn.Instance.Embedded.Pillar.CollectReward());

            WriteInfo("Done");
            WriteInfo($"Use receiveAll to collect your Pillar reward(s) after 1 momentum");
        }

        static async Task ProcessAsync(Pillar.WithdrawQsr options)
        {
            var address = Znn.Instance.DefaultKeyPair.Address;

            var depositedQsr =
                await Znn.Instance.Embedded.Pillar.GetDepositedQsr(address);

            if (depositedQsr == 0)
            {
                WriteInfo("No deposited QSR to withdraw");
                return;
            }

            WriteInfo($"Withdrawing {FormatAmount(depositedQsr, Constants.QsrDecimals)} QSR ...");

            await Znn.Instance.Send(Znn.Instance.Embedded.Pillar.WithdrawQsr());

            WriteInfo("Done");
        }

        #endregion

        #region Token

        static async Task ProcessAsync(Token.List options)
        {
            if (!options.PageIndex.HasValue)
                options.PageIndex = 0;

            if (!options.PageSize.HasValue)
                options.PageSize = 25;

            var tokenList = await Znn.Instance.Embedded.Token.GetAll(options.PageIndex.Value, options.PageSize.Value);

            foreach (var token in tokenList.List)
            {
                if (token.TokenStandard == TokenStandard.ZnnZts || token.TokenStandard == TokenStandard.QsrZts)
                {
                    WriteInfo(String.Format("{0} with symbol {1} and standard {2}",
                        token.TokenStandard == TokenStandard.ZnnZts ? token.Name : token.Name,
                        token.TokenStandard == TokenStandard.ZnnZts ? token.Symbol : token.Symbol,
                        token.TokenStandard == TokenStandard.ZnnZts ? token.TokenStandard : token.TokenStandard));
                    WriteInfo(String.Format("   Created by {0}",
                        token.TokenStandard == TokenStandard.ZnnZts ? token.Owner : token.Owner));
                    WriteInfo(String.Format("   {0} has {1} decimals, {2}, {3}, and {4}",
                        token.TokenStandard == TokenStandard.ZnnZts ? token.Name : token.Name,
                        token.Decimals,
                        token.IsMintable ? " is mintable" : " is not mintable",
                        token.IsBurnable ? "can be burned" : "cannot be burned",
                        token.IsUtility ? " is a utility coin" : " is not a utility coin"));
                    WriteInfo($"   The total supply is {FormatAmount(token.TotalSupply, token.Decimals)} and the maximum supply is ${FormatAmount(token.MaxSupply, token.Decimals)}");
                }
                else
                {
                    WriteInfo($"Token {token.Name} with symbol {token.Symbol} and standard {token.TokenStandard}");
                    WriteInfo($"   Issued by {token.Owner}");
                    WriteInfo(String.Format("   {0} has {1} decimals, {2}, {3}, and {4}",
                        token.Name,
                        token.Decimals,
                        token.IsMintable ? "can be minted" : "cannot be minted",
                        token.IsBurnable ? "can be burned" : "cannot be burned",
                        token.IsUtility ? " is a utility token" : " is not a utility token"));
                }
                WriteInfo($"   Domain `{token.Domain}`");
            }
        }

        static async Task ProcessAsync(Token.GetByStandard options)
        {
            var tokenStandard = TokenStandard.Parse(options.TokenStandard);
            var token = await Znn.Instance.Embedded.Token.GetByZts(tokenStandard);

            if (token == null)
            {
                WriteError("The token does not exist");
                return;
            }

            var type = "Token";

            if (token.TokenStandard == TokenStandard.QsrZts ||
                token.TokenStandard == TokenStandard.ZnnZts)
            {
                type = "Coin";
            }

            WriteInfo($"{type} {token.Name} with symbol {token.Symbol} and standard {token.TokenStandard}");
            WriteInfo($"   Created by {token.Owner}");
            WriteInfo($"   The total supply is {FormatAmount(token.TotalSupply, token.Decimals)} and a maximum supply is {FormatAmount(token.MaxSupply, token.Decimals)}");
            WriteInfo(String.Format("   The token has {0} decimals {1} and {2}",
                token.Decimals,
                token.IsMintable ? "can be minted" : "cannot be minted",
                token.IsBurnable ? "can be burned" : "cannot be burned"));
        }

        static async Task ProcessAsync(Token.GetByOwner options)
        {
            var ownerAddress = Address.Parse(options.OwnerAddress);

            var type = "Token";

            var tokens = await Znn.Instance.Embedded.Token.GetByOwner(ownerAddress);

            foreach (var token in tokens.List)
            {
                type = "Token";

                if (token.TokenStandard == TokenStandard.QsrZts ||
                    token.TokenStandard == TokenStandard.ZnnZts)
                {
                    type = "Coin";
                }

                WriteInfo($"{type} {token.Name} with symbol {token.Symbol} and standard {token.TokenStandard}");
                WriteInfo($"   Created by {token.Owner}");
                WriteInfo($"   The total supply is {FormatAmount(token.TotalSupply, token.Decimals)} and a maximum supply is {FormatAmount(token.MaxSupply, token.Decimals)}");
                WriteInfo(String.Format("   The token has {0} decimals {1} and {2}",
                    token.Decimals,
                    token.IsMintable ? "can be minted" : "cannot be minted",
                    token.IsBurnable ? "can be burned" : "cannot be burned"));
            }
        }

        static async Task ProcessAsync(Token.Issue options)
        {
            if (!Regex.IsMatch(options.Name, "^([a-zA-Z0-9]+[-._]?)*[a-zA-Z0-9]$"))
            {
                WriteError("The ZTS name contains invalid characters");
                return;
            }

            if (!Regex.IsMatch(options.Symbol, "^[A-Z0-9]+$"))
            {
                WriteError("The ZTS symbol must be all uppercase");
                return;
            }

            if (String.IsNullOrEmpty(options.Domain) || !Regex.IsMatch(options.Domain, "^([A-Za-z0-9][A-Za-z0-9-]{0,61}[A-Za-z0-9]\\.)+[A-Za-z]{2,}$"))
            {
                WriteError("Invalid domain\nExamples of valid domain names:\n    zenon.network\n    www.zenon.network\n    quasar.zenon.network\n    zenon.community\nExamples of invalid domain names:\n    zenon.network/index.html\n    www.zenon.network/quasar");
                return;
            }

            if (String.IsNullOrEmpty(options.Name) || options.Name.Length > 40)
            {
                WriteError($"Invalid ZTS name length (min 1, max 40, current {options.Name.Length}");
            }

            if (String.IsNullOrEmpty(options.Symbol) || options.Symbol.Length > 10)
            {
                WriteError($"Invalid ZTS symbol length (min 1, max 10, current {options.Symbol.Length}");
            }

            if (options.Domain.Length > 128)
            {
                WriteError($"Invalid ZTS domain length (min 0, max 128, current {options.Domain.Length})");
            }

            bool mintable;
            if (options.IsMintable == "0" || String.Equals(options.IsMintable, "false", StringComparison.OrdinalIgnoreCase))
            {
                mintable = false;
            }
            else if (options.IsMintable == "1" || String.Equals(options.IsMintable, "true", StringComparison.OrdinalIgnoreCase))
            {
                mintable = true;
            }
            else
            {
                WriteError("Mintable flag variable of type \"bool\" should be provided as either \"true\", \"false\", \"1\" or \"0\"");
                return;
            }

            bool burnable;
            if (options.IsBurnable == "0" || String.Equals(options.IsBurnable, "false", StringComparison.OrdinalIgnoreCase))
            {
                burnable = false;
            }
            else if (options.IsBurnable == "1" || String.Equals(options.IsBurnable, "true", StringComparison.OrdinalIgnoreCase))
            {
                burnable = true;
            }
            else
            {
                WriteError("Burnable flag variable of type \"bool\" should be provided as either \"true\", \"false\", \"1\" or \"0\"");
                return;
            }

            bool utility;
            if (options.IsUtility == "0" || String.Equals(options.IsUtility, "false", StringComparison.OrdinalIgnoreCase))
            {
                utility = false;
            }
            else if (options.IsUtility == "1" || String.Equals(options.IsUtility, "true", StringComparison.OrdinalIgnoreCase))
            {
                utility = true;
            }
            else
            {
                WriteError("Utility flag variable of type \"bool\" should be provided as either \"true\", \"false\", \"1\" or \"0\"");
                return;
            }

            var totalSupply = options.TotalSupply;
            var maxSupply = options.MaxSupply;
            var decimals = options.Decimals;

            WriteInfo($"{mintable} {burnable} {utility}");
            return;

            if (mintable == true)
            {
                if (maxSupply < totalSupply)
                {
                    WriteError("Max supply must to be larger than the total supply");
                    return;
                }
                if (maxSupply > (1 << 53))
                {
                    WriteError($"Max supply must to be less than {((1 << 53)) - 1}");
                    return;
                }
            }
            else
            {
                if (maxSupply != totalSupply)
                {
                    WriteError("Max supply must be equal to totalSupply for non-mintable tokens");
                    return;
                }
                if (totalSupply == 0)
                {
                    WriteError("Total supply cannot be \"0\" for non-mintable tokens");
                    return;
                }
            }

            WriteInfo("Issuing a new ZTS token will burn 1 ZNN");

            if (!Confirm("Do you want to proceed?"))
                return;

            WriteInfo($"Issuing {options.Name} ZTS token ...");

            await Znn.Instance.Send(
                Znn.Instance.Embedded.Token.IssueToken(
                    options.Name,
                    options.Symbol,
                    options.Domain,
                    totalSupply,
                    maxSupply,
                    decimals,
                    mintable,
                    burnable,
                    utility));

            WriteInfo("Done");
        }

        static async Task ProcessAsync(Token.Mint options)
        {
            var tokenStandard = TokenStandard.Parse(options.TokenStandard);
            var amount = options.Amount;
            var mintAddress = Address.Parse(options.ReceiveAddress);
            var token = await Znn.Instance.Embedded.Token.GetByZts(tokenStandard);

            if (token == null)
            {
                WriteError("The token does not exist");
                return;
            }
            else if (!token.IsMintable)
            {
                WriteError("The token is not mintable");
                return;
            }

            WriteInfo("Minting ZTS token ...");

            await Znn.Instance.Send(
                Znn.Instance.Embedded.Token.MintToken(tokenStandard, amount, mintAddress));

            WriteInfo("Done");
        }

        static async Task ProcessAsync(Token.Burn options)
        {
            var address = Znn.Instance.DefaultKeyPair.Address;
            var tokenStandard = TokenStandard.Parse(options.TokenStandard);
            var amount = options.Amount;

            var info =
                await Znn.Instance.Ledger.GetAccountInfoByAddress(address);
            var ok = true;

            foreach (var entry in info.BalanceInfoList)
            {
                if (entry.Token.TokenStandard == tokenStandard &&
                    entry.Balance < amount)
                {
                    WriteError($"You only have {FormatAmount(entry.Balance.Value, entry.Token.Decimals)} {entry.Token.Symbol} tokens");
                    ok = false;
                    break;
                }
            }

            if (!ok)
                return;

            WriteInfo($"Burning {options.TokenStandard} ZTS token ...");

            await Znn.Instance.Send(
                Znn.Instance.Embedded.Token.BurnToken(tokenStandard, amount));

            WriteInfo("Done");
        }

        static async Task ProcessAsync(Token.TransferOwnership options)
        {
            WriteInfo("Transferring ZTS token ownership ...");

            var address = Znn.Instance.DefaultKeyPair.Address;
            var tokenStandard = TokenStandard.Parse(options.TokenStandard);
            var newOwnerAddress = Address.Parse(options.NewOwnerAddress);
            var token = await Znn.Instance.Embedded.Token.GetByZts(tokenStandard);

            if (token.Owner != address)
            {
                WriteError($"Not owner of token {tokenStandard}");
                return;
            }

            await Znn.Instance.Send(Znn.Instance.Embedded.Token.UpdateToken(
                tokenStandard, newOwnerAddress, token.IsMintable, token.IsBurnable));

            WriteInfo("Done");
        }

        static async Task ProcessAsync(Token.DisableMint options)
        {
            WriteInfo("Disabling ZTS token mintable flag ...");

            var address = Znn.Instance.DefaultKeyPair.Address;
            var tokenStandard = TokenStandard.Parse(options.TokenStandard);
            var token = await Znn.Instance.Embedded.Token.GetByZts(tokenStandard);

            if (token.Owner != address)
            {
                WriteError($"Not owner of token {tokenStandard}");
                return;
            }

            await Znn.Instance.Send(Znn.Instance.Embedded.Token.UpdateToken(
                tokenStandard, token.Owner, false, token.IsBurnable));

            WriteInfo("Done");
        }

        #endregion

        #region Htlc

        static async Task ProcessAsync(Htlc.Get options)
        {
            var id = Hash.Parse(options.Id);
            var htlc = await Znn.Instance.Embedded.Htlc.GetHtlcInfoById(id);

            if (htlc == null)
            {
                WriteError($"The htlc id {options.Id} does not exist");
                return;
            }

            var token = await Znn.Instance.Embedded.Token.GetByZts(htlc.TokenStandard);

            var currentTime = DateTimeOffset.Now.ToUnixTimeSeconds();

            WriteInfo($"Htlc id {htlc.Id} with amount {FormatAmount(htlc.Amount, token.Decimals)} {token.Symbol}");
            if (htlc.ExpirationTime > currentTime)
                WriteInfo($"   Can be reclaimed in {FormatDuration(htlc.ExpirationTime - currentTime)} by {htlc.TimeLocked}");
            else
                WriteInfo($"   Can be reclaimed now by {htlc.TimeLocked}");
            WriteInfo($"   Can be unlocked by {htlc.HashLocked} with hashlock {BytesUtils.ToHexString(htlc.HashLock)} hashtype {htlc.HashType}");
        }

        static async Task ProcessAsync(Htlc.GetByTimeLocked options)
        {
            var currentTime = DateTimeOffset.Now.ToUnixTimeSeconds();
            var address = Znn.Instance.DefaultKeyPair.Address;

            if (!options.PageIndex.HasValue)
                options.PageIndex = 0;

            if (!options.PageSize.HasValue)
                options.PageSize = 25;

            var result = await Znn.Instance.Embedded.Htlc
                .GetHtlcInfosByTimeLockedAddress(address, options.PageIndex.Value, options.PageSize.Value);

            if (result == null || result.Count == 0)
            {
                WriteInfo("No time locked htlc entries found");
                return;
            }

            foreach (var htlc in result.List)
            {
                var token = await Znn.Instance.Embedded.Token.GetByZts(htlc.TokenStandard);

                WriteInfo($"Htlc id {htlc.Id} with amount {FormatAmount(htlc.Amount, token.Decimals)} {token.Symbol}");
                if (htlc.ExpirationTime > currentTime)
                    WriteInfo($"   Can be reclaimed in {FormatDuration(htlc.ExpirationTime - currentTime)} by {htlc.TimeLocked}");
                else
                    WriteInfo($"   Can be reclaimed now by {address}");
                WriteInfo($"   Can be unlocked by {htlc.HashLocked} with hashlock {BytesUtils.ToHexString(htlc.HashLock)} hashtype {htlc.HashType}");
            }
        }

        static async Task ProcessAsync(Htlc.GetByHashLocked options)
        {
            var currentTime = DateTimeOffset.Now.ToUnixTimeSeconds();
            var address = Znn.Instance.DefaultKeyPair.Address;

            if (!options.PageIndex.HasValue)
                options.PageIndex = 0;

            if (!options.PageSize.HasValue)
                options.PageSize = 25;

            var result = await Znn.Instance.Embedded.Htlc
                .GetHtlcInfosByHashLockedAddress(address, options.PageIndex.Value, options.PageSize.Value);

            if (result == null || result.Count == 0)
            {
                WriteInfo("No hash locked htlc entries found");
                return;
            }

            foreach (var htlc in result.List)
            {
                var token = await Znn.Instance.Embedded.Token.GetByZts(htlc.TokenStandard);

                WriteInfo($"Htlc id {htlc.Id} with amount {FormatAmount(htlc.Amount, token.Decimals)} {token.Symbol}");
                if (htlc.ExpirationTime > currentTime)
                    WriteInfo($"   Can be reclaimed in {FormatDuration(htlc.ExpirationTime - currentTime)} by {htlc.TimeLocked}");
                else
                    WriteInfo($"   Can be reclaimed now by {address}");
                WriteInfo($"   Can be unlocked by {htlc.HashLocked} with hashlock {BytesUtils.ToHexString(htlc.HashLock)} hashtype {htlc.HashType}");
            }
        }

        static async Task ProcessAsync(Htlc.Create options)
        {
            var currentTime = DateTimeOffset.Now.ToUnixTimeSeconds();
            var address = Znn.Instance.DefaultKeyPair.Address;
            var hashLocked = Address.Parse(options.HashLockedAddress);

            if (!options.HashType.HasValue)
            {
                options.HashType = HashTypeSha3256;
            }
            if (!options.KeyMaxSize.HasValue)
            {
                options.KeyMaxSize = 32;
            }

            if (options.HashType != HashTypeSha3256 && options.HashType != HashTypeSha2256)
            {
                WriteError($"Invalid hash type. Hash type {options.HashType} not supported.");
                return;
            }

            byte[] hashLock = new byte[0];

            try
            {
                hashLock = BytesUtils.FromHexString(options.HashLock);
            }
            catch
            {
                WriteError($"Invalid hash lock.");
                return;
            }

            TokenStandard tokenStandard;
            if (String.Equals(options.TokenStandard, "ZNN", StringComparison.OrdinalIgnoreCase))
            {
                tokenStandard = TokenStandard.ZnnZts;
            }
            else if (String.Equals(options.TokenStandard, "QSR", StringComparison.OrdinalIgnoreCase))
            {
                tokenStandard = TokenStandard.QsrZts;
            }
            else
            {
                tokenStandard = TokenStandard.Parse(options.TokenStandard);
            }

            var info =
                await Znn.Instance.Ledger.GetAccountInfoByAddress(address);

            bool ok = true;
            bool found = false;
            long amount = 0;
            Zenon.Model.NoM.Token? token = null;

            foreach (var item in info.BalanceInfoList)
            {
                if (item.Token.TokenStandard == tokenStandard)
                {
                    amount = options.Amount * item.Token.DecimalsExponent;
                    token = item.Token;

                    if (item.Balance < amount)
                    {
                        WriteError($"You only have {FormatAmount(item.Balance.Value, item.Token.Decimals)} {item.Token.Symbol} tokens");
                        ok = false;
                        break;
                    }
                    found = true;
                }
            }

            if (!ok) return;
            if (!found)
            {
                WriteError($"You only have {FormatAmount(0, 0)} {tokenStandard} tokens");
                return;
            }

            var expirationTime = currentTime + options.ExpirationTime;

            var block = Znn.Instance.Embedded.Htlc
                .CreateHtlc(address, hashLocked, tokenStandard, amount, expirationTime, options.HashType.Value, options.KeyMaxSize.Value, hashLock);

            WriteInfo($"Creating htlc with amount {FormatAmount(amount, token.Decimals)} {token.Symbol}");
            if (expirationTime > currentTime)
                WriteInfo($"   Can be reclaimed in {FormatDuration(expirationTime - currentTime)} by {address}");
            else
                WriteInfo($"   Can be reclaimed now by {address}");
            WriteInfo($"   Can be unlocked by {hashLocked} with hashlock {BytesUtils.ToHexString(hashLock)} hashtype {options.HashType}");

            await Znn.Instance.Send(block);

            WriteInfo("Done");
        }

        static async Task ProcessAsync(Htlc.Reclaim options)
        {
            var currentTime = DateTimeOffset.Now.ToUnixTimeSeconds();
            var address = Znn.Instance.DefaultKeyPair.Address;
            var id = Hash.Parse(options.Id);

            var htlc = await Znn.Instance.Embedded.Htlc.GetHtlcInfoById(id);

            if (htlc == null)
            {
                WriteError($"The htlc id {id} does not exist");
                return;
            }

            if (htlc.ExpirationTime > currentTime)
            {
                WriteError($"Cannot reclaim htlc. Try again in {FormatDuration(htlc.ExpirationTime - currentTime)}.");
                return;
            }

            var token = await Znn.Instance.Embedded.Token.GetByZts(htlc.TokenStandard);

            WriteInfo($"Reclaiming htlc id {htlc.Id} with amount {FormatAmount(htlc.Amount, token.Decimals)} {token.Symbol}");

            await Znn.Instance.Send(Znn.Instance.Embedded.Htlc.ReclaimHtlc(address, id));

            WriteInfo("Done");
            WriteInfo($"Use receiveAll to collect your htlc amount after 2 momentums");
        }

        static async Task ProcessAsync(Htlc.Unlock options)
        {
            var currentTime = DateTimeOffset.Now.ToUnixTimeSeconds();
            var address = Znn.Instance.DefaultKeyPair.Address;
            var id = Hash.Parse(options.Id);

            var htlc = await Znn.Instance.Embedded.Htlc.GetHtlcInfoById(id);

            if (htlc == null)
            {
                WriteError($"The htlc id {id} does not exist");
                return;
            }

            if (htlc.HashLocked != address)
            {
                WriteError($"Cannot unlock htlc. Permission denied");
                return;
            }

            if (htlc.ExpirationTime <= currentTime)
            {
                WriteError($"Cannot unlock htlc. Time lock expired.");
                return;
            }

            if (options.Preimage == null)
            {
                WriteInfo("Insert preimage:");
                options.Preimage = ReadPassword();
            }

            if (String.IsNullOrEmpty(options.Preimage))
            {
                WriteError("Cannot unlock htlc. Invalid pre-image");
                return;
            }

            if (options.Preimage.Length > htlc.KeyMaxSize)
            {
                WriteError("Cannot unlock htlc. Invalid pre-image size");
                return;
            }

            var token = await Znn.Instance.Embedded.Token.GetByZts(htlc.TokenStandard);

            WriteInfo($"Unlocking htlc id {htlc.Id} with amount {FormatAmount(htlc.Amount, token.Decimals)} {token.Symbol}");

            await Znn.Instance.Send(Znn.Instance.Embedded.Htlc.UnlockHtlc(address, id, Encoding.UTF8.GetBytes(options.Preimage)));

            WriteInfo("Done");
            WriteInfo($"Use receiveAll to collect your htlc amount after 2 momentums");
        }

        static async Task ProcessAsync(Htlc.Inspect options)
        {
            var currentTime = DateTimeOffset.Now.ToUnixTimeSeconds();
            var hash = Hash.Parse(options.BlockHash);

            var block = await Znn.Instance.Ledger.GetAccountBlockByHash(hash);

            if (block == null)
            {
                WriteError($"The account block {hash} does not exist");
                return;
            }

            if (block.Address != Address.HtlcAddress ||
                block.BlockType != BlockTypeEnum.ContractReceive)
            {
                WriteError($"The account block was not received by the htlc contract address");
                return;
            }

            if (block.PairedAccountBlock == null ||
                block.PairedAccountBlock.BlockType != BlockTypeEnum.UserSend)
            {
                WriteError($"The account block is not send by an user.");
                return;
            }

            var f = Definitions.Htlc.Entries
                .Where(x => AbiFunction.ExtractSignature(x.EncodeSignature()).SequenceEqual(AbiFunction.ExtractSignature(block.PairedAccountBlock.Data)))
                .Select(x => new AbiFunction(x.Name, x.Inputs))
                .FirstOrDefault();

            if (f == null)
            {
                WriteError($"The account block contains invalid data.");
                return;
            }

            if (String.Equals(f.Name, "UnlockHtlc", StringComparison.OrdinalIgnoreCase))
            {
                var args = f.Decode(block.PairedAccountBlock.Data);

                if (args.Length != 2)
                {
                    WriteError($"The account block has an invalid unlock argument length");
                    return;
                }

                WriteInfo($"Unlock htlc id {args[0]} with pre-image: {Encoding.UTF8.GetString((byte[])args[1])}");
            }
            else if (String.Equals(f.Name, "ReclaimHtlc", StringComparison.OrdinalIgnoreCase))
            {
                var args = f.Decode(block.PairedAccountBlock.Data);

                if (args.Length != 1)
                {
                    WriteError($"The account block has an invalid reclaim argument length");
                    return;
                }

                WriteInfo($"Reclaim htlc id {args[0]}");
            }
            else if (String.Equals(f.Name, "CreateHtlc", StringComparison.OrdinalIgnoreCase))
            {
                var args = f.Decode(block.PairedAccountBlock.Data);

                if (args.Length != 5)
                {
                    WriteError($"The account block has an invalid create argument length");
                    return;
                }

                var expirationTime = (long)((BigInteger)args[1]);
                var hashLock = (byte[])args[4];
                var amount = block.PairedAccountBlock.Amount;
                var token = block.PairedAccountBlock.Token;

                WriteInfo($"Create htlc {args[0]} {FormatAmount(amount, token.Decimals)} {token.Symbol} {expirationTime} {args[2]} {args[3]} {BytesUtils.ToHexString(hashLock)}");
            }
            else
            {
                WriteError($"The account block contains an unknown function call");
                return;
            }
        }

        static async Task ProcessAsync(Htlc.Monitor options)
        {
            var id = Hash.Parse(options.Id);

            var htlc = await Znn.Instance.Embedded.Htlc.GetHtlcInfoById(id);

            if (htlc == null)
            {
                WriteError($"The htlc id {id} does not exist");
                return;
            }

            await MonitorAsync(htlc);
        }

        static async Task ProcessAsync(Htlc.MonitorAll options)
        {
            var address = Znn.Instance.DefaultKeyPair.Address;

            var timeHtlc = await Znn.Instance.Embedded.Htlc.GetHtlcInfosByTimeLockedAddress(address);
            var hashHtlc = await Znn.Instance.Embedded.Htlc.GetHtlcInfosByHashLockedAddress(address);

            await MonitorAsync(timeHtlc.List.Concat(hashHtlc.List).ToArray());
        }

        static async Task MonitorAsync(params HtlcInfo[] htlcs)
        {
            if (htlcs == null || htlcs.Length == 0)
                return;

            foreach (var htlc in htlcs)
            {
                WriteInfo($"Monitoring htlc id {htlc.Id}");
            }

            var queue = new BlockingCollection<Hash>();

            WriteInfo("Subscribing for htlc-contract events ...");
            await Znn.Instance.Subscribe.ToAllAccountBlocks((json) =>
            {
                for (var i = 0; i < json.Length; i += 1)
                {
                    var tx = json[i];
                    if (tx.Value<string>("address") != Address.HtlcAddress.ToString())
                        continue;

                    var hash = Hash.Parse(tx.Value<string>("hash"));
                    WriteInfo($"receiving transaction with hash {hash}");
                    queue.Add(hash);
                }
            });
            WriteInfo("Subscribed successfully!");

            var currentTime = DateTimeOffset.Now.ToUnixTimeSeconds();
            var expired = new List<Hash>();

            while (true)
            {
                foreach (var htlc in htlcs)
                {
                    if (!expired.Contains(htlc.Id) && htlc.ExpirationTime <= currentTime)
                    {
                        WriteInfo($"Htlc id {htlc.Id} expired");
                        expired.Add(htlc.Id);
                    }
                }

                Hash? hash;
                if (queue.TryTake(out hash))
                {
                    foreach (var htlc in htlcs)
                    {
                        var block = await Znn.Instance.Ledger.GetAccountBlockByHash(hash);

                        if (block.BlockType != BlockTypeEnum.ContractReceive)
                            continue;

                        if (block.PairedAccountBlock == null ||
                            block.PairedAccountBlock.BlockType != BlockTypeEnum.UserSend)
                            continue;

                        if (block.DescendantBlocks == null ||
                            block.DescendantBlocks.Length == 0)
                            continue;

                        var f = Definitions.Htlc.Entries
                            .Where(x => AbiFunction.ExtractSignature(x.EncodeSignature()).SequenceEqual(AbiFunction.ExtractSignature(block.PairedAccountBlock.Data)))
                            .Select(x => new AbiFunction(x.Name, x.Inputs))
                            .FirstOrDefault();

                        if (f == null)
                            continue;

                        if (String.Equals(f.Name, "UnlockHtlc", StringComparison.OrdinalIgnoreCase))
                        {
                            if (block.PairedAccountBlock.Address != htlc.HashLocked)
                                continue;

                            var args = f.Decode(block.PairedAccountBlock.Data);

                            if (args.Length != 2)
                                continue;

                            if (args[0].ToString() != htlc.Id.ToString())
                                continue;

                            if (block.DescendantBlocks.Any(x =>
                                x.BlockType == BlockTypeEnum.ContractSend &&
                                x.ToAddress == htlc.HashLocked &&
                                x.TokenStandard == htlc.TokenStandard &&
                                x.Amount == htlc.Amount))
                            {
                                WriteInfo($"Htlc id {htlc.Id} unlocked with pre-image: {Encoding.UTF8.GetString((byte[])args[1])}");
                                break;
                            }
                        }
                        else if (String.Equals(f.Name, "ReclaimHtlc", StringComparison.OrdinalIgnoreCase))
                        {
                            if (block.PairedAccountBlock.Address != htlc.TimeLocked)
                                continue;

                            var args = f.Decode(block.PairedAccountBlock.Data);

                            if (args.Length != 1)
                                continue;

                            if (args[0].ToString() != htlc.Id.ToString())
                                continue;

                            if (block.DescendantBlocks.Any(x =>
                                x.BlockType == BlockTypeEnum.ContractSend &&
                                x.ToAddress == htlc.TimeLocked &&
                                x.TokenStandard == htlc.TokenStandard &&
                                x.Amount == htlc.Amount))
                            {
                                WriteInfo($"Htlc id {htlc.Id} reclaimed");
                                break;
                            }
                        }
                    }
                }

                await Task.Delay(1000);
            }
        }

        #endregion

        #region Wallet

        static int Process(Wallet.List options)
        {
            var stores = Znn.Instance.KeyStoreManager.ListAllKeyStores();

            if (stores.Length != 0)
            {
                WriteInfo("Available keyStores:");

                foreach (var store in stores)
                {
                    WriteInfo(Path.GetFileName(store));
                }
            }
            else
            {
                WriteInfo("No keyStores found");
            }

            return 0;
        }

        static int Process(Wallet.CreateNew options)
        {
            var keyStore = Znn.Instance.KeyStoreManager.CreateNew(options.Passphrase, options.KeyStoreName);

            WriteInfo($"keyStore successfully created: {Path.GetFileName(keyStore)}");

            return 0;
        }

        static int Process(Wallet.CreateFromMnemonic options)
        {
            var keyStore = Znn.Instance.KeyStoreManager.CreateFromMnemonic(options.Mnemonic, options.Passphrase, options.KeyStoreName);

            WriteInfo($"keyStore successfully from mnemonic: {Path.GetFileName(keyStore)}");

            return 0;
        }

        static int Process(Wallet.DumpMnemonic options)
        {
            WriteInfo($"Mnemonic for keyStore File: {Znn.Instance.DefaultKeyStorePath}");

            WriteInfo(Znn.Instance.DefaultKeyStore.Mnemonic);

            return 0;
        }

        static int Process(Wallet.DeriveAddresses options)
        {
            WriteInfo($"Addresses for keyStore File: {Znn.Instance.DefaultKeyStorePath}");

            var addresses = Znn.Instance.DefaultKeyStore.DeriveAddressesByRange(options.Start, options.End);

            for (int i = 0; i < options.End - options.Start; i += 1)
            {
                WriteInfo($"  {i + options.Start}\t{addresses[i]}");
            }

            return 0;
        }

        static int Process(Wallet.Export options)
        {
            File.Copy(Znn.Instance.DefaultKeyStorePath, options.FilePath!);

            WriteInfo("Done! Check the current directory");

            return 0;
        }

        #endregion

        #region Helpers

        static void WriteError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("Error ");
            Console.ResetColor();
            Console.WriteLine(message);
        }

        static void WriteInfo(string message)
        {
            Console.WriteLine(message);
        }

        static string? ReadPassword()
        {
            string? password = null;
            while (true)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter)
                    break;
                password += key.KeyChar;
            }
            return password;
        }

        static bool Confirm(string message, bool defaultValue = false)
        {
            while (true)
            {
                Console.WriteLine(message + " (Y/N):");
                var key = Console.ReadKey();
                if (key.Key == ConsoleKey.Y)
                    return true;
                else if (key.Key == ConsoleKey.N)
                    return false;
                else if (key.Key == ConsoleKey.Enter)
                    return defaultValue;
                else
                    Console.WriteLine($"Invalid value: {key}");
            }
        }

        static string Ask(string message)
        {
            Console.WriteLine(message);
            return Console.ReadLine();
        }

        static string FormatAmount(long amount, long decimals)
        {
            return (amount / Math.Pow(10, decimals)).ToString($"0." + new String('0', (int)decimals));
        }

        static string FormatDuration(long seconds)
        {
            return TimeSpan.FromSeconds(seconds).ToString();
        }

        static void ThrowError(string message)
        {
            throw new Exception(message);
        }

        #endregion
    }
}