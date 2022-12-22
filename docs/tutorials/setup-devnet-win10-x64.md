# Setup Zenon Network devnet with HTLC branch and Zenon CLI for .NET on Windows 10+

The instructions below are for setting up a Zenon Network **devnet** with the **HTLC** branch merged and Zenon CLI for .NET for Windows 10+.

## Required software

### Chocolatey

We will use **Chocolatey** for installing the necessary dependencies. **Chocolatey** is a Packet Manager for Windows. Check out their website at https://chocolatey.org/ for more information.

Make sure **Chocolatey** is installed on your system by following the instructions at https://chocolatey.org/install#individual

After installing **Chocolatey**, ensure that you are using an [PowerShell administrative shell](https://www.howtogeek.com/742916/how-to-open-windows-powershell-as-an-admin-in-windows-10/).

### Git

We will need git to interact wth the GitHub repositories. Execute the following command in PowerShell.

``` powershell
choco install git -y
```

### Golang

We will need Golang to to compile the go-zenon code. Execute the following command in PowerShell.

``` powershell
choco install go -y
```

### .Net SDK

We will need DotNet SDK to compile the znn-cli-cscharp code. Execute the following command in PowerShell.

``` powershell
choco install dotnet-sdk -y
```

### GCC compiler

We will need a GCC compiler to compile the go-zenon code. Execute the following command in PowerShell.

``` powershell
choco install winlibs-llvm-free
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

> Try the Firefox or Brave browser if the Zenon Explorer does not want to connect. Google Chrome can throw a mixed content error when connecting to an insecure destination.

### Zenon CLI for .NET

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