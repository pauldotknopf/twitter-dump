# About

Extract all tweets for a given query. This uses Twitters private API (via twitter.com) to bypass quotas/limits.

# Installation

```
dotnet tool install --global TwitterDump
```

# General

First, you must setup the authentication.

```
twitter-dump auth
```

After you have followed the instructions and succesfully authenticated, you can now dump a search to a JSON file.

```
twitter-dump search -q "(from:realDonaldTrump)" -o trump.json
```

# Installation in Ubuntu 20.04

To install on Unbuntu 20.04 do:
```
$ wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
$ sudo dpkg -i packages-microsoft-prod.deb
$ sudo apt-get update
$ sudo apt-get install -y apt-transport-https && sudo apt-get update
$ sudo apt-get install dotnet-sdk-3.1

$ export PATH="$PATH:$HOME/.dotnet/tools"

$ dotnet tool install --global TwitterDump
```
The software is now installed. You now need to get a token from a logged in session:

* Using the Chromium browser log in to Twitter and then navigate to: https://twitter.com/search
* Open Chromium developer tools (Ctrl-Shift I)
* Open the Network tab on the developer tools
* Filter requests for "adaptive.json"
* Search for anything in the web page (doesn't matter what)
* Scroll down until a network request for "adapative.json" is made
* Right click the request and click "Copy -> Copy as cURL"
* In a terminal run: perl -pe 's/\\\n//g' | twitter-dump auth
* Paste the contents of your clipboard
* Press Ctrl-D to close the input to 'perl'

Now you can run:
```
$ twitter-dump search -q "(from:GnuParallel)" -o gnuparallel.json
```

# License

MIT
