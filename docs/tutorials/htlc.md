# Tutorial: Hashed Timelocked Contact (HTLC)

Some time ago George published his work on [BTC Atomic Swaps via HTLC: Cope Edition](https://github.com/Big-Inches-Club-House/bich/discussions/1). I've been wanting to look at the code and create the necessary tools to interact with the contract. This way others can try to understand how it works and what you can do with it.

Before we start I think it's important to understand what a **HTLC** is and what it is used for. Therefore I highly recommend that you watch this simple explainer [YouTube video](https://www.youtube.com/watch?v=hs79R8kd_70). The video explains two scenarios: the first scenario explains the use of a **HTLC** between two persons using BTC. The second scenario explains the use of a **HTLC** between two persons exchanging two different coins and thus effectively creating an Atomic Swap.

In this tutorial we're going to create both scenario's' on our own devnet NoM network.

## Installation (Windows 10+)

To be able to interact with the **HTLC** we need to compile a modified node and tools and setup a **devnet**. Below are the instructions for setting up everything up on Windows.

### Chocolatey

We will use **Chocolatey** for installing the necessary dependencies. **Chocolatey** is a Packet Manager for Windows. Check out their website at https://chocolatey.org/ for more information.

Make sure **Chocolatey** is installed on your system by following the instructions at https://chocolatey.org/install#individual

After installing **Chocolatey**, ensure that you are using an [PowerShell administrative shell](https://www.howtogeek.com/742916/how-to-open-windows-powershell-as-an-admin-in-windows-10/).

### Git

We will need **git** to interact wth the GitHub repositories. Execute the following command in PowerShell.

``` powershell
choco install git -y
```

### Go

We will need Go to to compile the go-zenon code. Execute the following command in PowerShell.

``` powershell
choco install go -y
```

### MinGW

We will need a GCC compiler to compile the go-zenon code. Execute the following command in PowerShell.

``` powershell
choco install mingw -y
```

### DotNet SDK

We will need DotNet SDK to compile the znn-cli-cscharp code. Execute the following command in PowerShell.

``` powershell
choco install dotnet-sdk -y
```

## Configuration

After installing all the above tools make sure you close and open a new [PowerShell administrative shell](https://www.howtogeek.com/742916/how-to-open-windows-powershell-as-an-admin-in-windows-10/).

To use **git** we need to configure an user. Use you GitHub account if you have one; otherwise you can use anything you like.

``` powershell
git config --global user.email [your e-mail]
git config --global user.name [your name]
```

## Compilation

We will make a **repos** directory under the current userprofile to store all our work. Replace the path if you want it stored on a different location.

``` powershell 
cd $ENV:USERPROFILE
mkdir repos
cd repos
```

### BICH go-zenon

Create a clone of the **devnet** branch of the [Big Inches Club House go-zenon repository](https://github.com/Big-Inches-Club-House/go-zenon.git).

``` powershell
git clone -b devnet https://github.com/Big-Inches-Club-House/go-zenon.git
```

Change directory to the **go-zenon** directory.

``` powershell
cd go-zenon
```

Merge the **htlc** branch with the **devnet** branch.

``` powershell
git merge origin/htlc
```

Compile the **go-zenon** code.

``` powershell
go build -o build/libznn.dll -buildmode=c-shared -tags libznn main_libznn.go
go build -o build/znnd.exe main.go
```

Configure and run a **devnet** node.

``` powershell
./build/znnd --data ./devnet generate-devnet --genesis-block=z1qqjnwjjpnue8xmmpanz6csze6tcmtzzdtfsww7,40000,400000
./build/znnd --data ./devnet
```

Keep the shell open during the duration of this tutorial. It is now possible to connect the **Zenon Explorer** to the node.

Open a web browser and go to https://explorer.zenon.network and connect the **Zenon Explorer** to http://127.0.0.1:35997

Search for the address **z1qqjnwjjpnue8xmmpanz6csze6tcmtzzdtfsww7**

> Try using a different browser if the Zenon Explorer does not want to connect. 

### znn-cli-csharp

Open a new [PowerShell administrative shell](https://www.howtogeek.com/742916/how-to-open-windows-powershell-as-an-admin-in-windows-10/) and change directory to **repos**.

``` powershell
cd $ENV:USERPROFILE/repos
```

Create a clone of the **htlc** branch of the [KingGorrin znn-cli-csharp repository](https://github.com/KingGorrin/znn_cli_csharp.git).

``` powershell
git clone -b htlc https://github.com/KingGorrin/znn_cli_csharp.git
```

Change directory to the **znn_cli_csharp** directory.

``` powershell
cd znn_cli_csharp
```

Compile the **znn_cli_csharp** code

``` powershell
dotnet build ./src/ZenonCli/ZenonCli.csproj
```

Change directory to the binaries directory.

``` powershell
cd ./bin/ZenonCli/net6.0/
```

## Setup wallets

We will create two wallets for Alice and Bob using the csharp version of the znn-cli.

The primary address of Alice is **z1qqjnwjjpnue8xmmpanz6csze6tcmtzzdtfsww7**

The primary address of Bob is **z1qpsjv3wzzuuzdudg7tf6uhvr6sk4ag8me42ua4** 

Both wallets use the passphrase **secret**

> Notice that when we configured the devnet we used Alice's address as the genesis block and gave her some initial funding to play with.

Execute the following command to create Alice's wallet.

``` powershell
./znn-cli wallet.createFromMnemonic "route become dream access impulse price inform obtain engage ski believe awful absent pig thing vibrant possible exotic flee pepper marble rural fire fancy" secret Alice
```

Execute the following command to create Bob's wallet.

``` powershell
./znn-cli wallet.createFromMnemonic "alone emotion announce page spend eager middle lucky frame craft junk artefact upper finger drive corn version slot blade picnic festival wealth critic silver" secret Bob
```

### Generate plasma

In order to speed up the process of sending transactions on our devnet we will generate some plasma on both addresses.

Alice will fuse 100 QSR on both addresses using the **devnet** id 321.

> The first transaction can take a while when using PoW.

``` powershell
./znn-cli plasma.fuse z1qqjnwjjpnue8xmmpanz6csze6tcmtzzdtfsww7 100 -k Alice -n 321
./znn-cli plasma.fuse z1qpsjv3wzzuuzdudg7tf6uhvr6sk4ag8me42ua4 100 -k Alice -n 321
```

## HTLC scenario 1

Alice wants to buy something from Bob. She will give Bob 1 hour the time to deliver her the goods in exchange for 100 ZNN.

Alice checks her balance to make sure she has enough funds.

``` powershell
./znn-cli balance -k Alice -n 321
```

Alice creates a hash which she will use to lock the funds.

``` powershell
./znn-cli createHash "all your znn belong to us"
```

Alice locks 100 ZNN for Bob for 1 hour and uses the hash a the hashlock.

``` powershell
./znn-cli htlc.create z1qpsjv3wzzuuzdudg7tf6uhvr6sk4ag8me42ua4 ZNN 100 3600 de543a6cab8db5bdc086d1720b97b0f097458841cd0264d789350e3b07587f5b -k Alice -n 321
```

Alice makes sure the HTLC is created and the funds have been deducted from her account before notifying Bob.

>  Wait 2 Momentums for the transaction to be processed.

``` powershell
./znn-cli htlc.timeLocked -k Alice -n 321
./znn-cli balance -k Alice -n 321
```

Alice notifies Bob for him to inspect the HTLC.

``` powershell
./znn-cli htlc.hashLocked -k Bob -n 321
```

Bob inspects the HTLC and agrees to the conditions and writes down the HTLC hash id.

Bob has 1 hour the time to do his part of the deal. Once finished Alice will reveal the pre-image to Bob so that he can unlock the 100 ZNN.

``` powershell
./znn-cli htlc.unlock [hash id] "all your znn belong to us" -k Bob -n 321
```

> Wait 2 Momentums for the transaction to be processed.

Bob has unlocked the 100 ZNN which the contract has send to his wallet. Bob needs to receive the unreceived transactions.

``` powershell
./znn-cli receiveAll -k Bob -n 321
```

Bob is trilled and checks his balance to make sure everything is fine.

``` powershell
./znn-cli balance -k Bob -n 321
```

## HTLC scenario 2

Alice wants to exchange 100 ZNN for 1000 QSR with Bob.

Bob will need some QSR in order for this to work. We will pretend this did not happen.

``` powershell
./znn-cli send z1qpsjv3wzzuuzdudg7tf6uhvr6sk4ag8me42ua4 1000 QSR -k Alice -n 321
```

> Wait 2 Momentums for the transaction to be processed.

Bob needs to receive the unreceived transactions.

``` powershell
./znn-cli receiveAll -k Bob -n 321
```

Alice checks her balance to make sure she has enough funds.

``` powershell
./znn-cli balance -k Alice -n 321
```

Alice creates a hash which she will use to lock the funds.

``` powershell
./znn-cli createHash "all your znn belong to us"
```

Alice locks 100 ZNN for Bob for 1 hour and uses the hash a the hashlock.

``` powershell
./znn-cli htlc.create z1qpsjv3wzzuuzdudg7tf6uhvr6sk4ag8me42ua4 ZNN 100 3600 de543a6cab8db5bdc086d1720b97b0f097458841cd0264d789350e3b07587f5b -k Alice -n 321
```

Alice makes sure the HTLC is created and the funds have been deducted from her account before notifying Bob.

>  Wait 2 Momentums for the transaction to be processed.

``` powershell
./znn-cli htlc.timeLocked -k Alice -n 321
./znn-cli balance -k Alice -n 321
```

Alice notifies Bob for him to inspect the HTLC.

``` powershell
./znn-cli htlc.hashLocked -k Bob -n 321
```

Bob inspects the HTLC and agrees to the conditions and creates a HTLC locking 1000 QSR for Alice for 30 minutes using the same hashlock Alice used.

>  It's important that Bob's HTLC expires before Alice's

``` powershell
./znn-cli htlc.create z1qqjnwjjpnue8xmmpanz6csze6tcmtzzdtfsww7 QSR 1000 1800 de543a6cab8db5bdc086d1720b97b0f097458841cd0264d789350e3b07587f5b -k Bob -n 321
```

Bob makes sure the HTLC is created and the funds have been deducted from his account before notifying Alice.

>  Wait 2 Momentums for the transaction to be processed.

``` powershell
./znn-cli htlc.timeLocked -k Bob -n 321
./znn-cli balance -k Bob -n 321
```

Bob wants to know when Alice unlocks the HTLC containing the 1000 QSR.

``` powershell
./znn-cli htlc.monitor [hash id] -k Bob -n 321
```

Bob notifies Alice for her to inspect the HTLC.

``` powershell
./znn-cli htlc.hashLocked -k Alice -n 321
```

Alice inspects the HTLC and agrees to the conditions and unlocks the HTLC.

``` powershell
./znn-cli htlc.unlock [hash id] "all your znn belong to us" -k Alice -n 321
```

> Wait 2 Momentums for the transaction to be processed.

Alice has unlocked the 1000 QSR which the contract has send to her wallet. Alice needs to receive the unreceived transactions.

``` powershell
./znn-cli receiveAll -k Alice -n 321
```

Alice checks her balance to make sure everything is fine.

``` powershell
./znn-cli balance -k Alice -n 321
```

Meanwhile Bob gets notified of the pre-image Alice used to unlock Bob's HTLC and uses it to unlock Alice's HTLC.

``` powershell
./znn-cli htlc.unlock [hash id] "all your znn belong to us" -k Bob -n 321
```

> Wait 2 Momentums for the transaction to be processed.

Bob has unlocked the 100 ZNN which the contract has send to his wallet. Bob needs to receive the unreceived transactions.

``` powershell
./znn-cli receiveAll -k Bob -n 321
```

Bob checks his balance to make sure everything is fine.

``` powershell
./znn-cli balance -k Bob -n 321
```

## Clean up

Execute the following commands in order to undo all the installation files of this tutorial.

``` powershell
del $ENV:AppData\znn\wallet\Alice
del $ENV:AppData\znn\wallet\Bob
rm $ENV:USERPROFILE/repos -r -force
choco uninstall dotnet-sdk -y
choco uninstall mingw -y
choco uninstall go -y
choco uninstall git -y
```