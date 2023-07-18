using CommandLine;
using System.Numerics;
using Zenon;
using Zenon.Abi;
using Zenon.Embedded;
using Zenon.Model.Embedded;
using Zenon.Model.NoM;
using Zenon.Model.Primitives;
using Zenon.Utils;

namespace ZenonCli.Commands
{
    public class Htlc
    {
        [Verb("htlc.get", HelpText = "Display htlc details.")]
        public class Get : ConnectionCommand
        {
            [Value(0, Required = true, MetaName = "id")]
            public string? Id { get; set; }

            protected override async Task ProcessAsync()
            {
                var currentTime = DateTimeOffset.Now.ToUnixTimeSeconds();

                var id = ParseHash(this.Id, "id");

                HtlcInfo htlc;
                try
                {
                    htlc = await ZnnClient.Embedded.Htlc.GetById(id);
                }
                catch
                {
                    WriteError($"The htlc id {id} does not exist");
                    return;
                }

                var token = await GetTokenAsync(htlc.TokenStandard);

                WriteInfo($"Htlc id {htlc.Id} with amount {FormatAmount(htlc.Amount, token.Decimals)} {token.Symbol}");
                if (htlc.ExpirationTime > currentTime)
                {
                    WriteInfo($"   Can be unlocked by {htlc.HashLocked} with hashlock {BytesUtils.ToHexString(htlc.HashLock)} hashtype {htlc.HashType}");
                    WriteInfo($"   Can be reclaimed in {FormatDuration(htlc.ExpirationTime - currentTime)} by {htlc.TimeLocked}");
                }
                else
                {
                    WriteInfo($"   Can be reclaimed now by {htlc.TimeLocked}");
                }

                WriteInfo("Done");
            }
        }

        [Verb("htlc.create", HelpText = "Create an htlc.")]
        public class Create : KeyStoreAndConnectionCommand
        {
            [Value(0, Required = true, MetaName = "hashLockedAddress")]
            public string? HashLockedAddress { get; set; }

            [Value(1, Required = true, MetaName = "tokenStandard", MetaValue = "[ZNN/QSR/ZTS]")]
            public string? TokenStandard { get; set; }

            [Value(2, Required = true, MetaName = "amount")]
            public string? Amount { get; set; }

            [Value(3, Required = true, MetaName = "expirationTime", HelpText = "Total hours from now.")]
            public long ExpirationTime { get; set; }

            [Value(4, MetaName = "hashLock", HelpText = "The hash lock as a hexidecimal string.")]
            public string? HashLock { get; set; }

            [Value(5, MetaName = "hashType", Default = 0, HelpText = "0 = SHA3-256, 1 = SHA2-256")]
            public int? HashType { get; set; }

