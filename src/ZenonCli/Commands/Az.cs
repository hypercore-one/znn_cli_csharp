using CommandLine;
using Zenon.Model.Primitives;

namespace ZenonCli.Commands
{
    public class Az
    {
        [Verb("az.donate", HelpText = "Donate ZNN and QSR as fuel for the Mothership.")]
        public class Donate : KeyStoreAndConnectionCommand
        {
            [Value(0, Required = true, MetaName = "amount")]
            public string? Amount { get; set; }

            [Value(1, Default = "ZNN", MetaName = "tokenStandard", MetaValue = "[ZNN/QSR]")]
            public string? Zts { get; set; }

            protected override async Task ProcessAsync()
            {
                var address = ZnnClient.DefaultKeyPair.Address;

                var tokenStandard = ParseTokenStandard(Zts);
                if (tokenStandard != TokenStandard.ZnnZts ||
                    tokenStandard != TokenStandard.QsrZts)
                {
                    WriteError("You can only send ZNN or QSR.");
                    return;
                }

                var token = await GetTokenAsync(tokenStandard);
                var amount = ParseAmount(Amount!, token.Decimals);

                if (amount <= 0)
                {
                    WriteError($"You cannot send that amount.");
                    return;
                }

                await AssertBalanceAsync(address, tokenStandard, amount);

                WriteInfo($"Donating {FormatAmount(amount, token.Decimals)} ${token.Symbol} to Accelerator-Z ...");
                await ZnnClient.Send(ZnnClient.Embedded.Accelerator.Donate(amount, tokenStandard));
                WriteInfo("Done");
            }
        }
    }
}
