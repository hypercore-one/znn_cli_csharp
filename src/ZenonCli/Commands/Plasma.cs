﻿using CommandLine;
using Zenon;
using Zenon.Model.Primitives;

namespace ZenonCli.Commands
{
    public class Plasma
    {
        [Verb("plasma.list", HelpText = "List plasma fusion entries.")]
        public class List : ConnectionCommand
        {
            [Value(0, Default = 0, MetaName = "pageIndex")]
            public int? PageIndex { get; set; }

            [Value(1, Default = 25, MetaName = "PageSize")]
            public int? PageSize { get; set; }

            protected override async Task ProcessAsync()
            {
                if (!this.PageIndex.HasValue)
                    this.PageIndex = 0;

                if (!this.PageSize.HasValue)
                    this.PageSize = 25;

                if (this.PageIndex < 0)
                {
                    WriteError($"pageIndex must be at least 0");
                    return;
                }

                if (this.PageSize < 1 || this.PageSize > Constants.RpcMaxPageSize)
                {
                    WriteError($"pageSize must be at least 1 and at most {Constants.RpcMaxPageSize}");
                    return;
                }

                var address = Znn.Instance.DefaultKeyPair.Address;
                var fusionEntryList = await Znn.Instance.Embedded.Plasma.GetEntriesByAddress(address,
                        this.PageIndex.Value, this.PageSize.Value);

                if (fusionEntryList.Count > 0)
                {
                    WriteInfo($"Fusing {FormatAmount(fusionEntryList.QsrAmount, Constants.CoinDecimals)} QSR for Plasma in {fusionEntryList.Count} entries");
                }
                else
                {
                    WriteInfo("No Plasma fusion entries found");
                }

                foreach (var entry in fusionEntryList.List)
                {
                    WriteInfo($"  {FormatAmount(entry.QsrAmount, Constants.CoinDecimals)} QSR for {entry.Beneficiary}");
                    WriteInfo($"Can be canceled at momentum height: {entry.ExpirationHeight}. Use id {entry.Id} to cancel");
                }
            }
        }

        [Verb("plasma.get", HelpText = "Display the amount of plasma and QSR fused for an address.")]
        public class Get : ConnectionCommand
        {
            [Value(0, Required = true, MetaName = "address")]
            public string? address { get; set; }

            protected override async Task ProcessAsync()
            {
                var address = ParseAddress(this.address);
                var plasmaInfo = await Znn.Instance.Embedded.Plasma.Get(address);

                WriteInfo($"{address} has {plasmaInfo.CurrentPlasma} / {plasmaInfo.MaxPlasma} plasma with {FormatAmount(plasmaInfo.QsrAmount, Constants.CoinDecimals)} QSR fused.");
            }
        }

        [Verb("plasma.fuse", HelpText = "Fuse QSR to an address to generate plasma.")]
        public class Fuse : KeyStoreAndConnectionCommand
        {
            [Value(0, Required = true, MetaName = "toAddress")]
            public string? ToAddress { get; set; }

            [Value(1, Required = true, MetaName = "amount")]
            public long Amount { get; set; }

            protected override async Task ProcessAsync()
            {
                var beneficiary = ParseAddress(this.ToAddress, "toAddress");

                if (beneficiary == Address.EmptyAddress ||
                    beneficiary.IsEmbedded)
                {
                    WriteError("toAddress must be a user address.");
                    return;
                }

                var amount = this.Amount * Constants.OneQsr;

                if (amount < Constants.FuseMinQsrAmount)
                {
                    WriteInfo($"Invalid amount: {FormatAmount(amount, Constants.CoinDecimals)} QSR. Minimum staking amount is {FormatAmount(Constants.FuseMinQsrAmount, Constants.CoinDecimals)}");
                    return;
                }

                WriteInfo($"Fusing {FormatAmount(amount, Constants.CoinDecimals)} QSR to {beneficiary}");

                await Znn.Instance.Send(Znn.Instance.Embedded.Plasma.Fuse(beneficiary, amount));

                WriteInfo("Done");
            }
        }

        [Verb("plasma.cancel", HelpText = "Cancel a plasma fusion and receive the QSR back.")]
        public class Cancel : KeyStoreAndConnectionCommand
        {
            [Value(0, Required = true, MetaName = "id")]
            public string? Id { get; set; }

            protected override async Task ProcessAsync()
            {
                var address = Znn.Instance.DefaultKeyPair.Address;
                var id = ParseHash(this.Id, "id");

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
                WriteInfo($"Canceling Plasma fuse entry with id {id}");
                await Znn.Instance.Send(Znn.Instance.Embedded.Plasma.Cancel(id));
                WriteInfo("Done");
            }
        }
    }
}
