using CommandLine;
using Zenon;
using Zenon.Model.Primitives;

namespace ZenonCli.Commands
{
    public class Plasma
    {
        [Verb("plasma.list", HelpText = "List plasma fusion entries.")]
        public class List : WalletAndConnectionCommand
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

                AssertPageRange(PageIndex.Value, PageSize.Value);

                var address = await Zdk!.DefaultWalletAccount.GetAddressAsync();
                var fusionEntryList = await Zdk!.Embedded.Plasma.GetEntriesByAddress(address,
                        (uint)PageIndex.Value, (uint)PageSize.Value);

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
                var plasmaInfo = await Zdk!.Embedded.Plasma.Get(address);

                WriteInfo($"{address} has {plasmaInfo.CurrentPlasma} / {plasmaInfo.MaxPlasma} plasma with {FormatAmount(plasmaInfo.QsrAmount, Constants.CoinDecimals)} QSR fused.");
            }
        }

        [Verb("plasma.fuse", HelpText = "Fuse QSR to an address to generate plasma.")]
        public class Fuse : WalletAndConnectionCommand
        {
            [Value(0, Required = true, MetaName = "toAddress")]
            public string? ToAddress { get; set; }

            [Value(1, Required = true, MetaName = "amount")]
            public string? Amount { get; set; }

            protected override async Task ProcessAsync()
            {
                var beneficiary = ParseAddress(ToAddress, "toAddress");

                if (beneficiary == Address.EmptyAddress ||
                    beneficiary.IsEmbedded)
                {
                    WriteError("toAddress must be a user address.");
                    return;
                }

                var amount = ParseAmount(Amount!, Constants.CoinDecimals);

                if (amount < Constants.FuseMinQsrAmount)
                {
                    WriteError($"Invalid amount {FormatAmount(amount, Constants.CoinDecimals)} QSR. Minimum staking amount is {FormatAmount(Constants.FuseMinQsrAmount, Constants.CoinDecimals)}");
                    return;
                }

                WriteInfo($"Fusing {FormatAmount(amount, Constants.CoinDecimals)} QSR to {beneficiary}");

                await Zdk!.SendAsync(Zdk!.Embedded.Plasma.Fuse(beneficiary, amount));

                WriteInfo("Done");
            }
        }

        [Verb("plasma.cancel", HelpText = "Cancel a plasma fusion and receive the QSR back.")]
        public class Cancel : WalletAndConnectionCommand
        {
            [Value(0, Required = true, MetaName = "id")]
            public string? Id { get; set; }

            protected override async Task ProcessAsync()
            {
                var address = await Zdk!.DefaultWalletAccount.GetAddressAsync();
                var id = ParseHash(Id, "id");

                uint pageIndex = 0;
                bool found = false;
                bool gotError = false;

                var fusions =
                    await Zdk!.Embedded.Plasma.GetEntriesByAddress(address);

                while (fusions.List.Length > 0)
                {
                    var entry = fusions.List.FirstOrDefault((x) => x.Id == id);
                    if (entry != null)
                    {
                        found = true;
                        if (entry.ExpirationHeight >
                            (await Zdk!.Ledger.GetFrontierMomentum()).Height)
                        {
                            WriteError($"Fuse entry can not be cancelled yet");
                            gotError = true;
                        }
                        break;
                    }
                    pageIndex++;
                    fusions = await Zdk!.Embedded.Plasma
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
                await Zdk!.SendAsync(Zdk!.Embedded.Plasma.Cancel(id));
                WriteInfo("Done");
            }
        }
    }
}
