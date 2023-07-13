using CommandLine;
using Zenon;

namespace ZenonCli.Commands
{
    public class Az
    {
        [Verb("az.donate", HelpText = "Donate ZNN and QSR as fuel for the Mothership.")]
        public class Donate : KeyStoreAndConnectionCommand
        {
            [Value(0, Required = true, MetaName = "amount")]
            public long Amount { get; set; }

            [Value(1, Default = "ZNN", MetaName = "tokenStandard", MetaValue = "[ZNN/QSR]")]
            public string? TokenStandard { get; set; }

            protected override async Task ProcessAsync()
            {
                var address = ZnnClient.DefaultKeyPair.Address;

                var tokenStandard = ParseTokenStandard(this.TokenStandard);
                if (tokenStandard != Zenon.Model.Primitives.TokenStandard.ZnnZts ||
                    tokenStandard != Zenon.Model.Primitives.TokenStandard.QsrZts)
                {
                    WriteError("You can only send ZNN or QSR.");
                    return;
                }

                var token = await ZnnClient.Embedded.Token.GetByZts(tokenStandard);
                var amount = this.Amount * token.DecimalsExponent;

                if (amount <= 0)
                {
                    WriteError($"amount must be greater than 0");
                    return;
                }

                if (!await AssertBalanceAsync(ZnnClient, address, tokenStandard, amount))
                {
                    return;
                }

                WriteInfo($"Donating {FormatAmount(amount, token.Decimals)} ${token.Symbol} to Accelerator-Z ...");
                await ZnnClient.Send(ZnnClient.Embedded.Accelerator.Donate(amount, tokenStandard));
                WriteInfo("Done");
            }
        }
    }
}
