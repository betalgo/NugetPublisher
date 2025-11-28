![Betalgo NuGet Publisher Banner](.github/assets/banner.png)

# Betalgo NuGet Publisher

[![CI](https://github.com/Betalgo/NugetPublisher/actions/workflows/ci.yml/badge.svg)](https://github.com/Betalgo/NugetPublisher/actions/workflows/ci.yml)
[![GitHub release (latest by date)](https://img.shields.io/github/v/release/Betalgo/NugetPublisher)](https://github.com/Betalgo/NugetPublisher/releases)
[![License](https://img.shields.io/github/license/Betalgo/NugetPublisher)](LICENSE)

> **Stop writing complex PowerShell scripts for CI/CD.** Publish your NuGet packages with version auto-discovery, deterministic builds, and multi-feed support in seconds.

Composite GitHub Action that publishes .NET packages to NuGet.org (and optionally GitHub Packages) using a dedicated .NET console application executed through `dotnet run`. The action performs version discovery, deterministic packing, feed pushes, and optional git tagging without requiring PowerShell.

## Highlights

- Runs on .NET 8 LTS via `actions/setup-dotnet`.
- NuGet packages are cached by default via `actions/setup-dotnet` for faster restores.
- Automatically extracts `PackageId` and `Version` from your project or a custom file/regex.
- Skips publishing when the version already exists (configurable).
- Pushes to NuGet.org plus optional GitHub Packages, including symbol packages.
- Creates git tags safely (skips when the tag already exists).

## Usage

```yaml
name: Publish package

on:
  push:
    tags:
      - 'release/*'

jobs:
  nuget:
    runs-on: ubuntu-latest
    permissions:
      contents: write        # required for git tagging
      packages: write        # required when pushing to GitHub Packages
    steps:
      - uses: actions/checkout@v4

      - name: Publish via Betalgo-Nuget-Publisher
        uses: Betalgo/NugetPublisher@v1
        with:
          project-file: src/MyProject/MyProject.csproj
          nuget-api-key: ${{ secrets.NUGET_API_KEY }}
          publish-to-github-packages: true
```

## Inputs

| Name | Required | Default | Description |
| --- | --- | --- | --- |
| `project-file` | ✅ | — | Path to the `.csproj` that will be packed. |
| `package-name` | | derived from `<PackageId>` | Override for the NuGet package ID. |
| `version-file` | | project file | File inspected to extract the version. |
| `version-regex` | | — | Regex with a capture group for version extraction (skips XML parsing). |
| `version-regex-options` | | `Multiline` | `RegexOptions` flags separated by `\|`. |
| `version-static` | | — | Explicit version string (disables file parsing). |
| `tag-commit` | | `true` | Create/push a git tag using `tag-format`. |
| `tag-format` | | `v*` | Template for the git tag (`*` is replaced with the version). |
| `nuget-source` | | `https://api.nuget.org` | Base URL used to verify existing versions. |
| `nuget-push-source` | | `https://api.nuget.org/v3/index.json` | Feed passed to `dotnet nuget push`. |
| `nuget-api-key` | | — | API key/token for the primary NuGet feed. |
| `include-symbols` | | `false` | Generate and push symbol packages. |
| `configuration` | | `Release` | `dotnet pack` configuration. |
| `output-directory` | | `nupkg` next to project | Directory for generated packages. |
| `clean` | | `true` | Run `dotnet clean` before packing. |
| `publish-to-github-packages` | | `false` | Also push to GitHub Packages. |
| `github-packages-owner` | | `GITHUB_REPOSITORY_OWNER` | Owner used to build the GitHub feed URL. |
| `github-packages-source` | | derived | Fully qualified GitHub Packages feed URL. |
| `github-packages-api-key` | | `github.token` | Token with `packages:write` scope. |
| `github-packages-include-symbols` | | `false` | Include symbols when pushing to GitHub Packages. |
| `dotnet-version` | | `8.0.x` | SDK used by `actions/setup-dotnet`. |
| `git-user-name` | | `github-actions` | Git identity used for tagging. |
| `git-user-email` | | `actions@github.com` | Git email used for tagging. |
| `dry-run` | | `false` | Perform checks only (no pack/push). |
| `skip-when-version-exists` | | `true` | Stop early when the version already exists on the feed. |
| `extra-pack-arguments` | | — | Additional arguments appended to `dotnet pack`. |
| `no-restore` | | `false` | Skip the `dotnet restore` step (pass `--no-restore` to pack). |
| `continuous-integration-build` | | `true` | Set the `ContinuousIntegrationBuild` property to true. |

## Outputs

| Name | Description |
| --- | --- |
| `package-name` | Final package ID used while packing. |
| `version` | Version assigned to the package. |
| `should-publish` | `true` when the version was missing on the NuGet feed. |
| `published` | `true` when at least one feed received a push. |
| `package-path` | Full path to the generated `.nupkg`. |
| `symbols-package-path` | Full path to the generated `.snupkg` (when available). |
| `tag` | Git tag that was created and pushed. |
| `github-packages-source` | GitHub Packages feed URL that was used. |

## Permissions and Secrets

- Permissions:
  - `contents: write` when `tag-commit: true` to allow creating/pushing tags
  - `packages: write` when `publish-to-github-packages: true`
- Secrets:
  - `NUGET_API_KEY`: API key for NuGet.org
  - `GITHUB_TOKEN` (repo default) or a PAT with `packages:write` when pushing to GitHub Packages

## Requirements

- Run this action **after** `actions/checkout` so git metadata is available for tagging.
- Provide `NUGET_API_KEY` (and, if using GitHub Packages, `GITHUB_TOKEN`/PAT) via encrypted secrets.
- Ensure the workflow/job has required permissions (see above).

## Local Testing

You can exercise the publisher locally:

```bash
export PROJECT_FILE=src/MyProject/MyProject.csproj
export NUGET_API_KEY=dummy
dotnet run --project NugetPublisher/NugetPublisher.csproj
```

Set `DRY_RUN=true` to verify version discovery without touching feeds.
