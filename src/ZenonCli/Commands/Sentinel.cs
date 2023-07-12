using CommandLine;
using Zenon;

namespace ZenonCli.Commands
{
    public class Sentinel
    {
        [Verb("sentinel.list", HelpText = "List all sentinels.")]
        public class List : ConnectionCommand
        {
            protected override async Task ProcessAsync()
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
        }

        [Verb("sentinel.register", HelpText = "Register a sentinel.")]
        public class Register : KeyStoreAndConnectionCommand
        {
            protected override async Task ProcessAsync()
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
                    WriteInfo($"Required {FormatAmount(Constants.SentinelRegisterZnnAmount, Constants.CoinDecimals)} ZNN and {FormatAmount(Constants.SentinelRegisterQsrAmount, Constants.CoinDecimals)} QSR");
                    WriteInfo($"Available {FormatAmount(accountInfo.Znn!.Value, Constants.CoinDecimals)} ZNN and {FormatAmount(accountInfo.Qsr!.Value, Constants.CoinDecimals)} QSR");
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
        }

        [Verb("sentinel.revoke", HelpText = "Revoke a sentinel.")]
        public class Revoke : KeyStoreAndConnectionCommand
        {
            protected override async Task ProcessAsync()
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
        }

        [Verb("sentinel.collect", HelpText = "Collect sentinel rewards.")]
        public class Collect : KeyStoreAndConnectionCommand
        {
            protected override async Task ProcessAsync()
            {
                await Znn.Instance.Send(Znn.Instance.Embedded.Sentinel.CollectReward());

                WriteInfo("Done");
                WriteInfo($"Use receiveAll to collect your Sentinel reward(s) after 1 momentum");
            }
        }

        [Verb("sentinel.depositQsr", HelpText = "Deposit QSR to the sentinel contract.")]
        public class DepositQsr : KeyStoreAndConnectionCommand
        {
            protected override async Task ProcessAsync()
            {
                var address = Znn.Instance.DefaultKeyPair.Address;
                var balance = await Znn.Instance.Ledger.GetAccountInfoByAddress(address);
                var depositedQsr =
                    await Znn.Instance.Embedded.Sentinel.GetDepositedQsr(address);
                WriteInfo("You have {depositedQsr} / {sentinelRegisterQsrAmount} QSR deposited for the Sentinel");

                if (balance.Qsr!.Value < Constants.SentinelRegisterQsrAmount)
                {
                    WriteInfo($"Required {FormatAmount(Constants.SentinelRegisterQsrAmount, Constants.CoinDecimals)} QSR");
                    WriteInfo($"Available {FormatAmount(balance.Qsr!.Value, Constants.CoinDecimals)} QSR");
                    return;
                }

                if (depositedQsr < Constants.SentinelRegisterQsrAmount)
                {
                    WriteInfo($"Depositing {Constants.SentinelRegisterQsrAmount - depositedQsr} QSR for the Sentinel");
                    await Znn.Instance.Send(Znn.Instance.Embedded.Sentinel
                        .DepositQsr(Constants.SentinelRegisterQsrAmount - depositedQsr));
                }
                WriteInfo("Done");
            }
        }

        [Verb("sentinel.withdrawQsr", HelpText = "Withdraw deposited QSR from the sentinel contract.")]
        public class WithdrawQsr : KeyStoreAndConnectionCommand
        {
            protected override async Task ProcessAsync()
            {
                var address = Znn.Instance.DefaultKeyPair.Address;

                var depositedQsr =
                    await Znn.Instance.Embedded.Sentinel.GetDepositedQsr(address);

                if (depositedQsr == 0)
                {
                    WriteInfo($"No deposited QSR to withdraw");
                    return;
                }

                WriteInfo($"Withdrawing {FormatAmount(depositedQsr, Constants.CoinDecimals)} QSR ...");

                await Znn.Instance.Send(Znn.Instance.Embedded.Sentinel.WithdrawQsr());

                WriteInfo("Done");
            }
        }
    }
}