            protected override async Task ProcessAsync()
            {
                var address = ZnnClient.DefaultKeyPair.Address;
                var hashLocked = ParseAddress(this.HashLockedAddress, "hashLockedAddress");
                var tokenStandard = ParseTokenStandard(this.TokenStandard);
                var keyMaxSize = Constants.HtlcPreimageMaxLength;
                Hash hashLock;
                byte[] preimage = Helper.GeneratePreimage();

                var htlcTimelockMinHours = 1;
                var htlcTimelockMaxHours = htlcTimelockMinHours * 24;

                await AssertUserAddressAsync(address, "hashLockedAddress");

                var token = await GetTokenAsync(tokenStandard);
                var amount = ParseAmount(Amount, token.Decimals);

                if (amount <= 0)
                {
                    WriteError("amount must be greater than 0");
                    return;
                }

                await AssertBalanceAsync(address, tokenStandard, amount);

                if (!this.HashType.HasValue)
                {
                    this.HashType = Constants.HtlcHashTypeSha3;
                }

                if (this.HashType != Constants.HtlcHashTypeSha3 &&
                    this.HashType != Constants.HtlcHashTypeSha256)
                {
                    WriteError($"Invalid hash type. Hash type {this.HashType} is not supported.");
                    return;
                }

                if (this.HashLock != null)
                {
                    hashLock = ParseHash(this.HashLock);
                }
                else
                {
                    switch (this.HashType)
                    {
                        case Constants.HtlcHashTypeSha256:
                            hashLock = Hash.FromBytes(Helper.ComputeSha256Hash(preimage));
                            break;

                        default:
                            hashLock = Hash.Digest(preimage);
                            break;
                    }
                }

                if (this.ExpirationTime < htlcTimelockMinHours ||
                    this.ExpirationTime > htlcTimelockMaxHours)
                {
                    WriteError($"The expirationTime (hours) must be at least {htlcTimelockMinHours} and at most {htlcTimelockMaxHours}.");
                    return;
                }

                long expirationTime = this.ExpirationTime * 60 * 60; // convert to seconds
                Momentum currentFrontierMomentum = await ZnnClient.Ledger.GetFrontierMomentum();
                long currentTime = currentFrontierMomentum.Timestamp;
                expirationTime += currentTime;

                if (this.HashLock != null)
                {
                    WriteInfo($"Creating htlc with amount {FormatAmount(amount, token!.Decimals)} {token.Symbol}");
                }
                else
                {
                    WriteInfo($"Creating htlc with amount {FormatAmount(amount, token!.Decimals)} {token.Symbol} using preimage {BytesUtils.ToHexString(preimage)}");
                }

                WriteInfo($"   Can be reclaimed in {FormatDuration(expirationTime - currentTime)} by {address}");
                WriteInfo($"   Can be unlocked by {hashLocked} with hashlock {hashLock} hashtype {this.HashType}");

                var block = ZnnClient.Embedded.Htlc
                    .Create(tokenStandard, amount, hashLocked, expirationTime, this.HashType.Value, keyMaxSize, hashLock.Bytes);

                await ZnnClient.Send(block);

                WriteInfo($"Submitted htlc with id {block.Hash}");
                WriteInfo("Done");
            }
        }

        [Verb("htlc.reclaim", HelpText = "Reclaim an expired htlc.")]
        public class Reclaim : KeyStoreAndConnectionCommand
        {
            [Value(0, Required = true, MetaName = "id", HelpText = "The id of the htlc to reclaim.")]
            public string? Id { get; set; }

            protected override async Task ProcessAsync()
            {
                var address = ZnnClient.DefaultKeyPair.Address;
                var currentTime = DateTimeOffset.Now.ToUnixTimeSeconds();

                var id = ParseHash(this.Id, "id");

                HtlcInfo htlc;
                try
                {
                    htlc = await ZnnClient.Embedded.Htlc.GetById(id);
                }
                catch
                {
                    WriteError($"The htlc id {id} does not exist");
                    return;
                }

                if (htlc.ExpirationTime > currentTime)
                {
                    WriteError($"Cannot reclaim htlc. Try again in {FormatDuration(htlc.ExpirationTime - currentTime)}.");
                    return;
                }

                if (htlc.TimeLocked != address)
                {
                    WriteError("Cannot reclaim htlc. Permission denied");
                    return;
                }

                var token = await GetTokenAsync(htlc.TokenStandard);

                WriteInfo($"Reclaiming htlc id {htlc.Id} with amount {FormatAmount(htlc.Amount, token.Decimals)} {token.Symbol}");

                await ZnnClient.Send(ZnnClient.Embedded.Htlc.Reclaim(id));

                WriteInfo("Done");
                WriteInfo($"Use receiveAll to collect your htlc amount after 2 momentums");
            }
        }

        [Verb("htlc.unlock", HelpText = "Unlock an active htlc.")]
        public class Unlock : KeyStoreAndConnectionCommand
        {
            [Value(0, Required = true, MetaName = "id", HelpText = "The id of the htlc to unlock.")]
            public string? Id { get; set; }

            [Value(1, MetaName = "preimage", HelpText = "The preimage as a hexidecimal string.")]
            public string? Preimage { get; set; }

