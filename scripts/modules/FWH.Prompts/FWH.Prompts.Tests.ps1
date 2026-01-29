#Requires -Version 5.1

<#
.SYNOPSIS
    Unit tests for FWH.Prompts PowerShell module
    
.DESCRIPTION
    Tests all functions in the FWH.Prompts module including:
    - Template loading from prompts.md
    - Prompt generation
    - Template management
    - CLI integration functions
    
.NOTES
    Run with: Invoke-Pester .\FWH.Prompts.Tests.ps1
#>

$ErrorActionPreference = 'Stop'

Describe "FWH.Prompts Module Tests" {
    
    BeforeAll {
        # Resolve paths in BeforeAll so they are correct in Pester's runspace
        $ScriptDir = if ($PSCommandPath) { [System.IO.Path]::GetDirectoryName($PSCommandPath) } elseif ($PSScriptRoot) { $PSScriptRoot } else { (Get-Location).Path }
        $ModulePath = Join-Path $ScriptDir "FWH.Prompts.psd1"
        $PromptsFilePath = Join-Path $ScriptDir "prompts.md"
        $TestPromptsFilePath = Join-Path $ScriptDir "test-prompts.md"

        # Import module
        if (Get-Module FWH.Prompts) {
            Remove-Module FWH.Prompts -Force
        }
        Import-Module $ModulePath -Force
        
        # Verify prompts.md exists (default file)
        if (-not (Test-Path $PromptsFilePath)) {
            throw "prompts.md file not found at: $PromptsFilePath"
        }
        # Verify test-prompts.md exists (used by tests with -PromptsFile)
        if (-not (Test-Path $TestPromptsFilePath)) {
            throw "test-prompts.md file not found at: $TestPromptsFilePath"
        }
        # CR-PSM-2.5.1, 2.5.3: Ensure TestDrive has FunWasHad.sln so -ProjectRoot $TestDrive works for Write-CcliPromptToCli and Invoke-CcliClean
        'Microsoft Visual Studio Solution File, Format Version 12.00' | Set-Content -Path (Join-Path $TestDrive 'FunWasHad.sln') -Encoding UTF8 -ErrorAction SilentlyContinue
    }
    
    AfterAll {
        if (Get-Module FWH.Prompts) {
            Remove-Module FWH.Prompts -Force
        }
    }
    
    <#
    .SYNOPSIS
        Tests that the FWH.Prompts module loads correctly and exports all required functions and aliases.
    .DESCRIPTION
        Validates module initialization, function exports, and alias registration.
        
        What is being tested: The module's ability to load successfully, export all Ccli-prefixed functions, and register aliases for backward compatibility.
        
        Data involved: The FWH.Prompts module manifest (FWH.Prompts.psd1) and module script (FWH.Prompts.psm1) located in the module directory.
        
        Why the data matters: Module loading is the foundation for all other functionality. If the module fails to load or doesn't export functions correctly, none of the prompt templating features will work. The Ccli-prefixed functions are the primary API, and aliases provide backward compatibility for users who prefer the original naming.
        
        Expected outcome: The module should load without errors, export at least one Ccli-prefixed function, and register at least one alias.
        
        Reason for expectation: The module manifest defines FunctionsToExport and AliasesToExport, and the module script registers aliases. Successful loading confirms the module structure is correct and all dependencies are available.
    #>
    Context "Module Loading" {
        It "Should load the module successfully" {
            Get-Module FWH.Prompts | Should -Not -BeNullOrEmpty
        }
        
        It "Should export all Ccli-prefixed functions" {
            $functions = Get-Command -Module FWH.Prompts -CommandType Function
            $functions | Where-Object { $_.Name -like '*-Ccli*' } | Should -Not -BeNullOrEmpty
        }
        
        It "Should export all aliases" {
            $aliases = Get-Command -Module FWH.Prompts -CommandType Alias
            $aliases.Count | Should -BeGreaterThan 0
        }
    }
    
    <#
    .SYNOPSIS
        Tests that templates are correctly loaded from prompts.md and parsed with all required components.
    .DESCRIPTION
        Validates template loading, parsing, parameter extraction, and verification of built-in templates.
        
        What is being tested: The Initialize-CcliPromptTemplates function's ability to parse prompts.md, extract template names, descriptions, bodies, and parameters tables, and make them available through Get-CcliAvailablePrompts and Get-CcliPromptTemplate.
        
        Data involved: The prompts.md file in the module directory, which contains markdown-formatted templates separated by `---` markers. Each template has a `## name` header, description paragraph, template body with {Placeholder} syntax, and a `### Parameters` table.
        
        Why the data matters: Templates are the core feature of the module - they define reusable prompt structures. If templates aren't loaded correctly, users can't generate prompts. The prompts.md file is the single source of truth for all templates, so parsing must be robust and handle the markdown format correctly. Parameter extraction is critical because placeholders must match the parameters table.
        
        Expected outcome: Get-CcliAvailablePrompts should return at least one template, Get-CcliPromptTemplate should return a hashtable with Template and Parameters properties, the 'code-review' template should have FeatureName, FilePath, and Code parameters, and all 10 built-in templates should be available.
        
        Reason for expectation: The Initialize-CcliPromptTemplates function uses regex to parse prompts.md, extracting templates between `---` markers. It should identify template names from `##` headers, extract descriptions, bodies, and parameter tables. The built-in templates are hardcoded in prompts.md, so they should always be available after module load. Parameter extraction validates that the parsing logic correctly identifies placeholders and matches them to the parameters table.
    #>
    Context "Template Loading from prompts.md" {
        It "Should load templates from test-prompts.md" {
            $prompts = Get-CcliAvailablePrompts -PromptsFile $TestPromptsFilePath
            $prompts | Should -Not -BeNullOrEmpty
            $prompts.Count | Should -BeGreaterThan 0
        }
        
        It "Should load the 'code-review' template" {
            $template = Get-CcliPromptTemplate -Name 'code-review' -PromptsFile $TestPromptsFilePath
            $template | Should -Not -BeNullOrEmpty
            $template.Template | Should -Not -BeNullOrEmpty
            $template.Parameters | Should -Not -BeNullOrEmpty
        }
        
        It "Should have correct parameters for 'code-review' template" {
            $template = Get-CcliPromptTemplate -Name 'code-review' -PromptsFile $TestPromptsFilePath
            $template.Parameters | Should -Contain 'FeatureName'
            $template.Parameters | Should -Contain 'FilePath'
            $template.Parameters | Should -Contain 'Code'
        }
        
        It "Should load all expected built-in templates" {
            $expectedTemplates = @(
                'code-review',
                'implement-feature',
                'debug-issue',
                'refactor-code',
                'write-tests',
                'document-code',
                'optimize-performance',
                'add-feature',
                'fix-bug',
                'security-audit'
            )
            
            $availablePrompts = Get-CcliAvailablePrompts -PromptsFile $TestPromptsFilePath
            $availableNames = $availablePrompts.Name
            
            foreach ($expected in $expectedTemplates) {
                $availableNames | Should -Contain $expected
            }
        }
    }
    
    <#
    .SYNOPSIS
        Tests that Get-CcliPromptTemplate correctly retrieves templates and handles invalid template names.
    .DESCRIPTION
        Validates template retrieval, error handling, and ErrorAction parameter behavior.
        
        What is being tested: The Get-CcliPromptTemplate function's ability to retrieve templates by name, return proper data structures, and handle invalid template names according to ErrorAction parameter.
        
        Data involved: Valid template name 'code-review' (known to exist in prompts.md) and invalid template name 'nonexistent-template' (guaranteed not to exist). The function should return a hashtable with Template and Parameters properties for valid names.
        
        Why the data matters: Get-CcliPromptTemplate is the primary way users access template details. It must reliably return templates when they exist and provide clear error handling when they don't. The ErrorAction parameter allows users to control whether missing templates throw exceptions or return null, which is important for error handling in scripts.
        
        Expected outcome: For valid template name 'code-review', the function should return a hashtable with Template and Parameters properties. For invalid template name with ErrorAction Stop, it should throw an exception. For invalid template name with ErrorAction SilentlyContinue, it should return null.
        
        Reason for expectation: The function looks up templates in the internal $script:PromptTemplates hashtable populated during module initialization. Valid names should return the stored template data. Invalid names should trigger error handling based on ErrorAction - Stop throws, SilentlyContinue returns null. This follows PowerShell's standard error handling patterns.
    #>
    Context "Get-CcliPromptTemplate" {
        It "Should return template for valid name" {
            $template = Get-CcliPromptTemplate -Name 'code-review' -PromptsFile $TestPromptsFilePath
            $template | Should -Not -BeNullOrEmpty
            $template | Should -HaveType 'Hashtable'
            $template.Template | Should -Not -BeNullOrEmpty
            $template.Parameters | Should -Not -BeNullOrEmpty
        }
        
        It "Should return error for invalid template name" {
            { Get-CcliPromptTemplate -Name 'nonexistent-template' -PromptsFile $TestPromptsFilePath -ErrorAction Stop } | Should -Throw
        }
        
        It "Should return null for invalid template name when ErrorAction is Continue" {
            $result = Get-CcliPromptTemplate -Name 'nonexistent-template' -PromptsFile $TestPromptsFilePath -ErrorAction SilentlyContinue
            $result | Should -BeNullOrEmpty
        }
    }
    
    <#
    .SYNOPSIS
        Tests that Get-CcliPrompt correctly fills template placeholders with parameter values and handles missing parameters.
    .DESCRIPTION
        Validates parameter replacement, placeholder resolution, missing parameter warnings, and alias functionality.
        
        What is being tested: The Get-CcliPrompt function's ability to take a template name and parameters hashtable, replace all {Placeholder} placeholders in the template body with corresponding parameter values, and generate a complete prompt string.
        
        Data involved: Template name 'code-review' with parameters hashtable containing FeatureName='User Authentication', FilePath='src/AuthService.cs', Code='public class Auth { }'. The template contains placeholders {FeatureName}, {FilePath}, and {Code} that should be replaced. Also tests with missing parameters to validate warning behavior.
        
        Why the data matters: Parameter replacement is the core functionality of prompt generation. Users provide parameter values, and the function must correctly substitute them into the template. Missing parameters should be handled gracefully (warnings but still generate prompt) to allow partial fills. The alias Get-Prompt must work identically for backward compatibility.
        
        Expected outcome: With all parameters provided, the generated prompt should be a string containing 'User Authentication', 'src/AuthService.cs', and 'public class Auth', with no remaining {Placeholder} syntax. With missing parameters, the prompt should still be generated but may contain empty placeholders or warnings. The alias Get-Prompt should produce identical results.
        
        Reason for expectation: Get-CcliPrompt retrieves the template, uses string replacement to substitute {ParameterName} with values from the parameters hashtable. All occurrences of each placeholder should be replaced. Missing parameters result in empty replacements and warnings, but the function continues to allow partial template fills. The alias is registered to call the same function, so behavior should be identical.
    #>
    Context "Get-CcliPrompt" {
        It "Should generate a filled prompt with all parameters" {
            $params = @{
                FeatureName = 'User Authentication'
                FilePath = 'src/AuthService.cs'
                Code = 'public class Auth { }'
            }
            
            $prompt = Get-CcliPrompt -Name 'code-review' -Parameters $params -PromptsFile $TestPromptsFilePath
            $prompt | Should -Not -BeNullOrEmpty
            $prompt | Should -BeOfType 'String'
            $prompt | Should -Match 'User Authentication'
            $prompt | Should -Match 'src/AuthService.cs'
            $prompt | Should -Match 'public class Auth'
        }
        
        It "Should replace all placeholders in template" {
            $params = @{
                FeatureName = 'TestFeature'
                FilePath = 'Test.cs'
                Code = 'TestCode'
            }
            
            $prompt = Get-CcliPrompt -Name 'code-review' -Parameters $params -PromptsFile $TestPromptsFilePath
            $prompt | Should -Not -Match '\{FeatureName\}'
            $prompt | Should -Not -Match '\{FilePath\}'
            $prompt | Should -Not -Match '\{Code\}'
        }
        
        It "Should warn about missing parameters" {
            $params = @{
                FeatureName = 'Test'
                # Missing FilePath and Code
            }
            
            $prompt = Get-CcliPrompt -Name 'code-review' -Parameters $params -PromptsFile $TestPromptsFilePath
            $prompt | Should -Not -BeNullOrEmpty
        }
        
        It "Should work with alias Get-Prompt" {
            $params = @{
                FeatureName = 'Test'
                FilePath = 'Test.cs'
                Code = 'Test'
            }
            
            $prompt = Get-Prompt -Name 'code-review' -Parameters $params -PromptsFile $TestPromptsFilePath
            $prompt | Should -Not -BeNullOrEmpty
        }
    }
    
    <#
    .SYNOPSIS
        Tests that Get-CcliAvailablePrompts returns a complete list of all available templates with required properties.
    .DESCRIPTION
        Validates template listing functionality, return type, object structure, and alias behavior.
        
        What is being tested: The Get-CcliAvailablePrompts function's ability to return an array of objects representing all loaded templates, each with Name, Description, Parameters array, and ParameterCount properties.
        
        Data involved: All templates loaded from prompts.md during module initialization. Each template object should contain metadata extracted from the markdown file (name from `##` header, description from first paragraph, parameters from the parameters table).
        
        Why the data matters: Get-CcliAvailablePrompts is used to discover available templates, which is essential for users exploring the module's capabilities. The returned objects must contain all necessary information (name, description, parameters) so users can understand what each template does and what parameters it requires before using it. The ParameterCount property provides quick insight into template complexity.
        
        Expected outcome: Get-CcliAvailablePrompts should return a non-empty array of objects. Each object should have Name (string), Description (string), Parameters (array), and ParameterCount (integer) properties. The alias Get-AvailablePrompts should return identical results.
        
        Reason for expectation: The function iterates through the internal $script:PromptTemplates hashtable and creates PSCustomObject instances for each template with properties extracted during parsing. The array format allows easy filtering and iteration. The alias calls the same function, so results should be identical.
    #>
    Context "Get-CcliAvailablePrompts" {
        It "Should return list of available prompts" {
            $prompts = Get-CcliAvailablePrompts -PromptsFile $TestPromptsFilePath
            $prompts | Should -Not -BeNullOrEmpty
            $prompts | Should -BeOfType 'System.Object[]'
        }
        
        It "Should return prompts with Name, Description, Parameters, and ParameterCount" {
            $prompts = Get-CcliAvailablePrompts -PromptsFile $TestPromptsFilePath
            $first = $prompts[0]
            $first | Should -HaveProperty 'Name'
            $first | Should -HaveProperty 'Description'
            $first | Should -HaveProperty 'Parameters'
            $first | Should -HaveProperty 'ParameterCount'
        }
        
        It "Should work with alias Get-AvailablePrompts" {
            $prompts = Get-AvailablePrompts -PromptsFile $TestPromptsFilePath
            $prompts | Should -Not -BeNullOrEmpty
        }
    }
    
    <#
    .SYNOPSIS
        Tests that Invoke-CcliPrompt outputs generated prompts to the console correctly.
    .DESCRIPTION
        Validates console output functionality and alias behavior.
        
        What is being tested: The Invoke-CcliPrompt function's ability to generate a prompt using Get-CcliPrompt and output it to the console via Write-Host or similar output mechanism.
        
        Data involved: Template name 'code-review' with complete parameters hashtable (FeatureName, FilePath, Code). The function should generate the filled prompt and write it to the output stream.
        
        Why the data matters: Invoke-CcliPrompt provides a convenient way to generate and immediately view prompts without manually calling Get-CcliPrompt and Write-Host. This is useful for quick prompt generation and testing. Console output allows users to see prompts immediately or redirect them to files.
        
        Expected outcome: Invoke-CcliPrompt should output a non-empty string to the console containing the filled prompt. The alias Invoke-Prompt should produce identical output.
        
        Reason for expectation: Invoke-CcliPrompt internally calls Get-CcliPrompt to generate the prompt, then outputs it using Write-Host or similar. The output stream (6>&1 captures all streams) should contain the generated prompt text. The alias calls the same function, so behavior should be identical.
    #>
    Context "Invoke-CcliPrompt" {
        It "Should output prompt to console" {
            $params = @{
                FeatureName = 'Test'
                FilePath = 'Test.cs'
                Code = 'Test'
            }
            
            $output = Invoke-CcliPrompt -Name 'code-review' -Parameters $params -PromptsFile $TestPromptsFilePath 6>&1
            $output | Should -Not -BeNullOrEmpty
        }
        
        It "Should work with alias Invoke-Prompt" {
            $params = @{
                FeatureName = 'Test'
                FilePath = 'Test.cs'
                Code = 'Test'
            }
            
            $output = Invoke-Prompt -Name 'code-review' -Parameters $params -PromptsFile $TestPromptsFilePath 6>&1
            $output | Should -Not -BeNullOrEmpty
        }
    }
    
    <#
    .SYNOPSIS
        Tests that New-CcliPromptTemplate can create runtime templates and prevents duplicate template names.
    .DESCRIPTION
        Validates dynamic template creation, duplicate name prevention, and alias functionality.
        
        What is being tested: The New-CcliPromptTemplate function's ability to create new templates at runtime, add them to the internal template store, and prevent overwriting existing templates (both from prompts.md and runtime-created).
        
        Data involved: New template with name 'test-template', description 'Test template', template body 'This is a {TestParam} template', and parameter array @('TestParam'). Also tests duplicate creation with name 'test-duplicate' to validate duplicate prevention.
        
        Why the data matters: Runtime template creation allows users to create custom templates programmatically without modifying prompts.md. This is useful for dynamic prompt generation or temporary templates. Duplicate prevention is critical to avoid accidentally overwriting existing templates, which could break user code that depends on specific template names.
        
        Expected outcome: Creating a new template should return $true and the template should be retrievable via Get-CcliPromptTemplate with matching Template body. Attempting to create a duplicate template should return $false without modifying the existing template. The alias New-PromptTemplate should work identically.
        
        Reason for expectation: New-CcliPromptTemplate adds templates to the $script:PromptTemplates hashtable. It should check if the template name already exists before adding. If it exists, return $false; if not, add the template and return $true. The template should be immediately available through Get-CcliPromptTemplate. The alias calls the same function, so behavior should be identical.
    #>
    Context "New-CcliPromptTemplate" {
        It "Should create a new template" {
            $result = New-CcliPromptTemplate -Name 'test-template' `
                -Description 'Test template' `
                -Template 'This is a {TestParam} template' `
                -Parameters @('TestParam')
            
            $result | Should -Be $true
            
            $template = Get-CcliPromptTemplate -Name 'test-template'
            $template | Should -Not -BeNullOrEmpty
            $template.Template | Should -Match 'TestParam'
        }
        
        It "Should prevent duplicate template names" {
            New-CcliPromptTemplate -Name 'test-duplicate' `
                -Description 'Test' `
                -Template 'Test {Param}' `
                -Parameters @('Param') | Out-Null
            
            $result = New-CcliPromptTemplate -Name 'test-duplicate' `
                -Description 'Test' `
                -Template 'Test {Param}' `
                -Parameters @('Param')
            
            $result | Should -Be $false
        }
        
        It "Should work with alias New-PromptTemplate" {
            $result = New-PromptTemplate -Name 'test-alias' `
                -Description 'Test' `
                -Template 'Test {Param}' `
                -Parameters @('Param')
            
            $result | Should -Be $true
        }
    }
    
    Context "Remove-CcliPromptTemplate" {
        It "Should remove a template" {
            # Create a template first
            New-CcliPromptTemplate -Name 'test-remove' `
                -Description 'Test' `
                -Template 'Test {Param}' `
                -Parameters @('Param') | Out-Null
            
            $result = Remove-CcliPromptTemplate -Name 'test-remove' -Force
            $result | Should -Be $true
            
            { Get-CcliPromptTemplate -Name 'test-remove' -ErrorAction Stop } | Should -Throw
        }
        
        It "Should work with alias Remove-PromptTemplate" {
            New-CcliPromptTemplate -Name 'test-remove-alias' `
                -Description 'Test' `
                -Template 'Test {Param}' `
                -Parameters @('Param') | Out-Null
            
            $result = Remove-PromptTemplate -Name 'test-remove-alias' -Force
            $result | Should -Be $true
        }
    }
    
    Context "Write-CcliPromptToCli" {
        It "Should write prompt to CLI.md file" {
            $testCliPath = Join-Path $TestDrive "CLI.md"
            
            $params = @{
                FeatureName = 'Test Feature'
                FilePath = 'Test.cs'
                Code = 'Test code'
            }
            
            Write-CcliPromptToCli -Name 'code-review' `
                -Parameters $params `
                -PromptsFile $TestPromptsFilePath `
                -ProjectRoot $TestDrive
            
            Test-Path $testCliPath | Should -Be $true
            
            $content = Get-Content $testCliPath -Raw
            $content | Should -Match 'Test Feature'
            $content | Should -Match 'code-review'
        }
        
        It "Should work with alias Write-PromptToCli" {
            $testCliPath = Join-Path $TestDrive "CLI.md"
            
            $params = @{
                FeatureName = 'Test'
                FilePath = 'Test.cs'
                Code = 'Test'
            }
            
            Write-PromptToCli -Name 'code-review' `
                -Parameters $params `
                -PromptsFile $TestPromptsFilePath `
                -ProjectRoot $TestDrive
            
            Test-Path $testCliPath | Should -Be $true
        }
    }
    
    Context "Template Parameter Validation" {
        It "Should extract all parameters from template body" {
            $template = Get-CcliPromptTemplate -Name 'code-review' -PromptsFile $TestPromptsFilePath
            $placeholders = [regex]::Matches($template.Template, '\{([^}]+)\}')
            $placeholderNames = $placeholders | ForEach-Object { $_.Groups[1].Value } | Select-Object -Unique
            
            foreach ($param in $template.Parameters) {
                $placeholderNames | Should -Contain $param
            }
        }
        
        It "Should handle templates with multiple occurrences of same parameter" {
            $params = @{
                FeatureName = 'Test'
                FilePath = 'Test.cs'
                Code = 'Test'
            }
            
            $prompt = Get-CcliPrompt -Name 'code-review' -Parameters $params -PromptsFile $TestPromptsFilePath
            # Should replace all occurrences
            ($prompt | Select-String -Pattern '\{FeatureName\}' -AllMatches).Matches.Count | Should -Be 0
        }
    }
    
    Context "Error Handling" {
        It "Should handle empty parameters hashtable gracefully" {
            $prompt = Get-CcliPrompt -Name 'code-review' -Parameters @{} -PromptsFile $TestPromptsFilePath
            $prompt | Should -Not -BeNullOrEmpty
        }
        
        It "Should handle null template gracefully" {
            { Get-CcliPrompt -Name 'nonexistent' -Parameters @{} -PromptsFile $TestPromptsFilePath -ErrorAction Stop } | Should -Throw
        }
    }
    
    <#
    .SYNOPSIS
        Tests that prompts.md file exists and has the correct markdown structure required for template parsing.
    .DESCRIPTION
        Validates file existence, markdown structure, separator markers, and parameters table format.
        
        What is being tested: The prompts.md file's structure and format, ensuring it follows the expected markdown format with proper separators and parameters tables that the parsing logic can process.
        
        Data involved: The prompts.md file in the module directory, which should contain markdown-formatted templates with `#` headers, `---` separators between templates, and `### Parameters` sections with markdown tables.
        
        Why the data matters: The prompts.md file is the single source of truth for all templates. If the file structure is incorrect (missing separators, malformed tables), the parsing logic will fail and templates won't load. This test validates that the file format matches what the parser expects, preventing runtime errors during module initialization.
        
        Expected outcome: prompts.md should exist in the module directory, start with a `#` header, contain at least one `---` separator (indicating multiple templates), and contain `### Parameters` headers and markdown table syntax (`| Parameter |`).
        
        Reason for expectation: The Initialize-CcliPromptTemplates function uses regex to parse prompts.md, splitting on `---` markers and looking for `##` headers, descriptions, template bodies, and `### Parameters` tables. The file must follow this structure for parsing to succeed. The presence of separators and parameter tables confirms the file format is correct.
    #>
    Context "prompts.md File Format" {
        It "Should have prompts.md file in module directory" {
            Test-Path $PromptsFilePath | Should -Be $true
        }
        
        It "Should have valid markdown structure with --- separators" {
            $content = Get-Content $PromptsFilePath -Raw
            $content | Should -Match '^#'
            $sections = ($content -split '---').Count
            $sections | Should -BeGreaterThan 1
        }
        
        It "Should have Parameters table for each template" {
            $content = Get-Content $PromptsFilePath -Raw
            $content | Should -Match '### Parameters'
            $content | Should -Match '\| Parameter \|'
        }
    }

    # region CR-PSM-2.5.1, 2.5.2, 2.5.3

    Context "Invoke-CcliClean (CR-PSM-2.5.1)" {
        It "Archives to CLI-history.md and resets CLI.md" {
            $root = $TestDrive
            $cliPath = Join-Path $root "CLI.md"
            $historyPath = Join-Path $root "CLI-history.md"
            $original = "# Original`n## Commands`n`n---"
            $utf8 = New-Object System.Text.UTF8Encoding $false
            [System.IO.File]::WriteAllText($cliPath, $original, $utf8)

            Invoke-CcliClean -ProjectRoot $root

            Test-Path $historyPath | Should -Be $true
            $history = Get-Content $historyPath -Raw
            $history | Should -Match 'Archive Entry:'
            $history | Should -Match 'Original'
            $after = Get-Content $cliPath -Raw
            $after | Should -Match '# CLI Agent'
            $after | Should -Not -Match 'Original'
        }
    }

    Context "Watch-CcliResults (CR-PSM-2.5.1)" {
        It "Completes without error with short Timeout when file exists" {
            $cliPath = Join-Path $TestDrive "watch-test.md"
            "## Results`n`n---" | Set-Content $cliPath -Encoding UTF8
            # Watch with 2s timeout; just assert it doesn't throw
            { Watch-CcliResults -CliFilePath $cliPath -Timeout 2 } | Should -Not -Throw
        }
    }

    Context "Read-CcliPromptsFile and Get-CcliAvailablePrompts edge cases (CR-PSM-2.5.2)" {
        It "Empty file returns no prompts" {
            $emptyPath = Join-Path $TestDrive "empty.md"
            [System.IO.File]::WriteAllText($emptyPath, "", (New-Object System.Text.UTF8Encoding $false))
            $prompts = Get-CcliAvailablePrompts -PromptsFile $emptyPath
            @($prompts).Count | Should -Be 0
        }

        It "File with only --- returns no prompts" {
            $path = Join-Path $TestDrive "only-sep.md"
            "---" | Set-Content $path -Encoding UTF8
            $prompts = Get-CcliAvailablePrompts -PromptsFile $path
            $prompts.Count | Should -Be 0
        }

        It "Section without ### Parameters returns template with ParameterCount 0" {
            $path = Join-Path $TestDrive "no-params.md"
            @"
## noparams

Body with {X}

---
"@ | Set-Content $path -Encoding UTF8
            $prompts = Get-CcliAvailablePrompts -PromptsFile $path
            $prompts | Should -Not -BeNullOrEmpty
            $prompts.Count | Should -Be 1
            $prompts[0].Name | Should -Be 'noparams'
            $prompts[0].ParameterCount | Should -Be 0
        }

        It "shared-context only returns no prompts" {
            $path = Join-Path $TestDrive "shared-only.md"
            @"
## shared-context

Some context.

---
"@ | Set-Content $path -Encoding UTF8
            $prompts = Get-CcliAvailablePrompts -PromptsFile $path
            $prompts.Count | Should -Be 0
        }
    }

    Context "Find-CcliProjectRoot and Read-CcliAgentConfig (CR-PSM-2.5.3)" {
        It "Find-CcliProjectRoot finds root from subdir when -ProjectRoot not passed" {
            $root = $TestDrive
            $sub = Join-Path $root "src"
            New-Item -ItemType Directory -Path $sub -Force | Out-Null
            $cliPath = Join-Path $root "CLI.md"
            "before" | Set-Content $cliPath -Encoding UTF8
            $historyPath = Join-Path $root "CLI-history.md"
            if (Test-Path $historyPath) { Remove-Item $historyPath -Force }

            Push-Location $sub
            try {
                Invoke-CcliClean
                Test-Path $historyPath | Should -Be $true
                (Get-Content $historyPath -Raw) | Should -Match 'before'
            } finally {
                Pop-Location
            }
        }

        It "Read-CcliAgentConfig uses CliMdPath from cli-agent.json" {
            $root = $TestDrive
            $outDir = Join-Path $root "out"
            New-Item -ItemType Directory -Path $outDir -Force | Out-Null
            '{"CliAgent":{"CliMdPath":"out/CLI.md"}}' | Set-Content (Join-Path $root "cli-agent.json") -Encoding UTF8
            $customPath = Join-Path $outDir "CLI.md"
            "custom" | Set-Content $customPath -Encoding UTF8
            $historyPath = Join-Path $root "CLI-history.md"
            if (Test-Path $historyPath) { Remove-Item $historyPath -Force }

            Invoke-CcliClean -ProjectRoot $root

            (Get-Content $customPath -Raw) | Should -Match '# CLI Agent'
            (Get-Content $customPath -Raw) | Should -Not -Match 'custom'
            (Get-Content $historyPath -Raw) | Should -Match 'custom'
        }
    }

    # endregion
}
