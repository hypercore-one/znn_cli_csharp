using CommandLine;
using Zenon;

namespace ZenonCli.Commands
{
    public class Pillar
    {
        [Verb("pillar.list", HelpText = "List all pillars.")]
        public class List : ConnectionCommand
        {
            protected override async Task ProcessAsync()
            {
                var pillarList = await Zdk!.Embedded.Pillar.GetAll();

                foreach (var pillar in pillarList.List)
                {
                    WriteInfo($"#{pillar.Rank + 1} Pillar {pillar.Name} has a delegated weight of {FormatAmount(pillar.Weight, Constants.CoinDecimals)} ZNN");
                    WriteInfo($"    Producer address {pillar.ProducerAddress}");
                    WriteInfo($"    Momentums {pillar.CurrentStats.ProducedMomentums} / expected {pillar.CurrentStats.ExpectedMomentums}");
                }
            }
        }

        [Verb("pillar.register", HelpText = "Register pillar.")]
        public class Register : WalletAndConnectionCommand
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

            protected override async Task ProcessAsync()
            {
                var address = await Zdk!.DefaultWalletAccount.GetAddressAsync();

                var producerAddress = ParseAddress(this.ProducerAddress, "producerAddress");
                var rewardAddress = ParseAddress(this.RewardAddress, "rewardAddress");

                await AssertUserAddressAsync(producerAddress, "producerAddress");
                await AssertUserAddressAsync(rewardAddress, "rewardAddress");

                var balance =
                    await Zdk!.Ledger.GetAccountInfoByAddress(address);
                var qsrAmount =
                    (await Zdk!.Embedded.Pillar.GetQsrRegistrationCost());
                var depositedQsr =
                    await Zdk!.Embedded.Pillar.GetDepositedQsr(address);

                if ((balance.Znn < Constants.PillarRegisterZnnAmount ||
                    balance.Qsr < qsrAmount) &&
                    qsrAmount > depositedQsr)
                {
                    WriteInfo($"Cannot register Pillar with address {address}");
                    WriteInfo($"Required {FormatAmount(Constants.PillarRegisterZnnAmount, Constants.CoinDecimals)} ZNN and {FormatAmount(qsrAmount, Constants.CoinDecimals)} QSR");
                    WriteInfo($"Available {FormatAmount(balance.Znn!.Value, Constants.CoinDecimals)} ZNN and {FormatAmount(balance.Qsr!.Value, Constants.CoinDecimals)} QSR");
                    return;
                }

                WriteInfo($"Creating a new Pillar will burn the deposited QSR required for the Pillar slot");

                if (!Confirm("Do you want to proceed?"))
                    return;

                var newName = this.Name;
                var ok =
                    await Zdk!.Embedded.Pillar.CheckNameAvailability(newName);

                while (!ok)
                {
                    newName = Ask("This Pillar name is already reserved. Please choose another name for the Pillar");
                    ok = await Zdk!.Embedded.Pillar.CheckNameAvailability(newName);
                }

                if (depositedQsr < qsrAmount)
                {
                    WriteInfo($"Depositing {FormatAmount(qsrAmount - depositedQsr, Constants.CoinDecimals)} QSR for the Pillar registration");
                    await Zdk!.SendAsync(Zdk!.Embedded.Pillar.DepositQsr(qsrAmount - depositedQsr));
                }

                WriteInfo("Registering Pillar ...");

                await Zdk!.SendAsync(Zdk!.Embedded.Pillar.Register(
                    newName,
                    producerAddress,
                    rewardAddress,
                    this.GiveBlockRewardPercentage,
                    this.GiveDelegateRewardPercentage));
                WriteInfo("Done");
                WriteInfo($"Check after 2 momentums if the Pillar was successfully registered using pillar.list command");
            }
        }

        [Verb("pillar.revoke", HelpText = "Revoke pillar.")]
        public class Revoke : WalletAndConnectionCommand
        {
            [Value(0, Required = true, MetaName = "name")]
            public string? Name { get; set; }

