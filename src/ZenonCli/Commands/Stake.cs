using CommandLine;
using Zenon;
using Zenon.Model.NoM;

namespace ZenonCli.Commands
{
    public class Stake
    {
        [Verb("stake.list", HelpText = "List all stakes.")]
        public class List : ConnectionCommand
        {
            [Value(0, Default = 0, MetaName = "pageIndex")]
            public int? PageIndex { get; set; }

            [Value(1, Default = 25, MetaName = "PageSize")]
            public int? PageSize { get; set; }

            protected override async Task ProcessAsync()
            {
                var currentTime = DateTimeOffset.Now.ToUnixTimeSeconds();
                var address = ZnnClient.DefaultKeyPair.Address;

                if (!this.PageIndex.HasValue)
                    this.PageIndex = 0;

                if (!this.PageSize.HasValue)
                    this.PageSize = 25;

                AssertPageRange(PageIndex.Value, PageSize.Value);

                var stakeList = await ZnnClient.Embedded.Stake.GetEntriesByAddress(
                    address, PageIndex.Value, PageSize.Value);

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
                    WriteInfo($"Stake id {entry.Id} with amount {FormatAmount(entry.Amount, Constants.CoinDecimals)} ZNN");

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
        }

        [Verb("stake.register", HelpText = "Register stake.")]
        public class Register : KeyStoreAndConnectionCommand
        {
            [Value(0, Required = true, MetaName = "amount")]
            public string? Amount { get; set; }

            [Value(1, Required = true, MetaName = "duration", HelpText = "Duration in months")]
            public long Duration { get; set; }

            protected override async Task ProcessAsync()
            {
                var address = ZnnClient.DefaultKeyPair.Address;
                var amount = ParseAmount(Amount!, Constants.CoinDecimals);
                var duration = this.Duration;

                if (duration < 1 || duration > 12)
                {
                    WriteError($"Invalid duration ({duration}) {Constants.StakeUnitDurationName}. It must be between 1 and 12");
                    return;
                }
                if (amount < Constants.StakeMinAmount)
                {
                    WriteError($"Invalid amount {FormatAmount(amount, Constants.CoinDecimals)} ZNN. Minimum staking amount is {FormatAmount(Constants.StakeMinAmount, Constants.CoinDecimals)}");
                    return;
                }

                AccountInfo balance =
                    await ZnnClient.Ledger.GetAccountInfoByAddress(address);

                if (balance.Znn! < amount)
                {
                    WriteInfo("Not enough ZNN to stake");
                    return;
                }

                WriteInfo($"Staking {FormatAmount(amount, Constants.CoinDecimals)} ZNN for {duration} {Constants.StakeUnitDurationName}(s)");

                await ZnnClient.Send(
                    ZnnClient.Embedded.Stake.Stake(Constants.StakeTimeUnitSec * duration, amount));

                WriteInfo("Done");
            }
        }

        [Verb("stake.revoke", HelpText = "Revoke stake.")]
        public class Revoke : KeyStoreAndConnectionCommand
        {
            [Value(0, Required = true, MetaName = "id")]
            public string? Id { get; set; }

            protected override async Task ProcessAsync()
            {
                var address = ZnnClient.DefaultKeyPair.Address;

                var hash = ParseHash(Id, "id");

                var currentTime = DateTimeOffset.Now.ToUnixTimeSeconds();
                int pageIndex = 0;
                bool one = false;
                bool gotError = false;

                var entries = await ZnnClient.Embedded.Stake.GetEntriesByAddress(address, pageIndex);

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
                    entries = await ZnnClient.Embedded.Stake.GetEntriesByAddress(address, pageIndex);
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

                await ZnnClient.Send(ZnnClient.Embedded.Stake.Cancel(hash));

                WriteInfo("Done");
                WriteInfo($"Use receiveAll to collect your stake amount and uncollected reward(s) after 2 momentums");
            }
        }

        [Verb("stake.collect", HelpText = "Collect staking rewards.")]
        public class Collect : KeyStoreAndConnectionCommand
        {
            protected override async Task ProcessAsync()
            {
                await ZnnClient.Send(ZnnClient.Embedded.Stake.CollectReward());

                WriteInfo("Done");
                WriteInfo($"Use receiveAll to collect your stake reward(s) after 1 momentum");
            }
        }
    }
}
