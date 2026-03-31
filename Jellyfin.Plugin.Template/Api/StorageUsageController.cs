using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.Template.Api;

/// <summary>
/// Exposes storage usage information for drives used by Jellyfin libraries.
/// </summary>
[ApiController]
[Route("Plugins/HddDisplay/Storage")]
public class StorageUsageController : ControllerBase
{
    private readonly ILibraryManager _libraryManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="StorageUsageController"/> class.
    /// </summary>
    /// <param name="libraryManager">Library manager.</param>
    public StorageUsageController(ILibraryManager libraryManager)
    {
        _libraryManager = libraryManager;
    }

    /// <summary>
    /// Gets usage for mounted drives that contain Jellyfin library paths.
    /// </summary>
    /// <returns>A list of storage usage entries.</returns>
    [HttpGet("Usage")]
    public ActionResult<IReadOnlyList<StorageUsageEntry>> GetUsage()
    {
        var libraryPaths = _libraryManager
            .GetVirtualFolders()
            .SelectMany(v => v.Locations ?? Array.Empty<string>())
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(GetPathComparer())
            .ToArray();

        var entries = new List<StorageUsageEntry>();

        foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
        {
            var matchingPaths = libraryPaths
                .Where(path => IsPathOnDrive(path, drive.Name))
                .ToArray();

            if (matchingPaths.Length == 0)
            {
                continue;
            }

            long totalBytes;
            long freeBytes;

            try
            {
                totalBytes = drive.TotalSize;
                freeBytes = drive.AvailableFreeSpace;
            }
            catch (IOException)
            {
                continue;
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }

            var usedBytes = Math.Max(0, totalBytes - freeBytes);
            var usedPercent = totalBytes == 0 ? 0 : (double)usedBytes / totalBytes * 100;

            entries.Add(new StorageUsageEntry
            {
                DriveName = drive.Name,
                VolumeLabel = drive.VolumeLabel,
                TotalBytes = totalBytes,
                UsedBytes = usedBytes,
                FreeBytes = freeBytes,
                UsedPercent = Math.Round(usedPercent, 2),
                LibraryPaths = matchingPaths
            });
        }

        return Ok(entries.OrderBy(e => e.DriveName, GetStringComparer()).ToArray());
    }

    /// <summary>
    /// Gets media usage grouped by media type from Jellyfin indexed items.
    /// </summary>
    /// <returns>List of grouped usage entries by media type.</returns>
    [HttpGet("MediaTypeUsage")]
    public ActionResult<IReadOnlyList<MediaTypeUsageEntry>> GetMediaTypeUsage()
    {
        var query = new InternalItemsQuery
        {
            Recursive = true,
            IncludeItemTypes = new[]
            {
                BaseItemKind.Movie,
                BaseItemKind.Episode,
                BaseItemKind.Audio,
                BaseItemKind.Photo,
                BaseItemKind.MusicVideo,
                BaseItemKind.Video
            }
        };

        var items = _libraryManager.GetItemList(query).ToArray();

        var grouped = items
            .GroupBy(GetMediaTypeKey)
            .Select(g => new MediaTypeUsageEntry
            {
                Key = g.Key,
                DisplayName = GetMediaTypeDisplayName(g.Key),
                ColorHex = GetMediaTypeColor(g.Key),
                ItemCount = g.Count(),
                TotalBytes = g.Sum(GetItemSize)
            })
            .Where(x => x.ItemCount > 0)
            .OrderByDescending(x => x.TotalBytes)
            .ToList();

        var allBytes = grouped.Sum(x => x.TotalBytes);
        foreach (var entry in grouped)
        {
            entry.SharePercent = allBytes == 0 ? 0 : Math.Round((double)entry.TotalBytes / allBytes * 100, 2);
        }

        return Ok(grouped);
    }

    private static long GetItemSize(BaseItem item)
    {
        try
        {
            return item.Size ?? 0;
        }
        catch (Exception)
        {
            return 0;
        }
    }

    private static string GetMediaTypeKey(BaseItem item)
    {
        var typeName = item.GetType().Name;
        return typeName switch
        {
            "Movie" => "movie",
            "Episode" => "series",
            "Audio" => "music",
            "Photo" => "images",
            "MusicVideo" => "musicvideo",
            "Video" => "video",
            _ => "other"
        };
    }

    private static string GetMediaTypeDisplayName(string key)
        => key switch
        {
            "movie" => "Movies",
            "series" => "Series / Episodes",
            "music" => "Music",
            "images" => "Images",
            "musicvideo" => "Music Videos",
            "video" => "Videos",
            _ => "Other"
        };

    private static string GetMediaTypeColor(string key)
        => key switch
        {
            "movie" => "#4f8cff",
            "series" => "#37c978",
            "music" => "#b076ff",
            "images" => "#ff9f43",
            "musicvideo" => "#ff5f7a",
            "video" => "#00b8d9",
            _ => "#9aa0a6"
        };

    private static bool IsPathOnDrive(string path, string driveName)
    {
        if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(driveName))
        {
            return false;
        }

        string normalizedPath;
        string normalizedDrive;

        try
        {
            normalizedPath = Path.GetFullPath(path).Replace('\\', '/');
            normalizedDrive = Path.GetFullPath(driveName).Replace('\\', '/');
        }
        catch (Exception)
        {
            return false;
        }

        if (!normalizedDrive.EndsWith('/'))
        {
            normalizedDrive += "/";
        }

        var comparison = GetStringComparison();
        return normalizedPath.StartsWith(normalizedDrive, comparison)
            || string.Equals(normalizedPath, normalizedDrive.TrimEnd('/'), comparison);
    }

    private static StringComparer GetPathComparer()
        => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;

    private static StringComparer GetStringComparer()
        => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;

    private static StringComparison GetStringComparison()
        => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
}

/// <summary>
/// Single storage usage result entry.
/// </summary>
public class StorageUsageEntry
{
    /// <summary>
    /// Gets or sets drive name or mountpoint root.
    /// </summary>
    public string DriveName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets volume label.
    /// </summary>
    public string VolumeLabel { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets total bytes.
    /// </summary>
    public long TotalBytes { get; set; }

    /// <summary>
    /// Gets or sets used bytes.
    /// </summary>
    public long UsedBytes { get; set; }

    /// <summary>
    /// Gets or sets free bytes.
    /// </summary>
    public long FreeBytes { get; set; }

    /// <summary>
    /// Gets or sets used percentage.
    /// </summary>
    public double UsedPercent { get; set; }

    /// <summary>
    /// Gets or sets Jellyfin library paths on this drive.
    /// </summary>
    public IReadOnlyList<string> LibraryPaths { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Media type usage summary entry.
/// </summary>
public class MediaTypeUsageEntry
{
    /// <summary>
    /// Gets or sets media type key.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets display color.
    /// </summary>
    public string ColorHex { get; set; } = "#9aa0a6";

    /// <summary>
    /// Gets or sets number of items.
    /// </summary>
    public int ItemCount { get; set; }

    /// <summary>
    /// Gets or sets total bytes of items in this media type.
    /// </summary>
    public long TotalBytes { get; set; }

    /// <summary>
    /// Gets or sets share in percent of all media-type bytes.
    /// </summary>
    public double SharePercent { get; set; }
}
