# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Nivra.DotNetAnalyzers** is a Roslyn-based C# code analyzer that enforces parameter formatting. It detects when method parameters or method arguments exceed 100 characters per line and provides an automated code fix to place each parameter/argument on its own line.

## Build & Test Commands

```bash
# Restore dependencies
dotnet restore

# Build all projects
dotnet build

# Run all tests
dotnet test

# Run tests in Release configuration
dotnet test --configuration Release

# Build VSIX (Visual Studio Extension)
msbuild .\Nivra.DotNetAnalyzers\Nivra.DotNetAnalyzers.Vsix -t:rebuild -p:Configuration=Release
```

## Project Structure

The solution consists of 5 main projects (all in `Nivra.DotNetAnalyzers/` subdirectory):

- **Nivra.DotNetAnalyzers** - Core analyzer that detects long parameter lines. Targets `netstandard2.0` using Microsoft.CodeAnalysis.CSharp
- **Nivra.DotNetAnalyzers.CodeFixes** - Code fix provider that reformats parameters/arguments. Targets `netstandard2.0`
- **Nivra.DotNetAnalyzers.Test** - MSTest unit tests. Targets `netcoreapp3.1`. Uses `CSharpCodeFixVerifier` pattern from Microsoft.CodeAnalysis testing framework
- **Nivra.DotNetAnalyzers.Vsix** - Visual Studio extension package
- **Nivra.DotNetAnalyzers.Package** - NuGet package metadata

Note: No .sln file exists; projects reference each other via .csproj dependencies.

## Architecture Patterns

### Analyzer Pattern

The analyzer (`NivraDotNetAnalyzersAnalyzer.cs`) follows standard Roslyn analyzer structure:

1. Inherits from `DiagnosticAnalyzer` and registers syntax node actions
2. Registers handlers for `SyntaxKind.MethodDeclaration` and `SyntaxKind.InvocationExpression`
3. Analyzes parameter/argument lists to detect lines > 100 characters
4. Reports diagnostics via `context.ReportDiagnostic()`
5. Uses localized resources for messages (via `Resources.resx`)

### Code Fix Pattern

The code fixer (`NivraDotNetAnalyzersCodeFixProvider.cs`):

1. Inherits from `CodeFixProvider` and exports with `[ExportCodeFixProvider]`
2. Declares which diagnostic IDs it fixes
3. In `RegisterCodeFixesAsync`, receives the diagnostic and registers a code action
4. The fix method traverses the syntax tree and reformats parameter lists with proper indentation
5. Returns a new document with the modified syntax tree

### Test Pattern

Tests (`NivraDotNetAnalyzersUnitTests.cs`) use the verifier pattern:

```csharp
var test = @"<code string>";
await VerifyCS.VerifyAnalyzerAsync(test);  // No diagnostics expected
await VerifyCS.VerifyCodeFixAsync(test, expected);  // Test fix
```

The `VerifyCS` alias wraps `CSharpCodeFixVerifier<Analyzer, CodeFix>` for type safety.

## Key Design Details

- **Diagnostic ID**: `"NivraDotNetAnalyzers"` - used to identify this analyzer's diagnostics
- **Line threshold**: 100 characters - configured in `AnalyzeParameters` method
- **Scope**: Both method definitions (parameters) and method calls (arguments)
- **Severity**: `DiagnosticSeverity.Info` - informational, not blocking
- **Indentation**: Code fix preserves leading trivia for proper formatting

## Building for Release

1. Tag commit with version (e.g., `git tag v1.0.0`)
2. Push tag to trigger GitHub Actions workflow
3. Workflow builds VSIX, runs tests, creates GitHub Release
4. VSIX is published as release asset
