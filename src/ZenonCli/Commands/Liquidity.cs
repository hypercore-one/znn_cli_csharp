using CommandLine;
using System.Numerics;
using Zenon;

namespace ZenonCli.Commands
{
    public partial class Liquidity
    {
        [Verb("liquidity.info", HelpText = "Get the liquidity information.")]
        public class Info : ConnectionCommand
        {
            protected override async Task ProcessAsync()
            {
                var info = await ZnnClient.Embedded.Liquidity
                    .GetLiquidityInfo();

                WriteInfo($"Liquidity info:");
                WriteInfo($"   Admnistrator: {info.Administrator}");
                WriteInfo($"   ZNN reward: {info.ZnnReward}");
                WriteInfo($"   QSR reward: {info.QsrReward}");
                WriteInfo($"   Is halted: {info.IsHalted}");
                WriteInfo($"   Tokens:");

                foreach (var tokenTuple in info.TokenTuples)
                {
                    var token = await GetTokenAsync(tokenTuple.TokenStandard);
                    var type = GetTokenType(tokenTuple.TokenStandard);

                    WriteInfo($"      {type} {token.Name} with symbol {token.Symbol} and standard {token.TokenStandard}");
                    WriteInfo($"        ZNN % {tokenTuple.ZnnPercentage} QSR % {tokenTuple.QsrPercentage} minimum amount {FormatAmount(tokenTuple.MinAmount, token.Decimals)}");
                    WriteInfo("");
                }
            }
        }

        [Verb("liquidity.security", HelpText = "Get the liquidity security info.")]
        public class SecurityInfo : ConnectionCommand
        {
            protected override async Task ProcessAsync()
            {
                var info = await ZnnClient.Embedded.Liquidity.GetSecurityInfo();

                WriteInfo($"Security info:");

                if (info.Guardians == null || info.Guardians.Length == 0)
                {
                    WriteInfo($"   Guardians: none");
                }
                else
                {
                    WriteInfo($"   Guardians: ");

                    foreach (var guardian in info.Guardians)
                    {
                        WriteInfo($"      {guardian}");
                    }
                }

                if (info.GuardiansVotes == null || info.GuardiansVotes.Length == 0)
                {
                    WriteInfo($"   Guardian votes: none");
                }
                else
                {
                    WriteInfo($"   Guardian votes: ");

                    foreach (var guardianVote in info.GuardiansVotes)
                    {
                        WriteInfo($"      {guardianVote}");
                    }
                }

                WriteInfo($"   Administrator delay: {info.AdministratorDelay}");
                WriteInfo($"   Soft delay: {info.SoftDelay}");
            }
        }

        [Verb("liquidity.timeChallenges", HelpText = "List the liquidity time challenges.")]
        public class TimeChallengesInfo : ConnectionCommand
        {
            protected override async Task ProcessAsync()
            {
                var list = await ZnnClient.Embedded.Liquidity.GetTimeChallengesInfo();

                if (list == null || list.Count == 0)
                {
                    WriteInfo("No time challenges found.");
                    return;
                }

                WriteInfo($"Time challenges:");

                foreach (var info in list.List)
                {
                    WriteInfo($"   Method: {info.MethodName}");
                    WriteInfo($"   Start height: {info.ChallengeStartHeight}");
                    WriteInfo($"   Params hash: {info.ParamsHash}");
                    WriteInfo("");
                }
            }
        }

        [Verb("liquidity.getRewardTotal", HelpText = "Display total rewards an address has earned.")]
        public class GetRewardTotal : ConnectionCommand
        {
            [Value(0, MetaName = "address", Required = true)]
            public string? Address { get; set; }

            protected override async Task ProcessAsync()
            {
                var address = ParseAddress(Address);
                var list =
                    await ZnnClient.Embedded.Liquidity.GetFrontierRewardByPage(address);

                if (list.Count > 0)
                {
                    var znnRewards = BigInteger.Zero;
                    var qsrRewards = BigInteger.Zero;

                    foreach (var entry in list.List)
                    {
                        if (entry.ZnnAmount != 0 || entry.QsrAmount != 0)
                        {
                            znnRewards += entry.ZnnAmount;
                            qsrRewards += entry.QsrAmount;
                        }
                    }

                    if (znnRewards == 0 && qsrRewards == 0)
                    {
                        WriteInfo("No rewards found.");
                    }
                    else
                    {
                        WriteInfo("Total rewards:");
                        WriteInfo($"   ZNN: {FormatAmount(znnRewards, Constants.CoinDecimals)}");
                        WriteInfo($"   QSR: {FormatAmount(qsrRewards, Constants.CoinDecimals)}");
                    }
                }
                else
                {
                    WriteInfo("No rewards found.");
                }
            }
        }

