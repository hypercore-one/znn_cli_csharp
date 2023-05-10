using CommandLine;

namespace ZenonCli.Options
{
    public class Bridge
    {
        [Verb("bridge.info", HelpText = "Get the bridge information")]
        public class Info : ConnectionOptions
        { }

        [Verb("bridge.security", HelpText = "Get the bridge security information")]
        public class SecurityInfo : ConnectionOptions
        { }

        [Verb("bridge.timeChallenges", HelpText = "List all bridge time challenges")]
        public class TimeChallengesInfo : ConnectionOptions
        { }

        [Verb("bridge.emergency", HelpText = "Put the bridge contract in emergency mode. Can only be called by administrator.")]
        public class Emergency : KeyStoreAndConnectionOptions
        { }

        [Verb("bridge.halt", HelpText = "Halt bridge operations.")]
        public class Halt : KeyStoreAndConnectionOptions
        {
            [Value(0, MetaName = "signature", HelpText = "Only non administrators needs a TSS signature with the current tssNonce.")]
            public string? Signature { get; set; }
        }

        [Verb("bridge.unhalt", HelpText = "Unhalt bridge operations. Can only be called by the administrator.")]
        public class Unhalt : KeyStoreAndConnectionOptions
        { }

        [Verb("bridge.enableKeyGen", HelpText = "Enable bridge key generation. Can only be called by the administrator.")]
        public class EnableKeyGen : KeyStoreAndConnectionOptions
        { }

        [Verb("bridge.disableKeyGen", HelpText = "Disable bridge key generation. Can only be called by the administrator.")]
        public class DisableKeyGen : KeyStoreAndConnectionOptions
        { }

        [Verb("bridge.setMetadata", HelpText = "Set the bridge metadata.")]
        public class SetMetadata : KeyStoreAndConnectionOptions
        {
            [Value(0, MetaName = "metadata")]
            public string? Metadata { get; set; }
        }

        [Verb("bridge.setRedeemDelay", HelpText = "Set the bridge redeem delay in momentums.")]
        public class SetRedeemDelay : KeyStoreAndConnectionOptions
        {
            [Value(0, MetaName = "redeemDelay", Required = true)]
            public long? RedeemDelay { get; set; }
        }

        public class Admin
        {
            [Verb("bridge.admin.nominate", HelpText = "Nominate bridge guardians. Can only be called by the administrator.")]
            public class NominateGuardians : KeyStoreAndConnectionOptions
            {
                [Value(0, MetaName = "guardians", Required = true)]
                public IEnumerable<string>? Guardians { get; set; }
            }

            [Verb("bridge.admin.propose", HelpText = "Propose bridge administrator. Can only be called by a guardian if the bridge contract is in emergency mode.")]
            public class ProposeAdministrator : KeyStoreAndConnectionOptions
            {
                [Value(0, MetaName = "admin", Required = true)]
                public string? Administrator { get; set; }
            }

            [Verb("bridge.admin.change", HelpText = "Change bridge administrator. Can only be called by the administrator.")]
            public class ChangeAdministrator : KeyStoreAndConnectionOptions
            {
                [Value(0, MetaName = "admin", Required = true)]
                public string? Administrator { get; set; }
            }
        }

        public class Wrap
        {
            [Verb("bridge.wrap.list", HelpText = "List all wrap token requests")]
            public class List : ConnectionOptions
            { }

            [Verb("bridge.wrap.listByAddress", HelpText = "List all wrap token requests by address")]
            public class ListByAddress : ConnectionOptions
            {
                [Value(0, MetaName = "address", Required = true, HelpText = "The address")]
                public string? Address { get; set; }
            }

            [Verb("bridge.wrap.listUnsigned", HelpText = "List all unsigned wrap token requests")]
            public class ListUnsigned : ConnectionOptions
            { }

            [Verb("bridge.wrap.get", HelpText = "Get wrap token request by id")]
            public class Get : ConnectionOptions
            {
                [Value(0, MetaName = "id", Required = true)]
                public string? Id { get; set; }
            }

            [Verb("bridge.wrap.update", HelpText = "Update wrap token request")]
            public class Update : KeyStoreAndConnectionOptions
            {
                [Value(0, MetaName = "id", Required = true)]
                public string? Id { get; set; }

                [Value(0, MetaName = "signature", Required = true, HelpText = "The base64 encoded ECDSA signature used to redeem funds on the destination network")]
                public string? Signature { get; set; }
            }

            [Verb("bridge.wrap.token", HelpText = "Wrap assets.")]
            public class Token : KeyStoreAndConnectionOptions
            {
                [Value(0, MetaName = "networkClass", Required = true, HelpText = "The class of the destination network")]
                public int? NetworkClass { get; set; }

                [Value(1, MetaName = "chainId", Required = true, HelpText = "The chain identifier of the destination network")]
                public int? ChainId { get; set; }

                [Value(2, MetaName = "address", Required = true, HelpText = "The address that can redeem the funds on the destination network")]
                public string? Address { get; set; }

                [Value(3, MetaName = "tokenStandard", Required = true, HelpText = "The ZTS used in the send block")]
                public string? TokenStandard { get; set; }

                [Value(4, MetaName = "amount", Required = true, HelpText = "The amount used in the send block")]
                public long? Amount { get; set; }
            }
        }

        public class Unwrap
        {
            [Verb("bridge.unwrap.list", HelpText = "List all unwrap token requests")]
            public class List : ConnectionOptions
            { }

