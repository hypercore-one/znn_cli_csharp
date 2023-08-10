using System.Globalization;
using System.Numerics;
using Zenon;
using Zenon.Client;
using Zenon.Model.Embedded;
using Zenon.Model.Primitives;
using Zenon.Utils;
using Zenon.Wallet;
using Zenon.Wallet.Ledger;
using ZenonCli.Options;

namespace ZenonCli.Commands
{
    public abstract class CommandBase : ICommand
    {
        public CommandBase()
        { }

        protected IEnumerable<IWalletManager> WalletManagers = new List<IWalletManager>()
        {
            new KeyStoreManager(),
            new LedgerManager(),
        };
        protected IWalletManager? WalletManager;
        protected IWalletDefinition? WalletDefinition;
        protected IWallet? Wallet;
        protected IWalletAccount? WalletAccount;
        protected Zdk? Zdk;

        public async Task ExecuteAsync()
        {
            try
            {
                if (this is Options.IWalletOptions)
                {
                    await InitWalletAsync((Options.IWalletOptions)this);
                }

                if (this is IClientOptions)
                {
                    await ConnectAsync((IClientOptions)this);
                }

                await ProcessAsync();

                if (this is IClientOptions)
                {
                    await DisconnectAsync((IClientOptions)this);
                }

                if (this is Options.IWalletOptions)
                {
                    await DisposeWalletAsync((Options.IWalletOptions)this);
                }
            }
            catch (Exception e)
            {
                WriteError(e.Message);
            }
        }

        protected virtual Task ProcessAsync() { throw new NotSupportedException(); }

        #region IWalletOptions

        protected async Task DisposeWalletAsync(Options.IWalletOptions options)
        {
            await Task.Run(() =>
            {
                Helper.Dispose(Wallet);
                Helper.Dispose(WalletManager);

                WalletAccount = null;
                Wallet = null;
                WalletManager = null;
            });
        }

        protected async Task InitWalletAsync(Options.IWalletOptions options)
        {
            var walletDefinitions = await GetAllWalletDefinitionsAsync();

            string? keyStorePath = null;
            if (walletDefinitions == null || walletDefinitions.Count() == 0)
            {
                ThrowError("No wallets founds");
            }
            else if (options.WalletName != null)
            {
                string? walletName;

                if (options.WalletName == "nanos" ||
                    options.WalletName == "nanosp" ||
                    options.WalletName == "nanox" ||
                    options.WalletName == "stax")
                {
                    walletName = options.WalletName.Insert(4, " ").Trim();
                }
                else
                {
                    walletName = options.WalletName;
                }

                // Use user provided keyStore: make sure it exists
                WalletDefinition = walletDefinitions.FirstOrDefault(x => String.Equals(x.WalletName, walletName, StringComparison.OrdinalIgnoreCase));

                if (WalletDefinition == null)
                {
                    ThrowError($"The wallet {options.WalletName} does not exist");
                }
            }
            else if (walletDefinitions.Count() == 1)
            {
                // In case there is just one keyStore, use it by default
                WriteInfo($"Using the default wallet {walletDefinitions.First().WalletName}");

                WalletDefinition = walletDefinitions.First();
            }
            else
            {
                // Multiple wallets present, but none is selected: action required
                ThrowError($"Please provide a wallet name. Use wallet.list to list all available wallets");
            }

            Zenon.Wallet.IWalletOptions? walletOptions;

            if (options.WalletName == "nanos" ||
                options.WalletName == "nanosp" ||
                options.WalletName == "nanox" ||
                options.WalletName == "stax")
            {
                walletOptions = null;
            }
            else
            {
                string? passphrase = options.Passphrase;

                if (passphrase == null)
                {
                    WriteInfo("Insert passphrase:");
                    passphrase = ReadPassword();
                }

                walletOptions = new KeyStoreOptions() { DecryptionPassword = passphrase };
            }

            int index = options.Index;

            try
            {
                foreach (var walletManager in WalletManagers)
                {
                    if (await walletManager.SupportsWalletAsync(WalletDefinition))
                    {
                        WalletManager = walletManager;
                        Wallet = await walletManager!.GetWalletAsync(WalletDefinition, walletOptions);
                        WalletAccount = await Wallet!.GetAccountAsync(index);
                        break;
                    }
                }
            }
            catch (IncorrectPasswordException)
            {
                ThrowError($"Invalid passphrase for wallet {keyStorePath}");
            }
        }