        [Verb("liquidity.getStakeEntries", HelpText = "Display all stake entries for an address.")]
        public class GetStakeEntries : ConnectionCommand
        {
            [Value(0, MetaName = "address", Required = true)]
            public string? Address { get; set; }

            protected override async Task ProcessAsync()
            {
                var address = ParseAddress(Address);
                var list =
                    await ZnnClient.Embedded.Liquidity.GetLiquidityStakeEntriesByAddress(address);

                if (list.Count > 0)
                {
                    WriteInfo("Stake entries:");
                    WriteInfo($"   Total amount: {list.TotalAmount}");
                    WriteInfo($"   Total weighted amount: {list.TotalWeightedAmount}");

                    foreach (var entry in list.List)
                    {
                        var token =
                            await GetTokenAsync(entry.TokenStandard);

                        var currentTime = Math.Floor((double)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000));
                        var duration = (entry.ExpirationTime - entry.StartTime) / Constants.StakeTimeUnitSec;
                        var timeRemaining = (long)(entry.ExpirationTime - currentTime);

                        WriteInfo($"      Id: {entry.Id}");
                        WriteInfo($"      Status: {(entry.Amount != 0 && entry.RevokeTime == 0 ? "Active" : "Cancelled")}");
                        WriteInfo($"      Token: {token.Name}");
                        WriteInfo($"      Amount: {FormatAmount(entry.Amount, token.Decimals)} ${token.Symbol}");
                        WriteInfo($"      Weighted amount: {FormatAmount(entry.WeightedAmount, token.Decimals)} {token.Symbol}");
                        WriteInfo($"      Duration: {duration} {Constants.StakeUnitDurationName}{(duration > 1 ? "s" : "")}");
                        WriteInfo($"      Time remaining: {FormatDuration(timeRemaining)} day{(timeRemaining > (24 * 60 * 60) ? "s" : "")}");
                        WriteInfo($"      Revoke time: {entry.RevokeTime}");
                        WriteInfo("");
                    }
                }
                else
                {
                    WriteInfo("No stake entries found.");
                }
            }
        }

        [Verb("liquidity.getUncollectedReward", HelpText = "Display uncollected rewards for an address.")]
        public class GetUncollectedReward : ConnectionCommand
        {
            [Value(0, MetaName = "address", Required = true)]
            public string? Address { get; set; }

            protected override async Task ProcessAsync()
            {
                var address = ParseAddress(this.Address);
                var uncollectedRewards =
                    await ZnnClient.Embedded.Liquidity.GetUncollectedReward(address);

                if (uncollectedRewards.ZnnAmount != 0 ||
                    uncollectedRewards.QsrAmount != 0)
                {
                    WriteInfo("Uncollected rewards:");
                    WriteInfo($"   ZNN: {FormatAmount(uncollectedRewards.ZnnAmount, Constants.CoinDecimals)}");
                    WriteInfo($"   QSR: {FormatAmount(uncollectedRewards.QsrAmount, Constants.CoinDecimals)}");
                }
                else
                {
                    WriteInfo("No uncollected rewards");
                }
            }
        }

        [Verb("liquidity.stake", HelpText = "Stake LP tokens.")]
        public class Stake : KeyStoreAndConnectionCommand
        {
            [Value(0, Required = true, MetaName = "duration", HelpText = "Duration in months")]
            public long Duration { get; set; }

            [Value(1, Required = true, MetaName = "amount")]
            public string? Amount { get; set; }

            [Value(2, Default = "ZNN", MetaName = "tokenStandard", MetaValue = "[ZNN/QSR/ZTS]")]
            public string? TokenStandard { get; set; }

