@{
	# Binary module file associated with this manifest
	RootModule = 'PSStreamLogger.psm1'

	GUID = '18b6925b-0c8f-4a14-abfa-c90550c025d7'
	ModuleVersion = '0.0.0'
	Author = 'romandres'
	Description = 'PowerShell module to leverage data written into PowerShell streams for logging purposes.'

	PrivateData = @{
		PSData = @{
			ProjectURI = 'https://github.com/romandres/PSStreamLogger'

			LicenseURI = 'https://github.com/romandres/PSStreamLogger/blob/main/LICENSE'

			Tags = @(
				'PSEdition_Desktop'
				'PSEdition_Core'
				'Windows'
			)
		}
	}

	CmdletsToExport = @(
		"Invoke-CommandWithLogging"
		"New-AzureApplicationInsightsLogger"
		"New-ConsoleLogger"
		"New-FileLogger"
		"New-EventLogLogger"
		"Out-PSStreamLogger"
	)
}
