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

```ps
Install-Module PSStreamLogger
```

## Usage

Configure the log module and invoke a script inside a wrapper script:

```powershell
Import-Module PSStreamLogger

# Create plain text file logger
$fileLogger = New-FileLogger -FilePath "C:\temp\script1.log" -MinimumLogLevel Verbose

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
Import-Module PSStreamLogger; Invoke-CommandWithLogging -ScriptBlock { & 'C:\temp\script1.ps1' -Verbose -InformationAction Continue } -Loggers @(New-FileLogger -FilePath 'C:\temp\script1.log' -MinimumLogLevel Verbose)
```

Configure the log module and invoke a script in one command-line command:

```bash
powershell.exe -Command "& { Import-Module PSStreamLogger; Invoke-CommandWithLogging -ScriptBlock { & 'C:\temp\script1.ps1' -Verbose -InformationAction Continue } -Loggers @(New-FileLogger -FilePath 'C:\temp\script1.log' -MinimumLogLevel Verbose) }"
```

# Targets

This module allows you to log to:

* Console output
* Plain text files
* Windows EventLog (only on Windows)

# Credits

This module relies on [Serilog](https://www.serilog.net) and [Microsoft.Extensions.Logging](https://github.com/aspnet/Logging) to be able to log the PowerShell streams into multiple targets.
