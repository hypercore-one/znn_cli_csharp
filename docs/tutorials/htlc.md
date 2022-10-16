# Tutorial: Hashed Timelocked Contact (HTLC)

Some time ago George published his work on [BTC Atomic Swaps via HTLC: Cope Edition](https://github.com/Big-Inches-Club-House/bich/discussions/1). I've been wanting to look at the code and create the necessary tools to interact with the contract. This way others can try to understand how it works and what you can do with it.

Before we start I think it's important to understand what a **HTLC** is and what it is used for. Therefore I highly recommend that you watch this simple explainer [YouTube video](https://www.youtube.com/watch?v=hs79R8kd_70). The video explains two scenarios: the first scenario explains the use of a **HTLC** between two persons using BTC. The second scenario explains the use of a **HTLC** between two persons exchanging two different coins and thus effectively creating an Atomic Swap.

In this tutorial we're going to create the first scenario on our own devnet NoM network.

## Installation (Windows 10+)

To be able to interact with the **HTLC** we need to compile a modified node and tools and setup a **devnet**. Below are the instructions for setting up everything up on Windows.

### Chocolatey

We will use **Chocolatey** for installing the necessary dependencies. **Chocolatey** is a Packet Manager for Windows. Check out their website at https://chocolatey.org/ for more information.

Make sure **Chocolatey** is installed on your system by following the instructions at https://chocolatey.org/install#individual

After installing **Chocolatey**, ensure that you are using an [PowerShell administrative shell](https://www.howtogeek.com/742916/how-to-open-windows-powershell-as-an-admin-in-windows-10/).

### Git

We will need **git** to interact wth the GitHub repositories. Execute the following command in PowerShell.

```powershell
choco install git -y
```

### Go

We will need Go to to compile the go-zenon code. Execute the following command in PowerShell.

```powershell
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

We will make a **repos** directory on the **C** drive to store all our work. Replace the drive letter if you want to it on a different location.

``` powershell 
cd c:\
mkdir repos
cd repos
```

### BICH go-zenon

Create a clone of the **devnet** branch of the [Big Inches Club House go-zenon repository](git clone -b devnet]https://github.com/Big-Inches-Club-House/go-zenon.git).

``` powers
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
go build -o build\libznn.dll -buildmode=c-shared -tags libznn main_libznn.go
go build -o build\znnd.exe main.go
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
cd c:\repos
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
dotnet build .\src\ZenonCli.sln
```

Change directory to the binaries directory.

``` powershell
cd .\bin\ZenonCli\debug\net6.0\
```

## Setup  wallets

We will create two wallets for Alice and Bob using the csharp version of the znn-cli.

The primary address of Alice is **z1qqjnwjjpnue8xmmpanz6csze6tcmtzzdtfsww7**

The primary address of Bob is **z1qpsjv3wzzuuzdudg7tf6uhvr6sk4ag8me42ua4** 

Both wallets use the passphrase **secret**

> Notice that when we configured the devnet we used Alice's address as the genesis block and gave her some initial funding to play with.

Execute the following command to create Alice's wallet.

``` powershell
.\znn-cli.exe wallet.createFromMnemonic "route become dream access impulse price inform obtain engage ski believe awful absent pig thing vibrant possible exotic flee pepper marble rural fire fancy" secret Alice
```

Execute the following command to create Bob's wallet.

``` powershell
.\znn-cli.exe wallet.createFromMnemonic "alone emotion announce page spend eager middle lucky frame craft junk artefact upper finger drive corn version slot blade picnic festival wealth critic silver" secret Bob
```

### Generate plasma

In order to speed up the process of sending transactions on our devnet we will generate some plasma on both addresses.

Alice will fuse 100 QSR on both addresses using the **devnet** id 321.

``` powershell
.\znn-cli.exe plasma.fuse z1qqjnwjjpnue8xmmpanz6csze6tcmtzzdtfsww7 100 -k Alice -n 321
.\znn-cli.exe plasma.fuse z1qpsjv3wzzuuzdudg7tf6uhvr6sk4ag8me42ua4 100 -k Alice -n 321
```

## HTLC scenario 1

Alice wants to lock 100 ZNN for Bob with an expiration of 1 hour.

Alice will first check her balance to make sure she has enough funds.

``` powershell
.\znn-cli.exe balance -k Alice -n 321
```

Alice creates the HTLC using Bob's address and the preimage **all your znn belong to us**.

``` powershell
.\znn-cli.exe htlc.create z1qpsjv3wzzuuzdudg7tf6uhvr6sk4ag8me42ua4 ZNN 100 3600 0 32 "all your znn belong to us" -k Alice -n 321
```

Alice will make sure the HTLC is created and the funds have been deducted from her account before notifying Bob.

>  Wait 2 Momentums for the transaction to be processed.

``` powershell
.\znn-cli.exe htlc.timeLocked -k Alice -n 321
.\znn-cli.exe balance -k Alice -n 321
```

Alice will notify Bob for him to inspect the HTLC.

``` powershell
.\znn-cli.exe htlc.hashLocked -k Bob -n 321
```

Bob inspects the HTLC and agrees to the conditions and write down the HTLC hash id.

Bob has one hour the time to do his part of the deal. Once finished Alice will reveal the preimage to Bob so that he can unlock the 100 ZNN.

``` powershell
.\znn-cli.exe htlc.unlock [hash id] "all your znn belong to us" -k Bob -n 321
```

> Wait 2 Momentums for the transaction to be processed.

Bob has unlocked the 100 ZNN which the contract has send to his wallet. Bob now needs to receive the unreceived transactions.

``` powershell
.\znn-cli.exe receiveAll -k Bob -n 321
```

Bob is trilled and checks his balance to make sure everything is fine.

``` powershell
.\znn-cli.exe balance -k Bob -n 321
```

## Clean up

Execute the following commands in order to undo all the installation files of this tutorial.

``` powershell
del $ENV:AppData\znn\wallet\Alice
del $ENV:AppData\znn\wallet\Bob
rd /s /q "c:\repos"
choco uninstall dotnet-sdk -y
choco uninstall mingw -y
choco uninstall go -y
choco uninstall git -y
```