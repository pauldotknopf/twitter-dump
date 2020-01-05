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

# License

MIT