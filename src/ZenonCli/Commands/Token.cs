using CommandLine;
using System.Text.RegularExpressions;
using Zenon;
using Zenon.Model.Primitives;

namespace ZenonCli.Commands
{
    public class Token
    {
        [Verb("token.list", HelpText = "List all tokens.")]
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

                var tokenList = await Znn.Instance.Embedded.Token.GetAll(this.PageIndex.Value, this.PageSize.Value);

                foreach (var token in tokenList.List)
                {
                    if (token.TokenStandard == TokenStandard.ZnnZts || token.TokenStandard == TokenStandard.QsrZts)
                    {
                        WriteInfo(String.Format("{0} with symbol {1} and standard {2}",
                            token.TokenStandard == TokenStandard.ZnnZts ? token.Name : token.Name,
                            token.TokenStandard == TokenStandard.ZnnZts ? token.Symbol : token.Symbol,
                            token.TokenStandard == TokenStandard.ZnnZts ? token.TokenStandard : token.TokenStandard));
                        WriteInfo(String.Format("   Created by {0}",
                            token.TokenStandard == TokenStandard.ZnnZts ? token.Owner : token.Owner));
                        WriteInfo(String.Format("   {0} has {1} decimals, {2}, {3}, and {4}",
                            token.TokenStandard == TokenStandard.ZnnZts ? token.Name : token.Name,
                            token.Decimals,
                            token.IsMintable ? "is mintable" : "is not mintable",
                            token.IsBurnable ? "can be burned" : "cannot be burned",
                            token.IsUtility ? "is a utility coin" : "is not a utility coin"));
                        WriteInfo($"   The total supply is {FormatAmount(token.TotalSupply, token.Decimals)} and the maximum supply is {FormatAmount(token.MaxSupply, token.Decimals)}");
                    }
                    else
                    {
                        WriteInfo($"Token {token.Name} with symbol {token.Symbol} and standard {token.TokenStandard}");
                        WriteInfo($"   Issued by {token.Owner}");
                        WriteInfo(String.Format("   {0} has {1} decimals, {2}, {3}, and {4}",
                            token.Name,
                            token.Decimals,
                            token.IsMintable ? "can be minted" : "cannot be minted",
                            token.IsBurnable ? "can be burned" : "cannot be burned",
                            token.IsUtility ? "is a utility token" : "is not a utility token"));
                    }
                    WriteInfo($"   Domain `{token.Domain}`");
                }
            }
        }

        [Verb("token.getByStandard", HelpText = "List tokens by standard.")]
        public class GetByStandard : ConnectionCommand
        {
            [Value(0, Required = true, MetaName = "tokenStandard")]
            public string? TokenStandard { get; set; }

            protected override async Task ProcessAsync()
            {
                var tokenStandard = ParseTokenStandard(this.TokenStandard);
                var token = await Znn.Instance.Embedded.Token.GetByZts(tokenStandard);

                if (token == null)
                {
                    WriteError("The token does not exist");
                    return;
                }

                var type = GetTokenType(token.TokenStandard);

                WriteInfo($"{type} {token.Name} with symbol {token.Symbol} and standard {token.TokenStandard}");
                WriteInfo($"   Created by {token.Owner}");
                WriteInfo($"   The total supply is {FormatAmount(token.TotalSupply, token.Decimals)} and a maximum supply is {FormatAmount(token.MaxSupply, token.Decimals)}");
                WriteInfo(String.Format("   The {0} has {1} decimals {2} and {3}",
                    type,
                    token.Decimals,
                    token.IsMintable ? "can be minted" : "cannot be minted",
                    token.IsBurnable ? "can be burned" : "cannot be burned"));
            }
        }

        [Verb("token.getByOwner", HelpText = "List tokens by owner.")]
        public class GetByOwner : ConnectionCommand
        {
            [Value(0, Required = true, MetaName = "ownerAddress")]
            public string? OwnerAddress { get; set; }

            protected override async Task ProcessAsync()
            {
                var ownerAddress = ParseAddress(this.OwnerAddress, "ownerAddress");

                var tokens = await Znn.Instance.Embedded.Token.GetByOwner(ownerAddress);

                foreach (var token in tokens.List)
                {
                    var type = GetTokenType(token.TokenStandard);

                    WriteInfo($"{type} {token.Name} with symbol {token.Symbol} and standard {token.TokenStandard}");
                    WriteInfo($"   Created by {token.Owner}");
                    WriteInfo($"   The total supply is {FormatAmount(token.TotalSupply, token.Decimals)} and a maximum supply is {FormatAmount(token.MaxSupply, token.Decimals)}");
                    WriteInfo(String.Format("   The {0} has {1} decimals {2} and {3}",
                        type,
                        token.Decimals,
                        token.IsMintable ? "can be minted" : "cannot be minted",
                        token.IsBurnable ? "can be burned" : "cannot be burned"));
                }
            }
        }

        [Verb("token.issue", HelpText = "Issue token.")]
        public class Issue : KeyStoreAndConnectionCommand
        {
            [Value(0, Required = true, MetaName = "name")]
            public string? Name { get; set; }

            [Value(1, Required = true, MetaName = "symbol")]
            public string? Symbol { get; set; }

            [Value(2, Required = true, MetaName = "domain")]
            public string? Domain { get; set; }

            [Value(3, Required = true, MetaName = "totalSupply")]
            public long TotalSupply { get; set; }

            [Value(4, Required = true, MetaName = "maxSupply")]
            public long MaxSupply { get; set; }

            [Value(5, Required = true, MetaName = "decimals")]
            public int Decimals { get; set; }

            [Value(6, Required = true, MetaName = "isMintable")]
            public string? IsMintable { get; set; }

            [Value(7, Required = true, MetaName = "isBurnable")]
            public string? IsBurnable { get; set; }

            [Value(8, Required = true, MetaName = "isUtility")]
            public string? IsUtility { get; set; }

            protected override async Task ProcessAsync()
            {
                if (!Regex.IsMatch(this.Name!, "^([a-zA-Z0-9]+[-._]?)*[a-zA-Z0-9]$"))
                {
                    WriteError("The ZTS name contains invalid characters");
                    return;
                }

                if (!Regex.IsMatch(this.Symbol!, "^[A-Z0-9]+$"))
                {
                    WriteError("The ZTS symbol must be all uppercase");
                    return;
                }

                if (String.IsNullOrEmpty(this.Domain) || !Regex.IsMatch(this.Domain, "^([A-Za-z0-9][A-Za-z0-9-]{0,61}[A-Za-z0-9]\\.)+[A-Za-z]{2,}$"))
                {
                    WriteError("Invalid domain\nExamples of valid domain names:\n    zenon.network\n    www.zenon.network\n    quasar.zenon.network\n    zenon.community\nExamples of invalid domain names:\n    zenon.network/index.html\n    www.zenon.network/quasar");
                    return;
                }

                if (String.IsNullOrEmpty(this.Name) || this.Name.Length > 40)
                {
                    WriteError($"Invalid ZTS name length (min 1, max 40, current {this.Name!.Length}");
                }

                if (String.IsNullOrEmpty(this.Symbol) || this.Symbol.Length > 10)
                {
                    WriteError($"Invalid ZTS symbol length (min 1, max 10, current {this.Symbol!.Length}");
                }

                if (this.Domain.Length > 128)
                {
                    WriteError($"Invalid ZTS domain length (min 0, max 128, current {this.Domain.Length})");
                }

                bool mintable;
                if (this.IsMintable == "0" || String.Equals(this.IsMintable, "false", StringComparison.OrdinalIgnoreCase))
                {
                    mintable = false;
                }
                else if (this.IsMintable == "1" || String.Equals(this.IsMintable, "true", StringComparison.OrdinalIgnoreCase))
                {
                    mintable = true;
                }
                else
                {
                    WriteError("Mintable flag variable of type \"bool\" should be provided as either \"true\", \"false\", \"1\" or \"0\"");
                    return;
                }

                bool burnable;
                if (this.IsBurnable == "0" || String.Equals(this.IsBurnable, "false", StringComparison.OrdinalIgnoreCase))
                {
                    burnable = false;
                }
                else if (this.IsBurnable == "1" || String.Equals(this.IsBurnable, "true", StringComparison.OrdinalIgnoreCase))
                {
                    burnable = true;
                }
                else
                {
                    WriteError("Burnable flag variable of type \"bool\" should be provided as either \"true\", \"false\", \"1\" or \"0\"");
                    return;
                }

                bool utility;
                if (this.IsUtility == "0" || String.Equals(this.IsUtility, "false", StringComparison.OrdinalIgnoreCase))
                {
                    utility = false;
                }
                else if (this.IsUtility == "1" || String.Equals(this.IsUtility, "true", StringComparison.OrdinalIgnoreCase))
                {
                    utility = true;
                }
                else
                {
                    WriteError("Utility flag variable of type \"bool\" should be provided as either \"true\", \"false\", \"1\" or \"0\"");
                    return;
                }

                var totalSupply = this.TotalSupply;
                var maxSupply = this.MaxSupply;
                var decimals = this.Decimals;

                if (mintable == true)
                {
                    if (maxSupply < totalSupply)
                    {
                        WriteError("Max supply must to be larger than the total supply");
                        return;
                    }
                    if (maxSupply > (1 << 53))
                    {
                        WriteError($"Max supply must to be less than {((1 << 53)) - 1}");
                        return;
                    }
                }
                else
                {
                    if (maxSupply != totalSupply)
                    {
                        WriteError("Max supply must be equal to totalSupply for non-mintable tokens");
                        return;
                    }
                    if (totalSupply == 0)
                    {
                        WriteError("Total supply cannot be \"0\" for non-mintable tokens");
                        return;
                    }
                }

                WriteInfo("Issuing a new ZTS token will burn 1 ZNN");

                if (!Confirm("Do you want to proceed?"))
                    return;

                WriteInfo($"Issuing {this.Name} ZTS token ...");

                await Znn.Instance.Send(
                    Znn.Instance.Embedded.Token.IssueToken(
                        this.Name,
                        this.Symbol,
                        this.Domain,
                        totalSupply,
                        maxSupply,
                        decimals,
                        mintable,
                        burnable,
                        utility));

                WriteInfo("Done");
            }
        }

        [Verb("token.mint", HelpText = "Mint token.")]
        public class Mint : KeyStoreAndConnectionCommand
        {
            [Value(0, Required = true, MetaName = "tokenStandard")]
            public string? TokenStandard { get; set; }

            [Value(1, Required = true, MetaName = "amount")]
            public long Amount { get; set; }

            [Value(2, Required = true, MetaName = "receiveAddress")]
            public string? ReceiveAddress { get; set; }

            protected override async Task ProcessAsync()
            {
                var tokenStandard = ParseTokenStandard(this.TokenStandard);
                var amount = this.Amount;
                var mintAddress = ParseAddress(this.ReceiveAddress);
                var token = await Znn.Instance.Embedded.Token.GetByZts(tokenStandard);

                if (token == null)
                {
                    WriteError("The token does not exist");
                    return;
                }
                else if (!token.IsMintable)
                {
                    WriteError("The token is not mintable");
                    return;
                }

                WriteInfo("Minting ZTS token ...");

                await Znn.Instance.Send(
                    Znn.Instance.Embedded.Token.MintToken(tokenStandard, amount, mintAddress));

                WriteInfo("Done");
            }
        }

        [Verb("token.burn", HelpText = "Burn token.")]
        public class Burn : KeyStoreAndConnectionCommand
        {
            [Value(0, Required = true, MetaName = "tokenStandard")]
            public string? TokenStandard { get; set; }

            [Value(1, Required = true, MetaName = "amount")]
            public long Amount { get; set; }

            protected override async Task ProcessAsync()
            {
                var address = Znn.Instance.DefaultKeyPair.Address;
                var tokenStandard = ParseTokenStandard(this.TokenStandard);
                var amount = this.Amount;

                var info =
                    await Znn.Instance.Ledger.GetAccountInfoByAddress(address);
                var ok = true;

                foreach (var entry in info.BalanceInfoList)
                {
                    if (entry.Token.TokenStandard == tokenStandard &&
                        entry.Balance < amount)
                    {
                        WriteError($"You only have {FormatAmount(entry.Balance.Value, entry.Token.Decimals)} {entry.Token.Symbol} tokens");
                        ok = false;
                        break;
                    }
                }

                if (!ok)
                    return;

                WriteInfo($"Burning {this.TokenStandard} ZTS token ...");

                await Znn.Instance.Send(
                    Znn.Instance.Embedded.Token.BurnToken(tokenStandard, amount));

                WriteInfo("Done");
            }
        }

        [Verb("token.transferOwnership", HelpText = "Transfer token ownership to another address.")]
        public class TransferOwnership : KeyStoreAndConnectionCommand
        {
            [Value(0, Required = true, MetaName = "tokenStandard")]
            public string? TokenStandard { get; set; }

            [Value(1, Required = true, MetaName = "newOwnerAddress")]
            public string? NewOwnerAddress { get; set; }

            protected override async Task ProcessAsync()
            {
                WriteInfo("Transferring ZTS token ownership ...");

                var address = Znn.Instance.DefaultKeyPair.Address;
                var tokenStandard = ParseTokenStandard(this.TokenStandard);
                var newOwnerAddress = ParseAddress(this.NewOwnerAddress, "newOwnerAddress");
                var token = await Znn.Instance.Embedded.Token.GetByZts(tokenStandard);

                if (token == null)
                {
                    WriteError("The token does not exist");
                    return;
                }

                if (token.Owner != address)
                {
                    WriteError($"Not owner of token {tokenStandard}");
                    return;
                }

                await Znn.Instance.Send(Znn.Instance.Embedded.Token.UpdateToken(
                    tokenStandard, newOwnerAddress, token.IsMintable, token.IsBurnable));

                WriteInfo("Done");
            }
        }

        [Verb("token.disableMint", HelpText = "Disable a token's minting capability.")]
        public class DisableMint : KeyStoreAndConnectionCommand
        {
            [Value(0, Required = true, MetaName = "tokenStandard")]
            public string? TokenStandard { get; set; }

            protected override async Task ProcessAsync()
            {
                WriteInfo("Disabling ZTS token mintable flag ...");

                var address = Znn.Instance.DefaultKeyPair.Address;
                var tokenStandard = ParseTokenStandard(this.TokenStandard);
                var token = await Znn.Instance.Embedded.Token.GetByZts(tokenStandard);

                if (token == null)
                {
                    WriteError("The token does not exist");
                    return;
                }

                if (token.Owner != address)
                {
                    WriteError($"Not owner of token {tokenStandard}");
                    return;
                }

                await Znn.Instance.Send(Znn.Instance.Embedded.Token.UpdateToken(
                    tokenStandard, token.Owner, false, token.IsBurnable));

                WriteInfo("Done");
            }
        }
    }
}
