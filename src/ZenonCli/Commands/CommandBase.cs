using System.Numerics;
using Zenon;
using Zenon.Model.Embedded;
using Zenon.Model.Primitives;
using Zenon.Wallet;
using ZenonCli.Options;

namespace ZenonCli.Commands
{
    public abstract class CommandBase : ICommand
    {
        public async Task ExecuteAsync()
        {
            try
            {
                if (this is IKeyStoreOptions)
                {
                    await InitKeyStoreAsync((IKeyStoreOptions)this);
                }

                if (this is IConnectionOptions)
                {
                    await ConnectAsync((IConnectionOptions)this);
                }

                await ProcessAsync();

                if (this is IConnectionOptions)
                {
                    await DisconnectAsync((IConnectionOptions)this);
                }
            }
            catch (Exception e)
            {
                WriteError(e.Message);
            }
        }

        protected virtual Task ProcessAsync() { throw new NotSupportedException(); }

        #region IKeyStoreOptions

        protected async Task InitKeyStoreAsync(IKeyStoreOptions options)
        {
            await Task.Run(() =>
            {
                var allKeyStores =
                    Znn.Instance.KeyStoreManager.ListAllKeyStores();

                string? keyStorePath = null;
                if (allKeyStores == null || allKeyStores.Length == 0)
                {
                    // Make sure at least one keyStore exists
                    ThrowError("No keyStore in the default directory");
                }
                else if (options.KeyStore != null)
                {
                    // Use user provided keyStore: make sure it exists
                    keyStorePath = Path.Join(ZnnPaths.Default.Wallet, options.KeyStore);

                    WriteInfo(keyStorePath);

                    if (!File.Exists(keyStorePath))
                    {
                        ThrowError($"The keyStore {options.KeyStore} does not exist in the default directory");
                    }
                }
                else if (allKeyStores.Length == 1)
                {
                    // In case there is just one keyStore, use it by default
                    WriteInfo($"Using the default keyStore {Path.GetFileName(allKeyStores[0])}");
                    keyStorePath = allKeyStores[0];
                }
                else
                {
                    // Multiple keyStores present, but none is selected: action required
                    ThrowError($"Please provide a keyStore or an address. Use wallet.list to list all available keyStores");
                }

                string? passphrase = options.Passphrase;

                if (passphrase == null)
                {
                    WriteInfo("Insert passphrase:");
                    passphrase = ReadPassword();
                }

                int index = options.Index;

                try
                {
                    Znn.Instance.DefaultKeyStore = Znn.Instance.KeyStoreManager.ReadKeyStore(passphrase, keyStorePath);
                    Znn.Instance.DefaultKeyStorePath = keyStorePath;
                }
                catch (IncorrectPasswordException)
                {
                    ThrowError($"Invalid passphrase for keyStore {keyStorePath}");
                }

                Znn.Instance.DefaultKeyPair = Znn.Instance.DefaultKeyStore.GetKeyPair(index);
            });
        }

        #endregion

        #region IConnectionOptions

        protected async Task ConnectAsync(IConnectionOptions options)
        {
            if (options.Verbose)
                ((Zenon.Client.WsClient)Znn.Instance.Client.Value).TraceSourceLevels = 
                    System.Diagnostics.SourceLevels.Verbose;

            await Znn.Instance.Client.Value.StartAsync(new Uri(options.Url!), false);

            if (options.Chain != null)
            {
                if (!String.Equals(options.Chain, "auto", StringComparison.OrdinalIgnoreCase))
                {
                    Znn.Instance.ChainIdentifier = int.Parse(options.Chain);
                }
                else
                {
                    var momentum = await Znn.Instance.Ledger.GetFrontierMomentum();
                    Znn.Instance.ChainIdentifier = momentum.ChainIdentifier;
                }
            }
        }

        protected async Task DisconnectAsync(IConnectionOptions options)
        {
            await Znn.Instance.Client.Value.StopAsync();
        }

        #endregion

        public Address ParseAddress(string? address, string argumentName = "address")
        {
            try
            {
                return Address.Parse(address);
            }
            catch
            {
                WriteError($"{argumentName} must be a valid address");

                throw;
            }
        }

