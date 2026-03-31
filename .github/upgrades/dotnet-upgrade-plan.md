# .NET 9.0 Upgrade Plan

## Execution Steps

Execute steps below sequentially one by one in the order they are listed.

1. Validate that an .NET 9.0 SDK required for this upgrade is installed on the machine and if not, help to get it installed.
2. Ensure that the SDK version specified in global.json files is compatible with the .NET 9.0 upgrade.
3. Upgrade Jellyfin.Plugin.Template/Jellyfin.Plugin.Template.csproj.

## Settings

This section contains settings and data used by execution steps.

### Excluded projects

Table below contains projects that do belong to the dependency graph for selected projects and should not be included in the upgrade.

| Project name | Description |
|:-------------|:-----------:|

### Project upgrade details

This section contains details about each project upgrade and modifications that need to be done in the project.

#### Jellyfin.Plugin.Template/Jellyfin.Plugin.Template.csproj modifications

Project properties changes:
  - Target framework should be changed from `net6.0` to `net9.0`

Other changes:
  - No additional changes were identified by analysis.