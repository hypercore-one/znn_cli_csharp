using CommandLine;

namespace ZenonCli.Options
{
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
            [Value(0, MetaName = "hashType", Default = 0, HelpText = "0 = SHA3-256, 1 = SHA-256")]
            public int? HashType { get; set; }

            [Value(1, MetaName = "keySize", Default = 32, HelpText = "The size of the preimage.")]
            public int? KeySize { get; set; }
        }
    }
}
