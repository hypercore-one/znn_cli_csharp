﻿using CommandLine;
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
                var pillarList = await ZnnClient.Embedded.Pillar.GetAll();

                foreach (var pillar in pillarList.List)
                {
                    WriteInfo($"#{pillar.Rank + 1} Pillar {pillar.Name} has a delegated weight of {FormatAmount(pillar.Weight, Constants.CoinDecimals)} ZNN");
                    WriteInfo($"    Producer address {pillar.ProducerAddress}");
                    WriteInfo($"    Momentums {pillar.CurrentStats.ProducedMomentums} / expected {pillar.CurrentStats.ExpectedMomentums}");
                }
            }
        }

        [Verb("pillar.register", HelpText = "Register pillar.")]
        public class Register : KeyStoreAndConnectionCommand
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
                var address = ZnnClient.DefaultKeyPair.Address;

                var producerAddress = ParseAddress(this.ProducerAddress, "producerAddress");
                if (!await AssertUserAddressAsync(producerAddress, "producerAddress"))
                    return;

                var rewardAddress = ParseAddress(this.RewardAddress, "rewardAddress");
                if (!await AssertUserAddressAsync(rewardAddress, "rewardAddress"))
                    return;
                
                var balance =
                    await ZnnClient.Ledger.GetAccountInfoByAddress(address);
                var qsrAmount =
                    (await ZnnClient.Embedded.Pillar.GetQsrRegistrationCost());
                var depositedQsr =
                    await ZnnClient.Embedded.Pillar.GetDepositedQsr(address);

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
                    await ZnnClient.Embedded.Pillar.CheckNameAvailability(newName);

                while (!ok)
                {
                    newName = Ask("This Pillar name is already reserved. Please choose another name for the Pillar");
                    ok = await ZnnClient.Embedded.Pillar.CheckNameAvailability(newName);
                }

                if (depositedQsr < qsrAmount)
                {
                    WriteInfo($"Depositing {FormatAmount(qsrAmount - depositedQsr, Constants.CoinDecimals)} QSR for the Pillar registration");
                    await ZnnClient.Send(ZnnClient.Embedded.Pillar.DepositQsr(qsrAmount - depositedQsr));
                }

                WriteInfo("Registering Pillar ...");

                await ZnnClient.Send(ZnnClient.Embedded.Pillar.Register(
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
        public class Revoke : KeyStoreAndConnectionCommand
        {
            [Value(0, Required = true, MetaName = "name")]
            public string? Name { get; set; }

            protected override async Task ProcessAsync()
            {
                var pillarList = await ZnnClient.Embedded.Pillar.GetAll();

                var ok = false;

                foreach (var pillar in pillarList.List)
                {
                    if (string.Equals(this.Name, pillar.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        ok = true;

                        if (pillar.IsRevocable)
                        {
                            WriteInfo($"Revoking Pillar {pillar.Name} ...");

                            await ZnnClient.Send(ZnnClient.Embedded.Pillar.Revoke(this.Name));

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
        public class Delegate : KeyStoreAndConnectionCommand
        {
            [Value(0, Required = true, MetaName = "name")]
            public string? Name { get; set; }

            protected override async Task ProcessAsync()
            {
                WriteInfo($"Delegating to Pillar {this.Name} ...");

                await ZnnClient.Send(ZnnClient.Embedded.Pillar.Delegate(this.Name));

                WriteInfo("Done");
            }
        }

        [Verb("pillar.undelegate", HelpText = "Undelegate pillar.")]
        public class Undelegate : KeyStoreAndConnectionCommand
        {
            protected override async Task ProcessAsync()
            {
                WriteInfo($"Delegating ...");

                await ZnnClient.Send(ZnnClient.Embedded.Pillar.Undelegate());

                WriteInfo("Done");
            }
        }

        [Verb("pillar.collect", HelpText = "Collect pillar rewards.")]
        public class Collect : KeyStoreAndConnectionCommand
        {
            protected override async Task ProcessAsync()
            {
                await ZnnClient.Send(ZnnClient.Embedded.Pillar.CollectReward());

                WriteInfo("Done");
                WriteInfo($"Use receiveAll to collect your Pillar reward(s) after 1 momentum");
            }
        }

        [Verb("pillar.depositQsr", HelpText = "Deposit QSR to the pillar contract.")]
        public class DepositQsr : KeyStoreAndConnectionCommand
        {
            protected override async Task ProcessAsync()
            {
                var address = ZnnClient.DefaultKeyPair.Address;

                var balance = await ZnnClient.Ledger.GetAccountInfoByAddress(address);
                var qsrAmount = await ZnnClient.Embedded.Pillar.GetQsrRegistrationCost();
                var depositedQsr =
                    await ZnnClient.Embedded.Pillar.GetDepositedQsr(address);

                WriteInfo($"You have {depositedQsr} / {qsrAmount} QSR deposited for the Pillar registration");

                if (balance.Qsr!.Value < qsrAmount)
                {
                    WriteInfo($"Required {FormatAmount(qsrAmount, Constants.CoinDecimals)} QSR");
                    WriteInfo($"Available {FormatAmount(balance.Qsr!.Value, Constants.CoinDecimals)} QSR");
                    return;
                }

                if (depositedQsr < qsrAmount)
                {
                    WriteInfo($"Depositing {FormatAmount(qsrAmount - depositedQsr, Constants.CoinDecimals)} QSR for the Pillar registration");
                    await ZnnClient.Send(ZnnClient.Embedded.Pillar.DepositQsr(qsrAmount - depositedQsr));
                }
                WriteInfo("Done");
            }
        }

        [Verb("pillar.withdrawQsr", HelpText = "Withdraw deposited QSR from the pillar contract.")]
        public class WithdrawQsr : KeyStoreAndConnectionCommand
        {
            protected override async Task ProcessAsync()
            {
                var address = ZnnClient.DefaultKeyPair.Address;

                var depositedQsr =
                    await ZnnClient.Embedded.Pillar.GetDepositedQsr(address);

                if (depositedQsr == 0)
                {
                    WriteInfo("No deposited QSR to withdraw");
                    return;
                }

                WriteInfo($"Withdrawing {FormatAmount(depositedQsr, Constants.CoinDecimals)} QSR ...");

                await ZnnClient.Send(ZnnClient.Embedded.Pillar.WithdrawQsr());

                WriteInfo("Done");
            }
        }
    }
}