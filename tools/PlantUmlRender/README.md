# PlantUmlRender

A .NET tool that renders PlantUML `.puml` files to PNG and SVG using [PlantUml.Net](https://www.nuget.org/packages/PlantUml.Net) (remote PlantUML server by default).

## Usage

```bash
# From repo root: render workflow files into docs/workflows
dotnet run --project tools/PlantUmlRender -- -o docs/workflows workflow.puml new-location.puml

# Options
#   -o, --output <dir>   Output directory (default: .)
#   -f, --formats <fmt>  svg, png, or both (default: both)
```

## Install as a global tool

After packing:

```bash
dotnet pack tools/PlantUmlRender -c Release -o nupkgs
dotnet tool install -g PlantUmlRender --add-source ./nupkgs
plantuml-render -o docs/workflows workflow.puml new-location.puml
```

## Integration

`scripts/Generate-Documentation.ps1` runs PlantUmlRender before DocFX so that `docs/workflows/workflow.svg` and `docs/workflows/new-location.svg` are generated when the PlantUML remote server is available. If the server returns an error (e.g. syntax or network), the workflow docs still build using the Mermaid and PlantUML source blocks.