        public Hash ParseHash(string? hash, string argumentName = "hash")
        {
            try
            {
                return Hash.Parse(hash);
            }
            catch
            {
                WriteError($"{argumentName} is not a valid hash");

                throw;
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
            catch
            {
                WriteError($"{argumentName} must be a valid token standard");

                throw;
            }
        }

        #region Assertions

        public async Task<bool> AssertUserAddressAsync(Address address, string argumentName = "address")
        {
            return await Task.Run(() =>
            {
                if (address == Address.EmptyAddress ||
                address.IsEmbedded)
                {
                    WriteError($"{argumentName} is not an user address");
                    return false;
                }
                return true;
            });
        }

        public async Task<bool> AssertBalanceAsync(Znn client, Address address, TokenStandard tokenStandard, BigInteger amount)
        {
            var account = await client.Ledger
                .GetAccountInfoByAddress(address);

            var balance = account.BalanceInfoList
                .FirstOrDefault(x => x.Token.TokenStandard == tokenStandard);

            if (balance == null)
            {
                WriteError($"You do not have any {tokenStandard} tokens");
                return false;
            }

            if (balance.Balance < amount)
            {
                if (balance.Balance == BigInteger.Zero)
                {
                    WriteError($"You do not have any {balance.Token.Symbol} tokens");
                }
                else
                {
                    WriteError($"You only have {FormatAmount(balance.Balance.Value, balance.Token.Decimals)} {balance.Token.Symbol} tokens");
                }
                return false;
            }

            return true;
        }

        public async Task<bool> AssertLiquidityGuardianAsync()
        {
            var address = Znn.Instance.DefaultKeyPair.Address;

            var info = await Znn.Instance.Embedded.Liquidity
                .GetSecurityInfo();

            if (!info.Guardians.Any(x => x == address))
            {
                WriteDenied($"{address} is not a liquidity guardian");
                return false;
            }
            return true;
        }

        public async Task<bool> AssertLiquidityAdminAsync()
        {
            var address = Znn.Instance.DefaultKeyPair.Address;

            var info = await Znn.Instance.Embedded.Liquidity
                .GetLiquidityInfo();

            if (info.Administrator != address)
            {
                WriteDenied($"{address} is not the liquidity administrator");
                return false;
            }
            return true;
        }

        public async Task<bool> AssertBridgeGuardianAsync()
        {
            var address = Znn.Instance.DefaultKeyPair.Address;

            var info = await Znn.Instance.Embedded.Bridge
                .GetSecurityInfo();

            if (!info.Guardians.Any(x => x == address))
            {
                WriteDenied($"{address} is not a bridge guardian");
                return false;
            }
            return true;
        }

        public async Task<bool> AssertBridgeAdminAsync()
        {
            var address = Znn.Instance.DefaultKeyPair.Address;

            var info = await Znn.Instance.Embedded.Bridge
                .GetBridgeInfo();

            if (info.Administrator != address)
            {
                WriteDenied($"{address} is not the bridge administrator");
                return false;
            }
            return true;
        }

        #endregion

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
                    WriteInfo($"      Metadata: { tokenPair.Metadata}");
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
                var token = await Znn.Instance.Embedded.Token.GetByZts(request.TokenStandard)!;
                symbol = token.Symbol;
                decimals = token.Decimals;
            }

            WriteInfo($"Id: {request.Id}");
            WriteInfo($"   Network class: {request.NetworkClass}");
            WriteInfo($"   Chain id: {request.ChainId}");
            WriteInfo($"   To: {request.ToAddress}");
            WriteInfo($"   From: {(await Znn.Instance.Ledger.GetAccountBlockByHash(request.Id))?.Address}");
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
                var token = await Znn.Instance.Embedded.Token.GetByZts(request.TokenStandard)!;
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
            var token = await Znn.Instance.Embedded.Token.GetByZts(request.TokenStandard)!;
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
            return (amount / BigInteger.Pow(10, (int)decimals)).ToString("0." + new String('0', (int)decimals));
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
