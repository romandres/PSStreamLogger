# PSStreamLogger

This module allows you to log the data of [PowerShell streams](https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_output_streams) into multiple log targets.

While you can use [stream redirection](https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_redirection) to redirect the streams messages into a plain text file for example, some information available in the streams are missing from that output.

The PSStreamLogger module enriches the log output with the data available from the streams.

## Installation

Download the latest module version and store it into a PowerShell module directory on the system you'll use it so you'll be able to import it.

## Usage

Configure the log module and invoke a script inside a wrapper script.

```powershell
Import-Module PSStreamLogger

# Create plain text file logger
$fileLogger = New-FileLogger -FilePath "C:\temp\script1.log"

# Execute script with logger
Invoke-CommandWithLogging -ScriptBlock { & "C:\temp\script1.ps1" -Verbose -InformationAction Continue } -Loggers @($fileLogger)
```

Configure the log module and invoke a script in one line in PowerShell:

```powershell
Import-Module PSStreamLogger; Invoke-CommandWithLogging -ScriptBlock { & 'C:\temp\script1.ps1' -Verbose -InformationAction Continue } -Loggers @(New-FileLogger -FilePath 'C:\temp\script1.log')
```

Configure the log module and invoke a script in one command-line command:

```bash
powershell.exe -Command "& { Import-Module PSStreamLogger; Invoke-CommandWithLogging -ScriptBlock { & 'C:\temp\script1.ps1' -Verbose -InformationAction Continue } -Loggers @(New-FileLogger -FilePath 'C:\temp\script1.log') }"
```

# Targets

This module allows you to log to:

* Console output
* Plain text files
* Windows EventLog (only on Windows)

# Credits

This module relies on [Serilog](https://www.serilog.net) and [Microsoft.Extensions.Logging](https://github.com/aspnet/Logging) to be able to log the PowerShell streams into multiple targets.
