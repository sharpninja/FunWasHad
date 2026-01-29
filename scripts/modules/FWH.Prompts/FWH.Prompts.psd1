@{
    # Script module or binary module file associated with this manifest
    RootModule = 'FWH.Prompts.psm1'
    
    # Version number of this module
    ModuleVersion = '1.0.5'
    
    # ID used to uniquely identify this module
    GUID = 'a1b2c3d4-e5f6-7890-abcd-ef1234567890'
    
    # Author of this module
    Author = 'FunWasHad Development Team'
    
    # Company or vendor of this module
    CompanyName = 'FunWasHad'
    
    # Copyright statement for this module
    Copyright = '(c) 2025 FunWasHad. All rights reserved.'
    
    # Description of the functionality provided by this module
    Description = 'PowerShell module providing templatized prompts for AI interactions with parameterized commands.'
    
    # Minimum version of the PowerShell engine required by this module
    PowerShellVersion = '5.1'
    
    # Functions to export from this module
    FunctionsToExport = @(
        'Get-CcliPrompt',
        'Invoke-CcliPrompt',
        'Get-CcliAvailablePrompts',
        'Get-CcliHelp',
        'Invoke-CcliClean',
        'New-CcliPromptTemplate',
        'Remove-CcliPromptTemplate',
        'Get-CcliPromptTemplate',
        'Write-CcliPromptToCli',
        'Watch-CcliResults'
    )
    
    # Cmdlets to export from this module
    CmdletsToExport = @()
    
    # Variables to export from this module
    VariablesToExport = @()
    
    # Aliases to export from this module
    AliasesToExport = @(
        'Get-Prompt',
        'Invoke-Prompt',
        'Get-AvailablePrompts',
        'Get-CliHelp',
        'clean-cli',
        'New-PromptTemplate',
        'Remove-PromptTemplate',
        'Get-PromptTemplate',
        'Write-PromptToCli',
        'Watch-CliResults'
    )
    
    # Private data to pass to the module
    PrivateData = @{
        PSData = @{
            Tags = @('prompts', 'ai', 'templates', 'llm')
            LicenseUri = ''
            ProjectUri = 'https://github.com/sharpninja/FunWasHad'
            ReleaseNotes = 'Initial release of FWH.Prompts module'
        }
    }
}