        #endregion

        #region IClientOptions

        protected async Task ConnectAsync(IClientOptions options)
        {
            var clientOptions = new WsClientOptions()
            {
                ProtocolVersion = Constants.ProtocolVersion,
                ChainIdentifier = Constants.ChainId,
                TraceSourceLevels = options.Verbose ? System.Diagnostics.SourceLevels.Verbose : System.Diagnostics.SourceLevels.Warning
            };

            var client = new WsClient(options.Url!, clientOptions);
            await client.ConnectAsync();
            
            if (options.Chain != null)
            {
                if (!String.Equals(options.Chain, "auto", StringComparison.OrdinalIgnoreCase))
                {
                    Helper.Dispose(client);

                    clientOptions.ChainIdentifier = int.Parse(options.Chain);
                    client = new WsClient(options.Url!, clientOptions);
                    await client.ConnectAsync();
                }
                else
                {
                    var momentum = await new Zdk(client).Ledger.GetFrontierMomentum();

                    Helper.Dispose(client);

                    clientOptions.ChainIdentifier = momentum.ChainIdentifier;
                    client = new WsClient(options.Url!, clientOptions);
                    await client.ConnectAsync();
                }
            }

            Zdk = new Zdk(client);
            Zdk!.DefaultWalletAccount = WalletAccount;
        }

        protected async Task DisconnectAsync(IClientOptions options)
        {
            await Task.Run(() =>
            {
                Helper.Dispose(Zdk!.Client);
                Zdk = null;
            });
        }

        #endregion

        public Address ParseAddress(string? address, string argumentName = "address")
        {
            try
            {
                return Address.Parse(address);
            }
            catch (Exception e)
            {
                throw new Exception($"{argumentName} must be a valid address", e);
            }
        }

        public Hash ParseHash(string? hash, string argumentName = "hash")
        {
            try
            {
                return Hash.Parse(hash);
            }
            catch (Exception e)
            {
                throw new Exception($"{argumentName} is not a valid hash", e);
            }
        }

        protected BigInteger ParseAmount(string value, long decimals, string argumentName = "amount")
        {
            try
            {
                return AmountUtils.ExtractDecimals(double.Parse(value, NumberStyles.Any, CultureInfo.InvariantCulture), (int)decimals);
            }
            catch (Exception e)
            {
                throw new Exception($"{argumentName} is not a valid number", e);
            }
        }

        public TokenStandard ParseTokenStandard(string? zts, string argumentName = "tokenStandard")
        {
            try
            {
                if (string.Equals(zts, "ZNN", StringComparison.OrdinalIgnoreCase))
                {
                    return TokenStandard.ZnnZts;
                }
                else if (string.Equals(zts, "QSR", StringComparison.OrdinalIgnoreCase))
                {
                    return TokenStandard.QsrZts;
                }
                else
                {
                    return TokenStandard.Parse(zts);
                }
            }
            catch (Exception e)
            {
                throw new Exception($"{argumentName} must be a valid token standard", e);
            }
        }

        #region Assertions

        public void AssertPageRange(int pageIndex, int pageSize)
        {
            if (pageIndex < 0)
            {
                throw new Exception($"The page index must be a positive integer");
            }

            if (pageSize < 1 || pageSize > Constants.RpcMaxPageSize)
            {
                throw new Exception($"The page size must be greater than 0 and less than or equal to {Constants.RpcMaxPageSize}");
            }
        }

        public async Task AssertUserAddressAsync(Address address, string argumentName = "address")
        {
            await Task.Run(() =>
            {
                if (address == Address.EmptyAddress ||
                    address.IsEmbedded)
                {
                    throw new Exception($"{argumentName} is not an user address");
                }
            });
        }

        public async Task AssertBalanceAsync(Address address, TokenStandard tokenStandard, BigInteger amount)
        {
            var account = await Zdk!.Ledger
                .GetAccountInfoByAddress(address);

            var balance = account.BalanceInfoList
                .FirstOrDefault(x => x.Token.TokenStandard == tokenStandard);

            if (balance == null)
            {
                throw new Exception($"You do not have any {tokenStandard} tokens");
            }

            if (balance.Balance < amount)
            {
                if (balance.Balance == BigInteger.Zero)
                {
                    throw new Exception($"You do not have any {balance.Token.Symbol} tokens");
                }
                else
                {
                    throw new Exception($"You only have {FormatAmount(balance.Balance.Value, balance.Token.Decimals)} {balance.Token.Symbol} tokens");
                }
            }
        }