            protected override async Task ProcessAsync()
            {
                var address = await ZnnClient.DefaultKeyPair.GetAddressAsync();
                var months = this.Duration;
                var duration = months * Constants.StakeTimeUnitSec;
                var tokenStandard = ParseTokenStandard(TokenStandard);
                var token = await GetTokenAsync(tokenStandard);
                var amount = ParseAmount(Amount!, token.Decimals);

                if (duration < Constants.StakeTimeMinSec ||
                    duration > Constants.StakeTimeMaxSec ||
                    duration % Constants.StakeTimeUnitSec != 0)
                {
                    WriteError($"Invalid staking duration");
                    return;
                }

                await AssertBalanceAsync(address, tokenStandard, amount);

                var info = await ZnnClient.Embedded.Liquidity.GetLiquidityInfo();
                if (info.IsHalted)
                {
                    WriteError("Liquidity contract is halted");
                    return;
                }

                var liquidityToken = info.TokenTuples.FirstOrDefault(x => x.TokenStandard == tokenStandard);

                if (liquidityToken != null)
                {
                    if (amount < liquidityToken.MinAmount)
                    {
                        WriteError($"Minimum staking requirement: ${FormatAmount(liquidityToken.MinAmount, token.Decimals)} ${token.Symbol}");
                        return;
                    }
                }
                else
                {
                    WriteError($"{token.Name} cannot be staked in the Liquidity contract");
                    return;
                }

                WriteInfo($"Staking {FormatAmount(amount, token.Decimals)} {token.Symbol} for {months} month{(months > 1 ? "s" : "")} ...");
                var block = ZnnClient.Embedded.Liquidity.LiquidityStake(duration, amount, tokenStandard);
                await ZnnClient.Send(block);
                WriteInfo("Done");
            }
        }

        [Verb("liquidity.cancelStake", HelpText = "Cancel an unlocked stake and receive your LP tokens.")]
        public class CancelStake : KeyStoreAndConnectionCommand
        {
            [Value(0, MetaName = "id", Required = true)]
            public string? Id { get; set; }

            protected override async Task ProcessAsync()
            {
                var id = ParseHash(Id, "id");

                var address = await ZnnClient.DefaultKeyPair.GetAddressAsync();
                var list = await ZnnClient.Embedded.Liquidity
                    .GetLiquidityStakeEntriesByAddress(address);

                if (list.Count == 0)
                {
                    WriteInfo("No stake entries found");
                    return;
                }

                var entry = list.List.FirstOrDefault(x => x.Id == id);

                if (entry != null)
                {
                    var currentTime = (long)Math.Floor((double)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000));

                    if (currentTime > entry.ExpirationTime)
                    {
                        WriteInfo("Cancelling liquidity stake ...");
                        var block =
                            ZnnClient.Embedded.Liquidity.CancelLiquidityStake(id);
                        await ZnnClient.Send(block);
                        WriteInfo("Done");
                        WriteInfo("Use receiveAll to collect your staked amount after 2 momentums");
                    }
                    else
                    {
                        WriteInfo("That staking entry is not unlocked yet");
                        WriteInfo($"Time Remaining: {FormatDuration(entry.ExpirationTime - currentTime)}");
                    }
                }
                else
                {
                    WriteInfo("Staking entry not found");
                }
            }
        }

        [Verb("liquidity.collectRewards", HelpText = "Collect liquidity rewards.")]
        public class CollectRewards : KeyStoreAndConnectionCommand
        {
            protected override async Task ProcessAsync()
            {
                var address = await ZnnClient.DefaultKeyPair.GetAddressAsync();
                var uncollectedRewards =
                    await ZnnClient.Embedded.Liquidity.GetUncollectedReward(address);

                if (uncollectedRewards.ZnnAmount != 0 ||
                    uncollectedRewards.QsrAmount != 0)
                {
                    WriteInfo("Uncollected Rewards:");
                    WriteInfo($"   ZNN: {FormatAmount(uncollectedRewards.ZnnAmount, Constants.CoinDecimals)}");
                    WriteInfo($"   QSR: {FormatAmount(uncollectedRewards.QsrAmount, Constants.CoinDecimals)}");
                    WriteInfo("");
                    WriteInfo("Collecting rewards ...");
                    var block =
                        ZnnClient.Embedded.Liquidity.CollectReward();
                    await ZnnClient.Send(block);
                    WriteInfo("Done");
                }
                else
                {
                    WriteInfo("No uncollected rewards");
                }
            }
        }
    }
}