            protected override async Task ProcessAsync()
            {
                var pillarList = await Zdk!.Embedded.Pillar.GetAll();

                var ok = false;

                foreach (var pillar in pillarList.List)
                {
                    if (string.Equals(this.Name, pillar.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        ok = true;

                        if (pillar.IsRevocable)
                        {
                            WriteInfo($"Revoking Pillar {pillar.Name} ...");

                            await Zdk!.SendAsync(Zdk!.Embedded.Pillar.Revoke(this.Name));

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
        }

        [Verb("pillar.delegate", HelpText = "Delegate to pillar.")]
        public class Delegate : WalletAndConnectionCommand
        {
            [Value(0, Required = true, MetaName = "name")]
            public string? Name { get; set; }

            protected override async Task ProcessAsync()
            {
                WriteInfo($"Delegating to Pillar {this.Name} ...");

                await Zdk!.SendAsync(Zdk!.Embedded.Pillar.Delegate(this.Name));

                WriteInfo("Done");
            }
        }

        [Verb("pillar.undelegate", HelpText = "Undelegate pillar.")]
        public class Undelegate : WalletAndConnectionCommand
        {
            protected override async Task ProcessAsync()
            {
                WriteInfo($"Undelegating ...");

                await Zdk!.SendAsync(Zdk!.Embedded.Pillar.Undelegate());

                WriteInfo("Done");
            }
        }

        [Verb("pillar.collect", HelpText = "Collect pillar rewards.")]
        public class Collect : WalletAndConnectionCommand
        {
            protected override async Task ProcessAsync()
            {
                await Zdk!.SendAsync(Zdk!.Embedded.Pillar.CollectReward());

                WriteInfo("Done");
                WriteInfo($"Use receiveAll to collect your Pillar reward(s) after 1 momentum");
            }
        }

        [Verb("pillar.depositQsr", HelpText = "Deposit QSR to the pillar contract.")]
        public class DepositQsr : WalletAndConnectionCommand
        {
            protected override async Task ProcessAsync()
            {
                var address = await Zdk!.DefaultWalletAccount.GetAddressAsync();

                var balance = await Zdk!.Ledger.GetAccountInfoByAddress(address);
                var qsrAmount = await Zdk!.Embedded.Pillar.GetQsrRegistrationCost();
                var depositedQsr =
                    await Zdk!.Embedded.Pillar.GetDepositedQsr(address);

                WriteInfo($"You have {FormatAmount(depositedQsr, Constants.CoinDecimals)} / {FormatAmount(qsrAmount, Constants.CoinDecimals)} QSR  for the Pillar registration");

                if (balance.Qsr!.Value < qsrAmount)
                {
                    WriteInfo($"Required {FormatAmount(qsrAmount, Constants.CoinDecimals)} QSR");
                    WriteInfo($"Available {FormatAmount(balance.Qsr!.Value, Constants.CoinDecimals)} QSR");
                    return;
                }

                if (depositedQsr < qsrAmount)
                {
                    WriteInfo($"Depositing {FormatAmount(qsrAmount - depositedQsr, Constants.CoinDecimals)} QSR for the Pillar registration");
                    await Zdk!.SendAsync(Zdk!.Embedded.Pillar.DepositQsr(qsrAmount - depositedQsr));
                }
                WriteInfo("Done");
            }
        }

        [Verb("pillar.withdrawQsr", HelpText = "Withdraw deposited QSR from the pillar contract.")]
        public class WithdrawQsr : WalletAndConnectionCommand
        {
            protected override async Task ProcessAsync()
            {
                var address = await Zdk!.DefaultWalletAccount.GetAddressAsync();

                var depositedQsr =
                    await Zdk!.Embedded.Pillar.GetDepositedQsr(address);

                if (depositedQsr == 0)
                {
                    WriteInfo("No deposited QSR to withdraw");
                    return;
                }

                WriteInfo($"Withdrawing {FormatAmount(depositedQsr, Constants.CoinDecimals)} QSR ...");

                await Zdk!.SendAsync(Zdk!.Embedded.Pillar.WithdrawQsr());

                WriteInfo("Done");
            }
        }
    }
}