        public async Task AssertLiquidityGuardianAsync()
        {
            var address = await Zdk!.DefaultWalletAccount.GetAddressAsync();

            var info = await Zdk!.Embedded.Liquidity
                .GetSecurityInfo();

            if (!info.Guardians.Any(x => x == address))
            {
                throw new Exception($"{address} is not a liquidity guardian");
            }
        }

        public async Task AssertLiquidityAdminAsync()
        {
            var address = await Zdk!.DefaultWalletAccount.GetAddressAsync();

            var info = await Zdk!.Embedded.Liquidity
                .GetLiquidityInfo();

            if (info.Administrator != address)
            {
                throw new Exception($"{address} is not the liquidity administrator");
            }
        }

        public async Task AssertBridgeGuardianAsync()
        {
            var address = await Zdk!.DefaultWalletAccount.GetAddressAsync();

            var info = await Zdk!.Embedded.Bridge
                .GetSecurityInfo();

            if (!info.Guardians.Any(x => x == address))
            {
                throw new Exception($"{address} is not a bridge guardian");
            }
        }

        public async Task AssertBridgeAdminAsync()
        {
            var address = await Zdk!.DefaultWalletAccount.GetAddressAsync();

            var info = await Zdk!.Embedded.Bridge
                .GetBridgeInfo();

            if (info.Administrator != address)
            {
                throw new Exception($"{address} is not the bridge administrator");
            }
        }

        #endregion

        #region FormatUtils

        protected async Task<Zenon.Model.NoM.Token> GetTokenAsync(TokenStandard tokenStandard)
        {
            try
            {
                var token = await Zdk!.Embedded.Token.GetByZts(tokenStandard);
                return token!;
            }
            catch (Exception e)
            {
                throw new Exception($"{tokenStandard} does not exist", e);
            }
        }

        #endregion

        protected async Task<IEnumerable<IWalletDefinition>> GetAllWalletDefinitionsAsync()
        {
            var result = new List<IWalletDefinition>();

            foreach (var walletManager in WalletManagers)
            {
                result.AddRange(await walletManager.GetWalletDefinitionsAsync());
            }

            return result;
        }

        protected string GetTokenType(TokenStandard zts)
        {
            if (zts == TokenStandard.QsrZts ||
                zts == TokenStandard.ZnnZts)
            {
                return "Coin";
            }
            return "Token";
        }

        protected void Write(BridgeNetworkInfo network)
        {
            WriteInfo($"   Name: {network.Name}");
            WriteInfo($"   Network class: {network.NetworkClass}");
            WriteInfo($"   Chain id: {network.ChainId}");
            WriteInfo($"   Contract address: {network.ContractAddress}");
            WriteInfo($"   Metadata: {network.Metadata}");
            if (network.TokenPairs != null && network.TokenPairs.Length != 0)
            {
                WriteInfo($"   Token pairs:");

                foreach (var tokenPair in network.TokenPairs)
                {
                    WriteInfo($"      Token standard: {tokenPair.TokenStandard}");
                    WriteInfo($"      Token address: {tokenPair.TokenAddress}");
                    WriteInfo($"      Bridgeable: {tokenPair.Bridgeable}");
                    WriteInfo($"      Redeemable: {tokenPair.Redeemable}");
                    WriteInfo($"      Owned: {tokenPair.Owned}");
                    WriteInfo($"      MinAmount: {tokenPair.MinAmount}");
                    WriteInfo($"      Fee %: {tokenPair.FeePercentage}");
                    WriteInfo($"      Redeem delay: {tokenPair.RedeemDelay}");
                    WriteInfo($"      Metadata: {tokenPair.Metadata}");
                    WriteInfo("");
                }
            }
            WriteInfo($"");
        }