            protected override async Task ProcessAsync()
            {
                var address = ZnnClient.DefaultKeyPair.Address;
                var currentTime = DateTimeOffset.Now.ToUnixTimeSeconds();
                Hash preimageCheck;

                var id = ParseHash(this.Id, "id");

                HtlcInfo htlc;
                try
                {
                    htlc = await ZnnClient.Embedded.Htlc.GetById(id);
                }
                catch
                {
                    WriteError($"The htlc id {id} does not exist");
                    return;
                }

                if (!await ZnnClient.Embedded.Htlc.GetProxyUnlockStatus(htlc.HashLocked))
                {
                    WriteError($"Cannot unlock htlc. Permission denied");
                    return;
                }
                else if (htlc.ExpirationTime <= currentTime)
                {
                    WriteError($"Cannot unlock htlc. Time lock expired.");
                    return;
                }

                if (this.Preimage == null)
                {
                    WriteInfo("Insert preimage:");
                    this.Preimage = ReadPassword();
                }

                if (string.IsNullOrEmpty(this.Preimage))
                {
                    WriteError("Cannot unlock htlc. Invalid preimage");
                    return;
                }

                switch (htlc.HashType)
                {
                    case Constants.HtlcHashTypeSha256:
                        WriteInfo("HashType 1 detected. Encoding preimage to SHA2-256 ...");
                        preimageCheck = Hash.FromBytes(Helper.ComputeSha256Hash(BytesUtils.FromHexString(this.Preimage)));
                        break;

                    default:
                        preimageCheck = Hash.Digest(BytesUtils.FromHexString(this.Preimage));
                        break;
                }

                if (preimageCheck != Hash.FromBytes(htlc.HashLock))
                {
                    WriteError("preimage does not match the hashlock");
                    return;
                }

                var token = await GetTokenAsync(htlc.TokenStandard);

                WriteInfo($"Unlocking htlc id {htlc.Id} with amount {FormatAmount(htlc.Amount, token.Decimals)} {token.Symbol}");

                await ZnnClient.Send(ZnnClient.Embedded.Htlc.Unlock(id, BytesUtils.FromHexString(this.Preimage)));

                WriteInfo("Done");
                WriteInfo($"Use receiveAll to collect your htlc amount after 2 momentums");
            }
        }

        [Verb("htlc.inspect", HelpText = "Inspect htlc account-block.")]
        public class Inspect : ConnectionCommand
        {
            [Value(0, Required = true, MetaName = "blockHash", HelpText = "The hash of a htlc account-block.")]
            public string? BlockHash { get; set; }

            protected override async Task ProcessAsync()
            {
                var hash = ParseHash(this.BlockHash, "blockHash");
                var block = await ZnnClient.Ledger.GetAccountBlockByHash(hash);

                if (block == null)
                {
                    WriteError($"The account block {hash} does not exist");
                    return;
                }

                if (block.PairedAccountBlock == null ||
                    block.BlockType != BlockTypeEnum.UserSend)
                {
                    WriteError($"The account block was not send by a user.");
                    return;
                }

                var f = Definitions.Htlc.Entries
                    .Where(x => AbiFunction.ExtractSignature(x.EncodeSignature()).SequenceEqual(AbiFunction.ExtractSignature(block.Data)))
                    .Select(x => new AbiFunction(x.Name, x.Inputs))
                    .FirstOrDefault();

                if (f == null)
                {
                    WriteError($"The account block contains invalid data.");
                    return;
                }

                if (String.Equals(f.Name, "Unlock", StringComparison.OrdinalIgnoreCase))
                {
                    var args = f.Decode(block.Data);

                    if (args.Length != 2)
                    {
                        WriteError($"The account block has an invalid unlock argument length");
                        return;
                    }

                    WriteInfo($"Unlock htlc id: {args[0]} unlocked by {block.Address} with preimage: {BytesUtils.ToHexString((byte[])args[1])}");
                }
                else if (String.Equals(f.Name, "Reclaim", StringComparison.OrdinalIgnoreCase))
                {
                    var args = f.Decode(block.Data);

                    if (args.Length != 1)
                    {
                        WriteError($"The account block has an invalid reclaim argument length");
                        return;
                    }

                    WriteInfo($"Reclaim htlc id: {args[0]} reclaimed by ${block.Address}");
                }
                else if (String.Equals(f.Name, "Create", StringComparison.OrdinalIgnoreCase))
                {
                    var args = f.Decode(block.Data);

                    if (args.Length != 5)
                    {
                        WriteError($"The account block has an invalid create argument length");
                        return;
                    }

                    var expirationTime = ((BigInteger)args[1]);
                    var hashLock = (byte[])args[4];
                    var amount = block.Amount;
                    var token = block.Token;

                    WriteInfo($"Create htlc: {args[0]} {FormatAmount(amount, token.Decimals)} {token.Symbol} {expirationTime} {args[2]} {args[3]} {BytesUtils.ToHexString(hashLock)} created by {block.Address}");
                }
                else
                {
                    WriteError($"The account block contains an unknown function call");
                    return;
                }
            }
        }

