# CLI History

This file contains archived content from CLI.md.


---
## Archive Entry: 2026-01-25 15:50:14
---

# CLI Agent

This file is monitored by the CLI Agent. Add commands below and the agent will execute them.

## Usage

Add commands in the format:
\`\`\`cli
COMMAND
\`\`\`

The agent will execute the command and append results below.

## Commands



## Prompts

## Results

### Command: `prompt code-review`
**Executed:** 2026-01-25 15:38:54

```
Prompt generated:

Errors:
At E:\github\FunWasHad\scripts\modules\FWH.Prompts\FWH.Prompts.psm1:1039 char:41
+                                         $inRelevantSection = if ([str ...
+                                         ~~~~~~~~~~~~~~~~~~
Expressions are only allowed as the first element of a pipeline.
At E:\github\FunWasHad\scripts\modules\FWH.Prompts\FWH.Prompts.psm1:1069 char:29
+                             }
+                             ~
Unexpected token '}' in expression or statement.
At E:\github\FunWasHad\scripts\modules\FWH.Prompts\FWH.Prompts.psm1:1070 char:25
+                         }
+                         ~
Unexpected token '}' in expression or statement.
At E:\github\FunWasHad\scripts\modules\FWH.Prompts\FWH.Prompts.psm1:1071 char:21
+                     }
+                     ~
Unexpected token '}' in expression or statement.
At E:\github\FunWasHad\scripts\modules\FWH.Prompts\FWH.Prompts.psm1:1072 char:17
+                 }
+                 ~
Unexpected token '}' in expression or statement.
At E:\github\FunWasHad\scripts\modules\FWH.Prompts\FWH.Prompts.psm1:1076 char:13
+             }
+             ~
Unexpected token '}' in expression or statement.
At E:\github\FunWasHad\scripts\modules\FWH.Prompts\FWH.Prompts.psm1:1077 char:9
+         }
+         ~
Unexpected token '}' in expression or statement.
At E:\github\FunWasHad\scripts\modules\FWH.Prompts\FWH.Prompts.psm1:1082 char:5
+     }
+     ~
Unexpected token '}' in expression or statement.
At E:\github\FunWasHad\scripts\modules\FWH.Prompts\FWH.Prompts.psm1:1086 char:1
+ }
+ ~
Unexpected token '}' in expression or statement.
    + CategoryInfo          : ParserError: (:) [], ParentContainsErrorRecordException
    + FullyQualifiedErrorId : ExpressionsMustBeFirstInPipeline
 
Import-Module : The module to process 'FWH.Prompts.psm1', listed in field 'ModuleToProcess/RootModule' of module 
manifest 'E:\github\FunWasHad\scripts\modules\FWH.Prompts\FWH.Prompts.psd1' was not processed because no valid module 
was found in any module directory.
At line:1 char:1
+ Import-Module 'E:\github\FunWasHad\scripts\modules\FWH.Prompts\FWH.Pr ...
+ ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    + CategoryInfo          : ResourceUnavailable: (FWH.Prompts:String) [Import-Module], PSInvalidOperationException
    + FullyQualifiedErrorId : Modules_ModuleFileNotFound,Microsoft.PowerShell.Commands.ImportModuleCommand
 
Get-Prompt : The term 'Get-Prompt' is not recognized as the name of a cmdlet, function, script file, or operable 
program. Check the spelling of the name, or if a path was included, verify that the path is correct and try again.
At line:1 char:90
+ ... ipts\modules\FWH.Prompts\FWH.Prompts.psd1' -Force; Get-Prompt -Name ' ...
+                                                        ~~~~~~~~~~
    + CategoryInfo          : ObjectNotFound: (Get-Prompt:String) [], CommandNotFoundException
    + FullyQualifiedErrorId : CommandNotFoundException
 


```

---
*Last updated: 2026-01-25 15:38:54*

---
*Last updated: 2026-01-25 15:38:52*


---
## Archive Entry: 2026-01-25 15:51:16
---

# CLI Agent

This file is monitored by the CLI Agent. Add commands below and the agent will execute them.

## Usage

Add commands in the format:


The agent will execute the command and append results below.

## Commands

## Prompts

## Results

### Command: `COMMAND`
**Executed:** 2026-01-25 15:50:15

```

Errors:
'COMMAND' is not recognized as an internal or external command,
operable program or batch file.

Exit Code: 1

```

---
*Last updated: 2026-01-25 15:50:15*

---
*Last updated: 2026-01-25 15:50:14*

---
## Archive Entry: 2026-01-25 16:54:59
---

# CLI Agent

This file is monitored by the CLI Agent. Add commands below and the agent will execute them.

## Usage

Add commands in the format:
\`\`\`cli
help
\`\`\`

The agent will execute the command and append results below.

## Commands



## Prompts

## Results

### Command: `prompt code-review`
**Executed:** 2026-01-25 16:34:48

```
Error: Cursor CLI (agent) not found. Install from https://cursor.com/install
Prompt generated for manual use:

---
Request a code review for a specific file or feature.

Please review the following code for all features in the current directory:
```

---
*Last updated: 2026-01-25 16:34:48*

---
*Last updated: 2026-01-25 16:33:36*


---
## Archive Entry: 2026-01-25 17:11:13
---

# CLI Agent

This file is monitored by the CLI Agent. Add commands below and the agent will execute them.

## Usage

Add commands in the format:
\`\`\`cli
help
\`\`\`

The agent will execute the command and append results below.

## Commands
```cli
prompt code-review
```


## Prompts

## Results

_Results will appear here after commands are executed._

### Prompt: code-review (2026-01-25 17:05:00)

```prompt
Request a code review for a specific file or feature.

Please review the following code for all features in the current directory:

```

```

Focus on:
- Code quality and best practices
- Potential bugs or issues
- Performance optimizations
- Security concerns
- Test coverage recommendations

Perform the same review for each of these models:
- ChatGPT (latest, most thorough)
- Claude Sonnet (latest, most thorough)
- Grok (latest, most thorough)

Aggregate the results into a single review and note which models each line item comes from.

Provide specific, actionable feedback.
```

---
*Last updated: 2026-01-25 17:05:00*


---
## Archive Entry: 2026-01-25 17:32:25
---

# CLI Agent

This file is monitored by the CLI Agent. Add commands below and the agent will execute them.

## Usage

Add commands in the format:
```cli
help
```

The agent will execute the command and append results below.

## Commands



## Prompts

## Results

_Results will appear here after commands are executed._

### Prompt: code-review (2026-01-25 17:23:52)

```prompt
Request a code review for a specific file or feature.

Please review the following code for all features in the current directory:

```

```

Focus on:
- Code quality and best practices
- Potential bugs or issues
- Performance optimizations
- Security concerns
- Test coverage recommendations

Perform the same review for each of these models:
- ChatGPT (latest, most thorough)
- Claude Sonnet (latest, most thorough)
- Grok (latest, most thorough)

Aggregate the results into a single review and note which models each line item comes from.

Provide specific, actionable feedback.
```

---
*Last updated: 2026-01-25 17:23:52*