        protected async Task WriteAsync(WrapTokenRequest request)
        {
            string symbol;
            long decimals;
            if (request.TokenStandard == TokenStandard.ZnnZts)
            {
                symbol = "ZNN";
                decimals = Constants.CoinDecimals;
            }
            else if (request.TokenStandard == TokenStandard.QsrZts)
            {
                symbol = "QSR";
                decimals = Constants.CoinDecimals;
            }
            else
            {
                var token = await GetTokenAsync(request.TokenStandard)!;
                symbol = token.Symbol;
                decimals = token.Decimals;
            }

            WriteInfo($"Id: {request.Id}");
            WriteInfo($"   Network class: {request.NetworkClass}");
            WriteInfo($"   Chain id: {request.ChainId}");
            WriteInfo($"   To: {request.ToAddress}");
            WriteInfo($"   From: {(await Zdk!.Ledger.GetAccountBlockByHash(request.Id))?.Address}");
            WriteInfo($"   Token standard: {request.TokenStandard}");
            WriteInfo($"   Amount: {FormatAmount(request.Amount, decimals)} {symbol}");
            WriteInfo($"   Fee: {FormatAmount(request.Fee, decimals)} {symbol}");
            WriteInfo($"   Signature: {request.Signature}");
            WriteInfo($"   Creation momentum height: {request.CreationMomentumHeight}");
            WriteInfo("");
        }

        protected async Task WriteAsync(UnwrapTokenRequest request)
        {
            string symbol;
            long decimals;
            if (request.TokenStandard == TokenStandard.ZnnZts)
            {
                symbol = "ZNN";
                decimals = Constants.CoinDecimals;
            }
            else if (request.TokenStandard == TokenStandard.QsrZts)
            {
                symbol = "QSR";
                decimals = Constants.CoinDecimals;
            }
            else
            {
                var token = await GetTokenAsync(request.TokenStandard)!;
                symbol = token.Symbol;
                decimals = token.Decimals;
            }

            WriteInfo($"Id: {request.TransactionHash}");
            WriteInfo($"   Network class: {request.NetworkClass}");
            WriteInfo($"   Chain id: {request.ChainId}");
            WriteInfo($"   Log index: {request.LogIndex}");
            WriteInfo($"   To: {request.ToAddress}");
            WriteInfo($"   Token standard: {request.TokenStandard}");
            WriteInfo($"   Amount: {FormatAmount(request.Amount, decimals)} {symbol}");
            WriteInfo($"   Signature: {request.Signature}");
            WriteInfo($"   Registration momentum height: {request.RegistrationMomentumHeight}");
            WriteInfo($"   Redeemed: {request.Redeemed == 1}");
            WriteInfo($"   Revoked: {request.Revoked == 1}");
            WriteInfo("");
        }

        protected async Task WriteRedeemAsync(UnwrapTokenRequest request)
        {
            var token = await GetTokenAsync(request.TokenStandard)!;
            var decimals = token.Decimals;

            WriteInfo($"Redeeming id: {request.TransactionHash}");
            WriteInfo($"   Log index: {request.LogIndex}");
            WriteInfo($"   Amount: {FormatAmount(request.Amount, decimals)} {token.Symbol}");
            WriteInfo($"   To: {request.ToAddress}");
            WriteInfo("");
        }

        public void WriteDenied(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("Permisison denied! ");
            Console.ResetColor();
            Console.WriteLine(message);
        }

        public void WriteError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("Error! ");
            Console.ResetColor();
            Console.WriteLine(message);
        }

        public void WriteWarning(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("Warning! ");
            Console.ResetColor();
            Console.WriteLine(message);
        }

        public void WriteInfo(string message)
        {
            Console.WriteLine(message);
        }

        public string? ReadPassword()
        {
            string? password = null;
            while (true)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter)
                    break;
                password += key.KeyChar;
            }
            return password;
        }

        public bool Confirm(string message, bool defaultValue = false)
        {
            while (true)
            {
                Console.WriteLine(message + " (Y/N):");
                var key = Console.ReadKey();
                if (key.Key == ConsoleKey.Y)
                    return true;
                else if (key.Key == ConsoleKey.N)
                    return false;
                else if (key.Key == ConsoleKey.Enter)
                    return defaultValue;
                else
                    Console.WriteLine($"Invalid value: {key}");
            }
        }

        public string? Ask(string message)
        {
            Console.WriteLine(message);
            return Console.ReadLine();
        }

        public string FormatAmount(BigInteger amount, long decimals)
        {
            return AmountUtils.AddDecimals(amount, (int)decimals);
        }

        public string FormatDuration(long seconds)
        {
            return TimeSpan.FromSeconds(seconds).ToString();
        }

        public void ThrowError(string message)
        {
            throw new Exception(message);
        }
    }
}