        [Verb("htlc.monitor", HelpText = "Monitor htlc by id -- automatically reclaim it or display its preimage.")]
        public class Monitor : KeyStoreAndConnectionCommand
        {
            [Value(0, Required = true, MetaName = "id", HelpText = "The id of the htlc to monitor.")]
            public string? Id { get; set; }

            protected override async Task ProcessAsync()
            {
                var address = ZnnClient.DefaultKeyPair.Address;

                var id = ParseHash(this.Id, "id");

                var htlc = await ZnnClient.Embedded.Htlc.GetById(id);

                if (htlc == null)
                {
                    WriteError($"The htlc id {id} does not exist");
                    return;
                }

                while (await MonitorAsync(ZnnClient, address, new HtlcInfo[] { htlc }) != true)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
            }

            private async Task<bool> MonitorAsync(Znn znnClient, Address address, HtlcInfo[] htlcs)
            {
                foreach (var htlc in htlcs)
                {
                    WriteInfo($"Monitoring htlc id {htlc.Id}");
                }

                var htlcList = htlcs.ToList();
                var waitingToBeReclaimed = new List<HtlcInfo>();
                var queue = new List<Hash>();

                WriteInfo("Subscribing for htlc-contract events ...");
                await znnClient.Subscribe.ToAllAccountBlocks((json) =>
                {
                    // Extract hashes for all new tx that interact with the htlc contract
                    for (var i = 0; i < json.Length; i += 1)
                    {
                        var tx = json[i];
                        if (tx.Value<string>("toAddress") != Address.HtlcAddress.ToString())
                            continue;

                        var hash = Hash.Parse(tx.Value<string>("hash"));
                        WriteInfo($"Receiving transaction with hash {hash}");
                        queue.Add(hash);
                    }
                });

                while (true)
                {
                    if (htlcList.Count == 0 && waitingToBeReclaimed.Count == 0)
                    {
                        break;
                    }

                    var currentTime = DateTimeOffset.Now.ToUnixTimeSeconds();

                    foreach (var htlc in htlcList.ToArray())
                    {
                        // Reclaim any expired timeLocked htlc that is being monitored
                        if (htlc.ExpirationTime <= currentTime)
                        {
                            if (htlc.TimeLocked == address)
                            {
                                try
                                {
                                    await znnClient.Send(znnClient.Embedded.Htlc.Reclaim(htlc.Id));
                                    WriteInfo($"  Reclaiming htlc id {htlc.Id} now ...");
                                    htlcList.Remove(htlc);
                                }
                                catch
                                {
                                    WriteError($"  Error occurred when reclaiming {htlc.Id}");
                                }
                            }
                            else
                            {
                                WriteInfo($"  Waiting for {htlc.TimeLocked} to reclaim ...");
                                waitingToBeReclaimed.Add(htlc);
                                htlcList.Remove(htlc);
                            }
                        }
                    }

                    foreach (var hash in queue.ToArray())
                    {
                        // Identify if htlc tx are either UnlockHtlc or ReclaimHtlc
                        var block = await znnClient.Ledger.GetAccountBlockByHash(hash);

                        if (block.BlockType != BlockTypeEnum.UserSend)
                            continue;

                        if (block.PairedAccountBlock == null ||
                            block.PairedAccountBlock.BlockType != BlockTypeEnum.ContractReceive)
                            continue;

                        if (block.PairedAccountBlock.DescendantBlocks == null ||
                            block.PairedAccountBlock.DescendantBlocks.Length == 0)
                            continue;

                        var f = Definitions.Htlc.Entries
                            .Where(x => AbiFunction.ExtractSignature(x.EncodeSignature()).SequenceEqual(AbiFunction.ExtractSignature(block.Data)))
                            .Select(x => new AbiFunction(x.Name, x.Inputs))
                            .FirstOrDefault();

                        if (f == null)
                            continue;

                        // If UnlockHtlc, display preimage that are hashLocked to current address
                        foreach (var htlc in htlcList.ToArray())
                        {
                            if (String.Equals(f.Name, "Unlock", StringComparison.OrdinalIgnoreCase))
                            {
                                var args = f.Decode(block.Data);

                                if (args.Length != 2)
                                    continue;

                                if (args[0].ToString() != htlc.Id.ToString())
                                    continue;

                                if (block.PairedAccountBlock.DescendantBlocks.Any(x =>
                                    x.BlockType == BlockTypeEnum.ContractSend &&
                                    x.ToAddress == htlc.HashLocked &&
                                    x.TokenStandard == htlc.TokenStandard &&
                                    x.Amount == htlc.Amount))
                                {
                                    var preimage = (byte[])args[1];

                                    WriteInfo($"Htlc id {htlc.Id} unlocked with preimage: {BytesUtils.ToHexString(preimage)}");

                                    htlcList.Remove(htlc);
                                }
                            }
                        }

                        // If ReclaimHtlc, inform user that a monitored, expired htlc
                        // and has been reclaimed by the timeLocked address
                        foreach (var htlc in waitingToBeReclaimed.ToArray())
                        {
                            if (String.Equals(f.Name, "Reclaim", StringComparison.OrdinalIgnoreCase))
                            {
                                var args = f.Decode(block.Data);

                                if (args.Length != 1)
                                    continue;

                                if (args[0].ToString() != htlc.Id.ToString())
                                    continue;

                                if (block.PairedAccountBlock.DescendantBlocks.Any(x =>
                                    x.BlockType == BlockTypeEnum.ContractSend &&
                                    x.ToAddress == htlc.TimeLocked &&
                                    x.TokenStandard == htlc.TokenStandard &&
                                    x.Amount == htlc.Amount))
                                {
                                    WriteInfo($"Htlc id {htlc.Id} reclaimed");
                                    waitingToBeReclaimed.Remove(htlc);
                                }
                                else
                                {
                                    WriteInfo(block.ToString());
                                }
                            }
                        }

                        queue.Remove(hash);
                    }

                    await Task.Delay(TimeSpan.FromSeconds(1));
                }

                WriteInfo("No longer monitoring any htlc's");

                return true;
            }
        }

