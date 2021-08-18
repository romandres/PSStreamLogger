# PSStreamLogger

This module allows you to use the built-in functionality of [PowerShell streams](https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_output_streams) to log data and output it into multiple log targets.

Use the PowerShell `Write-*` cmdlets to write into PowerShell streams in your scripts and modules like this:

```powershell
Write-Error "Error message"
Write-Warning "Warning message"
Write-Information -MessageData "Information message"
Write-Host "Information message" # Write-Host writes into the information stream
Write-Debug "Debug message"
Write-Verbose "Verbose message"
```

And use the PSStreamLogger to log this data into log files or other log targets.

Scripts and modules that already write into PowerShell streams do not have to be updated and can just be invoked through the PSStreamLogger.

## Installation

Download the latest module version and store it into a PowerShell module directory on the system you'll use it so you'll be able to import it.

### Which version to use?

* Standard (**recommended version**)  
The standard version of this module is based on .NET Standard 2.0 and will work on both PowerShell 7.x and Windows PowerShell 5.1. To use this version with Windows PowerShell 5.1 at least .NET Framework 4.7.2 must be installed.
* Full (Windows-only version based on the .NET Framework)  
This version of the module is based on .NET Framework 4.8 and will only work on Windows and Windows PowerShell 5.1. Only use this version if you need to log to the Windows EventLog using Windows PowerShell. Every other case, including logging to the Windows EventLog using PowerShell 7.x, works well with the standard version.

## Usage

Configure the log module and invoke a script inside a wrapper script:

```powershell
Import-Module PSStreamLogger

# Create plain text file logger
$fileLogger = New-FileLogger -FilePath "C:\temp\script1.log"

# Execute script with logger
Invoke-CommandWithLogging -ScriptBlock { & "C:\temp\script1.ps1" -Verbose -InformationAction Continue } -Loggers @($fileLogger)
```

You can also directly execute commands in the script block without calling an external script:

```powershell
...

# Execute commands with logger
Invoke-CommandWithLogging -ScriptBlock {
    Write-Verbose "Creating file" -Verbose
    New-Item -Path "C:\temp\file1.txt" -Type File
} -Loggers @($fileLogger)
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
