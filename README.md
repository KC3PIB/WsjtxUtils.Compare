# WsjtxUtils.Compare
Compare the decoded messages from two [WSJT-X](https://wsjt.sourceforge.io/wsjtx.html) clients operating in a 77-bit mode (FST4, FT4, FT8, MSK144, Q65). This application will correlate decoded messages from both clients by DECall, DXCall, QSO State, and Decode time to create the data needed to compare and characterize receiver and decoder behavior.

Matched decodes are written to the CSV file `correlated-decodes-ab.csv`. Any decode that is not correlated between clients and has been in the buffer for longer than the configurable parameter `AmountOfSecondsInBufferWithNoMatchIsUncorrelated` is written to the file `uncorrelated-decodes-a.csv` or `uncorrelated-decodes-b.csv` based on its source.

## Requirements
For pre-compiled [releases](https://github.com/KC3PIB/WsjtxUtils.Compare/releases):
- [.NET 7 Runtime](https://docs.microsoft.com/en-us/dotnet/core/install/)
    - Installation instruction for [Windows](https://docs.microsoft.com/en-us/dotnet/core/install/windows?tabs=net70), [Mac](https://docs.microsoft.com/en-us/dotnet/core/install/macos), [Linux](https://docs.microsoft.com/en-us/dotnet/core/install/linux)

To compile from source:
- [.NET 7 SDK](https://docs.microsoft.com/en-us/dotnet/core/install/)
    - Installation instruction for [Windows](https://docs.microsoft.com/en-us/dotnet/core/install/windows?tabs=net70), [Mac](https://docs.microsoft.com/en-us/dotnet/core/install/macos), [Linux](https://docs.microsoft.com/en-us/dotnet/core/install/linux)

## Configuration
Options can be configured by editing the [config.json](https://github.com/KC3PIB/WsjtxUtils.Compare/) file or by overriding specific parameters by command-line.
```json
{
  "PeriodicTimerSeconds": 1,
  "AmountOfSecondsInBufferWithNoMatchIsUncorrelated": 30.0,
  "IsCorrelatedDecodeWiggleTimeInSeconds" : 12,
  "CorrelatedDecodesFile": "correlated-decodes-ab.csv",
  "UncorrelatedDecodesFileSourceA": "uncorrelated-decodes-a.csv",
  "UncorrelatedDecodesFileSourceB": "uncorrelated-decodes-b.csv",
  "Server": {
    "Address": "127.0.0.1",
    "Port": 2237
  },
  "Serilog": {
    "MinimumLevel": "Information",
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "theme": "Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme::Code, Serilog.Sinks.Console",
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {NewLine}{Exception}"
        }
      }
    ]
  }
}
```
