# Setup Zenon Network devnet with HTLC branch and Zenon CLI for .NET on Ubuntu 22.04+

The instructions below are for setting up a Zenon Network **devnet** with the **HTLC** branch merged and Zenon CLI for .NET for Ubuntu 22.04+.

Click the link below to follow the instructions in a Video Tutorial made by 0x3639; otherwise continue reading the instructions.

[Video Tutorial: Zenon Network devnet with HTLC branch and Zenon CLI for .NET on Ubuntu 22.04](https://youtu.be/UC-oX1YjlJ0)

## Required software

### Git

We will need git to interact wth the GitHub repositories. Execute the following command in a Terminal.

``` bash
sudo apt install git
```

### Golang

We will need Golang to compile the go-zenon code. Execute the following command in a Terminal.

``` bash
sudo apt install golang-go
```

### .Net SDK

The .NET SDK allows you to develop apps with .NET. If you install the .NET SDK, you don’t need to install the corresponding runtime. To install the .NET SDK, run the following commands in the Terminal.

``` bash
sudo apt-get update && sudo apt-get install -y dotnet6
```

### Make

We will need Make to execute makefiles. Execute the following command in a Terminal.

``` bash
sudo apt install make
```

### GCC compiler

We need a gcc compiler to compile the go-zenon code. Check to make sure gcc is installed.

``` bash
gcc --version
```

You should see something like the following in Ubuntu 22.04

``` bash
gcc (Ubuntu 11.2.0-19ubuntu1) 11.2.0
Copyright (C) 2021 Free Software Foundation, Inc.
This is free software; see the source for copying conditions.  There is NO
warranty; not even for MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
```

If gcc is not installed run the following command

``` bash
sudo apt install gcc
```

## Configuration

After installing all the above tools make sure you close and open a new Bash Shell.

To use **git** we need to configure an user. Use you GitHub account if you have one; otherwise you can use anything you like.

``` bash
git config --global user.email [your e-mail]
git config --global user.name [your name]
```

## Compilation

We will make a **repos** directory under the current userprofile to store all our work. Replace the path if you want it stored on a different location.

``` bash 
cd ~/
mkdir repos
cd repos
```

### BICH go-zenon

Create a clone of the **devnet** branch of the [Big Inches Club House go-zenon repository](https://github.com/Big-Inches-Club-House/go-zenon.git).

``` bash
git clone -b devnet https://github.com/Big-Inches-Club-House/go-zenon.git
```

Change directory to the **go-zenon** directory.

``` bash
cd go-zenon
```

Merge the **htlc** branch with the **devnet** branch.

``` bash
git merge origin/htlc
```

Compile the **go-zenon** code.

``` bash
make znnd
```

Configure and run a **devnet** node.

``` bash
./build/znnd --data ./devnet generate-devnet --genesis-block=z1qqjnwjjpnue8xmmpanz6csze6tcmtzzdtfsww7,40000,400000
./build/znnd --data ./devnet
```

Keep the shell open during the duration of this tutorial. It is now possible to connect the **Zenon Explorer** to the node.

Open a web browser and go to https://explorer.zenon.network and connect the **Zenon Explorer** to http://127.0.0.1:35997

Search for the address **z1qqjnwjjpnue8xmmpanz6csze6tcmtzzdtfsww7**

> Try the Firefox or Brave browser if the Zenon Explorer does not want to connect. Google Chrome can throw a mixed content error when connecting to an insecure destination.

### Zenon CLI for .NET

Open a new Bash Shell and change directory to **repos**.

``` bash
cd ~/repos
```

Create a clone of the **htlc** branch of the [KingGorrin znn-cli-csharp repository](https://github.com/KingGorrin/znn_cli_csharp.git).

``` bash
git clone -b htlc https://github.com/KingGorrin/znn_cli_csharp.git
```

Change directory to the **znn_cli_csharp** directory.

``` bash
cd znn_cli_csharp
```

Compile the **znn_cli_csharp** code

``` bash
dotnet build --runtime linux-x64 src/ZenonCli/ZenonCli.csproj
```

Change directory to the binaries directory.

``` bash
cd ~/repos/znn_cli_csharp/bin/ZenonCli/net6.0/linux-x64
```