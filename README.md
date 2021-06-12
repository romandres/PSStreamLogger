# PSStreamLogger

```powershell
Import-Module PSStreamLogger

# Create plain text file logger that will create a new log file every day or after it reaches 1GB in file size (parameter FileSizeLimit to set another limit)
$fileLogger = New-FileLogger -FilePath "C:\temp\script1.log" -RollOnFileSizeLimit -RollingInterval Day

# Execute script with logger
Invoke-CommandWithLogging -ScriptBlock { & "C:\temp\script1.ps1" -Verbose -InformationAction Continue } -Loggers @($fileLogger)
```

```powershell
powershell.exe -Command "& { Import-Module PSStreamLogger; Invoke-CommandWithLogging -ScriptBlock { & 'C:\temp\script1.ps1' -Verbose -InformationAction Continue } -Loggers @(New-FileLogger -FilePath 'C:\temp\script1.log' -RollOnFileSizeLimit -RollingInterval Day) }"
```

# Credits

* [Serilog](https://www.serilog.net)