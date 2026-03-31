# HDD Display Plugin Repository

Dieses Repository ist ein Jellyfin-Plugin-Repository mit automatischem Release-Flow, Manifest-Update und Changelog-Erzeugung.

Derzeit ist ein Plugin enthalten:

- `HDD Display`
- GUID: `eb5d7894-8eef-4b36-aa6f-5d124e828ce1`

## Was das Plugin macht

`HDD Display` zeigt auf der Plugin-Konfigurationsseite den Speicherplatz für gemountete Laufwerke an, die von Jellyfin-Bibliothekspfaden genutzt werden.

Die Ansicht enthält:

- Laufwerk/Mountpoint
- Volume Label
- Gesamtgröße
- Belegt
- Frei
- Auslastung in Prozent
- Zugeordnete Jellyfin-Library-Pfade

## Repository-Struktur

Wichtige Ordner und Dateien im Repo:

- `.github/workflows/`
  - `release-plugin.yml` (Build, Paketierung, Release, Manifest-Update)
  - weitere CI-Workflows (`build-dotnet.yml`, `test-dotnet.yml`, `codeql-analysis.yml`)
- `Jellyfin.Plugin.Template/`
  - `Plugin.cs` (Haupteinstieg des Plugins)
  - `Jellyfin.Plugin.Template.csproj` (Projektdatei, aktuell `net9.0`)
  - `Api/StorageUsageController.cs` (API für Speicherverbrauch)
  - `Configuration/PluginConfiguration.cs` (persistierte Plugin-Einstellungen)
  - `Configuration/configPage.html` (Jellyfin-Pluginseite)
- `manifest.json` (Plugin-Repository-Manifest)
- `PLUGIN_REPOSITORY.md` (Kurzinfo Multi-Plugin-Betrieb)

## Naming-Konvention pro Plugin

Wenn du mehrere Plugins in diesem Repo pflegen willst, nutze pro Plugin ein konsistentes Schema.

Empfohlene Namensfelder pro Plugin:

- `Plugin Name`: Anzeigename in Jellyfin (z. B. `HDD Display`)
- `Plugin Slug`: Dateisystem-/Paketname ohne Leerzeichen (z. B. `HDDDisplay`)
- `Project Namespace`: C#-Namespace (z. B. `Jellyfin.Plugin.HddDisplay`)
- `Plugin GUID`: eindeutige GUID

Empfohlene Datei-/Ordnernamen pro Plugin:

- Projektordner: `src/<PluginSlug>/` (oder bestehend unter eigenem Ordner)
- Projektdatei: `<PluginSlug>.csproj`
- Plugin-Klasse: `Plugin.cs`
- Konfiguration: `Configuration/PluginConfiguration.cs`
- Config-Seite: `Configuration/configPage.html`
- API-Controller: `Api/*Controller.cs`
- Release-ZIP: `<PluginSlug>_<Version>.zip`

Beispiel für `HDD Display`:

- Plugin Name: `HDD Display`
- Plugin Slug: `HDDDisplay`
- ZIP-Datei: `HDDDisplay_1.0.0.5.zip`

## Manifest (`manifest.json`)

`manifest.json` ist ein JSON-Array. Jeder Eintrag entspricht einem Plugin.

Pro Plugin enthält der Eintrag u. a.:

- `guid`
- `name`
- `description`
- `overview`
- `owner`
- `category`
- `versions` (Liste von Releases)

Pro Version werden gespeichert:

- `version`
- `changelog`
- `targetAbi`
- `sourceUrl`
- `checksum`
- `timestamp`

## Release-Automation

Der Workflow `.github/workflows/release-plugin.yml` läuft bei Tags `v*`.

Ablauf:

- Build und Publish des Plugins
- Erstellung des ZIP-Artefakts
- Berechnung der Checksum (Jellyfin-kompatibel)
- Generierung eines detaillierten Changelogs aus Git-Commits
- Upload von ZIP + `manifest.json` + `changelog.md` ins GitHub Release
- Update von `manifest.json` auf `master`

Damit werden automatisch gepflegt:

- Versionsnummer
- Download-URL
- Checksum
- Timestamp
- Changelog

## Detaillierte Changelogs

Der Changelog wird pro Release aus den Commit-Messages seit dem vorherigen Tag erzeugt.

Er landet an drei Stellen:

- als `body` im GitHub Release
- in `release-assets/changelog.md`
- in `manifest.json` im Feld `versions[].changelog`

## Jellyfin Repository-URL

In Jellyfin muss auf die direkte Manifest-URL gezeigt werden, nicht auf die GitHub-Repo-Hauptseite.

Nutze eine dieser URLs:

- `https://raw.githubusercontent.com/mleem97/marvin-jelly-2/master/manifest.json`
- `https://github.com/mleem97/marvin-jelly-2/releases/latest/download/manifest.json`

## Neues Plugin hinzufügen (Mehrfach-Repo)

Für ein weiteres Plugin:

- neuen Plugin-Eintrag in `manifest.json` anlegen (eigene GUID)
- eigenes Projekt mit eigenem `Plugin Slug` hinzufügen
- Workflow-Konfiguration pro Plugin setzen:
  - `PLUGIN_GUID`
  - `PLUGIN_NAME`
  - `PLUGIN_PACKAGE_PREFIX`
- Tag für Release erzeugen

Hinweis: Der Workflow aktualisiert gezielt den Manifest-Eintrag anhand der GUID und überschreibt keine anderen Plugin-Einträge.

## Lokale Entwicklung

Build lokal:

- `dotnet restore`
- `dotnet build -c Release`

Plugin-Ausgabe liegt im Build-/Publish-Output des jeweiligen Projekts.

## Lizenz

Siehe `LICENSE` im Repository.