            [Verb("bridge.unwrap.listByAddress", HelpText = "List all unwrap token requests by address")]
            public class ListByAddress : ConnectionOptions
            {
                [Value(0, MetaName = "address", Required = true, HelpText = "The address")]
                public string? Address { get; set; }
            }

            [Verb("bridge.unwrap.get", HelpText = "Get unwrap token request by hash and log index")]
            public class Get : ConnectionOptions
            {
                [Value(0, MetaName = "hash", Required = true, HelpText = "The transaction hash")]
                public string? Hash { get; set; }

                [Value(1, MetaName = "logIndex", Required = true, HelpText = "The log index")]
                public int? LogIndex { get; set; }
            }

            [Verb("bridge.unwrap.token", HelpText = "Unwrap assets.")]
            public class Token : KeyStoreAndConnectionOptions
            {
                [Value(0, MetaName = "networkClass", Required = true, HelpText = "The class of the source network")]
                public int? NetworkClass { get; set; }

                [Value(1, MetaName = "chainId", Required = true, HelpText = "The chain identifier of the source network")]
                public int? ChainId { get; set; }

                [Value(2, MetaName = "transactionHash", Required = true, HelpText = "The hash of the transaction on the source network")]
                public string? TransactionHash { get; set; }

                [Value(3, MetaName = "logIndex", Required = true, HelpText = "The log index in the block of the transaction that locked/burned the funds on the source network; together with txHash it creates a unique identifier for a transaction")]
                public string? LogIndex { get; set; }

                [Value(4, MetaName = "address", Required = true, HelpText = "The destination NoM address")]
                public string? Address { get; set; }

                [Value(5, MetaName = "tokenStandard", Required = true, HelpText = "The address of the locked/burned token on the source network")]
                public string? TokenStandard { get; set; }

                [Value(6, MetaName = "amount", Required = true, HelpText = "The amount of token that was locked/burned")]
                public long? Amount { get; set; }

                [Value(7, MetaName = "signature", Required = true, HelpText = "The signature as base64 encoded string of the unwrap request")]
                public string? Signature { get; set; }
            }

            [Verb("bridge.unwrap.revoke", HelpText = "Revoke unwrap request.")]
            public class Revoke : KeyStoreAndConnectionOptions
            {
                [Value(0, MetaName = "transactionHash", Required = true, HelpText = "The hash of the transaction")]
                public string? TransactionHash { get; set; }

                [Value(1, MetaName = "logIndex", Required = true, HelpText = "The log index in the block of the transaction that locked/burned the funds")]
                public string? LogIndex { get; set; }
            }
        }

        public class Orchestrator
        {
            [Verb("bridge.orchestrator.info", HelpText = "Get the orchestrator information")]
            public class Info : ConnectionOptions
            { }

            [Verb("bridge.orchestrator.set", HelpText = "Set the orchestrator information")]
            public class Set : KeyStoreAndConnectionOptions
            {
                [Value(0, MetaName = "windowSize", Required = true, HelpText = "Size in momentums of a window used in the orchestrator to determine which signing ceremony should occur, wrap or unwrap request and to determine the key sign ceremony timeout")]
                public long? WindowSize { get; set; }

                [Value(1, MetaName = "keyGenThreshold", Required = true, HelpText = "Minimum number of participants of a key generation ceremony")]
                public int? KeyGenThreshold { get; set; }

                [Value(2, MetaName = "confirmationsToFinality", Required = true, HelpText = "Minimum number of momentums to consider a wrap request confirmed")]
                public int? ConfirmationsToFinality { get; set; }

                [Value(3, MetaName = "estimatedMomentumTime", Required = true, HelpText = "Time in seconds between momentums")]
                public int? EstimatedMomentumTime { get; set; }
            }
        }

        public class Network
        {
            [Verb("bridge.network.list", HelpText = "List all available bridge netwoks")]
            public class List : ConnectionOptions
            { }

            [Verb("bridge.network.get", HelpText = "Get the current bridge network information")]
            public class Get : ConnectionOptions
            {
                [Value(0, MetaName = "networkClass", Required = true)]
                public int? NetworkClass { get; set; }

                [Value(1, MetaName = "chainId", Required = true)]
                public int? ChainId { get; set; }
            }

            [Verb("bridge.network.set", HelpText = "Add or edit an existing bridge network")]
            public class Set : KeyStoreAndConnectionOptions
            {
                [Value(0, MetaName = "networkClass", Required = true)]
                public int? NetworkClass { get; set; }

                [Value(1, MetaName = "chainId", Required = true)]
                public int? ChainId { get; set; }

                [Value(2, MetaName = "name", Required = true)]
                public string? Name { get; set; }

                [Value(3, MetaName = "contractAddress", Required = true)]
                public string? ContractAddress { get; set; }

                [Value(4, MetaName = "metadata")]
                public string? Metadata { get; set; }
            }

            [Verb("bridge.network.remove", HelpText = "Remove an existing bridge network")]
            public class Remove : KeyStoreAndConnectionOptions
            {
                [Value(0, MetaName = "networkClass", Required = true)]
                public int? NetworkClass { get; set; }

                [Value(1, MetaName = "chainId", Required = true)]
                public int? ChainId { get; set; }
            }

            [Verb("bridge.network.setMetadata", HelpText = "Set the metadata for a bridge network")]
            public class SetMetadata : KeyStoreAndConnectionOptions
            {
                [Value(0, MetaName = "networkClass", Required = true)]
                public int? NetworkClass { get; set; }

                [Value(1, MetaName = "chainId", Required = true)]
                public int? ChainId { get; set; }

                [Value(2, MetaName = "metadata")]
                public string? Metadata { get; set; }
            }
        }
    }
}