        [Verb("htlc.getProxyStatus", HelpText = "Display proxy unlock status for an address.")]
        public class GetProxyUnlockStatus : ConnectionCommand
        {
            [Value(0, Required = true, MetaName = "address")]
            public string? Address { get; set; }

            protected override async Task ProcessAsync()
            {
                var address = ParseAddress(this.Address);

                var status = await ZnnClient.Embedded.Htlc.GetProxyUnlockStatus(address);

                WriteInfo($"Htlc proxy unlocking is {(status ? "allowed" : "denied")} for {address}");

                WriteInfo("Done");
            }
        }

        [Verb("htlc.allowProxy", HelpText = "Allow htlc proxy unlock.")]
        public class AllowProxyUnlock : KeyStoreAndConnectionCommand
        {
            protected override async Task ProcessAsync()
            {
                var address = ZnnClient.DefaultKeyPair.Address;

                await ZnnClient.Send(ZnnClient.Embedded.Htlc.AllowProxyUnlock());

                WriteInfo($"Htlc proxy unlocking is allowed for ${address}");
                WriteInfo("Done");
            }
        }

        [Verb("htlc.denyProxy", HelpText = "Deny htlc proxy unlock.")]
        public class DenyProxyUnlock : KeyStoreAndConnectionCommand
        {
            protected override async Task ProcessAsync()
            {
                var address = ZnnClient.DefaultKeyPair.Address;

                await ZnnClient.Send(ZnnClient.Embedded.Htlc.DenyProxyUnlock());

                WriteInfo($"Htlc proxy unlocking is denied for ${address}");
                WriteInfo("Done");
            }
        }
    }
}
