using CommandLine;
using Zenon;

namespace ZenonCli.Commands
{
    public class Sentinel
    {
        [Verb("sentinel.list", HelpText = "List all sentinels.")]
        public class List : WalletAndConnectionCommand
        {
            protected override async Task ProcessAsync()
            {
                var address = await Zdk!.DefaultWalletAccount.GetAddressAsync();
                var sentinels = await Zdk!.Embedded.Sentinel.GetAllActive();

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
        }

        [Verb("sentinel.register", HelpText = "Register a sentinel.")]
        public class Register : WalletAndConnectionCommand
        {
            protected override async Task ProcessAsync()
            {
                var address = await Zdk!.DefaultWalletAccount.GetAddressAsync();

                var accountInfo =
                    await Zdk!.Ledger.GetAccountInfoByAddress(address);
                var depositedQsr =
                    await Zdk!.Embedded.Sentinel.GetDepositedQsr(address);

                WriteInfo($"You have {FormatAmount(depositedQsr, Constants.CoinDecimals)} QSR deposited for the Sentinel");

                if (accountInfo.Znn < Constants.SentinelRegisterZnnAmount ||
                    accountInfo.Qsr < Constants.SentinelRegisterQsrAmount)
                {
                    WriteInfo($"Cannot register Sentinel with address {address}");
                    WriteInfo($"Required {FormatAmount(Constants.SentinelRegisterZnnAmount, Constants.CoinDecimals)} ZNN and {FormatAmount(Constants.SentinelRegisterQsrAmount, Constants.CoinDecimals)} QSR");
                    WriteInfo($"Available {FormatAmount(accountInfo.Znn!.Value, Constants.CoinDecimals)} ZNN and {FormatAmount(accountInfo.Qsr!.Value, Constants.CoinDecimals)} QSR");
                    return;
                }

                if (depositedQsr < Constants.SentinelRegisterQsrAmount)
                {
                    await Zdk!.SendAsync(Zdk!.Embedded.Sentinel
                        .DepositQsr(Constants.SentinelRegisterQsrAmount - depositedQsr));
                }
                await Zdk!.SendAsync(Zdk!.Embedded.Sentinel.Register());
                WriteInfo("Done");
                WriteInfo($"Check after 2 momentums if the Sentinel was successfully registered using sentinel.list command");
            }
        }

        [Verb("sentinel.revoke", HelpText = "Revoke a sentinel.")]
        public class Revoke : WalletAndConnectionCommand
        {
            protected override async Task ProcessAsync()
            {
                var address = await Zdk!.DefaultWalletAccount.GetAddressAsync();

                var entry =
                    await Zdk!.Embedded.Sentinel.GetByOwner(address);

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

                await Zdk!.SendAsync(Zdk!.Embedded.Sentinel.Revoke());

                WriteInfo("Done");
                WriteInfo($"Use receiveAll to collect back the locked amount of ZNN and QSR");
            }
        }

        [Verb("sentinel.collect", HelpText = "Collect sentinel rewards.")]
        public class Collect : WalletAndConnectionCommand
        {
            protected override async Task ProcessAsync()
            {
                await Zdk!.SendAsync(Zdk!.Embedded.Sentinel.CollectReward());

                WriteInfo("Done");
                WriteInfo($"Use receiveAll to collect your Sentinel reward(s) after 1 momentum");
            }
        }

        [Verb("sentinel.depositQsr", HelpText = "Deposit QSR to the sentinel contract.")]
        public class DepositQsr : WalletAndConnectionCommand
        {
            protected override async Task ProcessAsync()
            {
                var address = await Zdk!.DefaultWalletAccount.GetAddressAsync();
                var balance = await Zdk!.Ledger.GetAccountInfoByAddress(address);
                var depositedQsr =
                    await Zdk!.Embedded.Sentinel.GetDepositedQsr(address);
                WriteInfo($"You have {FormatAmount(depositedQsr, Constants.CoinDecimals)} / {FormatAmount(Constants.SentinelRegisterQsrAmount, Constants.CoinDecimals)} QSR deposited for the Sentinel");

                if (balance.Qsr!.Value < Constants.SentinelRegisterQsrAmount)
                {
                    WriteInfo($"Required {FormatAmount(Constants.SentinelRegisterQsrAmount, Constants.CoinDecimals)} QSR");
                    WriteInfo($"Available {FormatAmount(balance.Qsr!.Value, Constants.CoinDecimals)} QSR");
                    return;
                }

                if (depositedQsr < Constants.SentinelRegisterQsrAmount)
                {
                    WriteInfo($"Depositing {FormatAmount(Constants.SentinelRegisterQsrAmount - depositedQsr, Constants.CoinDecimals)} QSR for the Sentinel");
                    await Zdk!.SendAsync(Zdk!.Embedded.Sentinel
                        .DepositQsr(Constants.SentinelRegisterQsrAmount - depositedQsr));
                }
                WriteInfo("Done");
            }
        }

        [Verb("sentinel.withdrawQsr", HelpText = "Withdraw deposited QSR from the sentinel contract.")]
        public class WithdrawQsr : WalletAndConnectionCommand
        {
            protected override async Task ProcessAsync()
            {
                var address = await Zdk!.DefaultWalletAccount.GetAddressAsync();

                var depositedQsr =
                    await Zdk!.Embedded.Sentinel.GetDepositedQsr(address);

                if (depositedQsr == 0)
                {
                    WriteInfo($"No deposited QSR to withdraw");
                    return;
                }

                WriteInfo($"Withdrawing {FormatAmount(depositedQsr, Constants.CoinDecimals)} QSR ...");

                await Zdk!.SendAsync(Zdk!.Embedded.Sentinel.WithdrawQsr());

                WriteInfo("Done");
            }
        }
    }
}
