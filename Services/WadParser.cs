using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace DMINLauncher.Services;

public class WadInfo
{
    public string FilePath { get; set; } = "";
    public string FileName => Path.GetFileName(FilePath);
    public string WadType { get; set; } = "Unknown"; // IWAD or PWAD
    public long FileSize { get; set; }
    public int LumpCount { get; set; }
    public List<string> MapNames { get; set; } = new();
    public int MapCount => MapNames.Count;
    public bool IsValid { get; set; }
    public string Error { get; set; } = "";

    public string FileSizeFormatted
    {
        get
        {
            if (FileSize < 1024) return $"{FileSize} B";
            if (FileSize < 1024 * 1024) return $"{FileSize / 1024.0:F1} KB";
            return $"{FileSize / (1024.0 * 1024.0):F1} MB";
        }
    }

    public string Summary
    {
        get
        {
            if (!IsValid) return Error;
            var maps = MapCount > 0 ? $"{MapCount} map{(MapCount != 1 ? "s" : "")}" : "No maps";
            return $"{WadType} | {FileSizeFormatted} | {maps}";
        }
    }

    public string MapListSummary
    {
        get
        {
            if (MapNames.Count == 0) return "";
            if (MapNames.Count <= 5) return string.Join(", ", MapNames);
            return string.Join(", ", MapNames.Take(5)) + $" (+{MapNames.Count - 5} more)";
        }
    }
}

public static class WadParser
{
    // Common DOOM map markers (ExMy for DOOM 1, MAPxx for DOOM 2)
    private static readonly HashSet<string> MapMarkerPrefixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "E1M", "E2M", "E3M", "E4M", "E5M", "E6M", // DOOM 1 episodes
        "MAP"  // DOOM 2 style
    };

    public static WadInfo Parse(string filePath)
    {
        var info = new WadInfo { FilePath = filePath };

        try
        {
            if (!File.Exists(filePath))
            {
                info.Error = "File not found";
                return info;
            }

            var fileInfo = new FileInfo(filePath);
            info.FileSize = fileInfo.Length;

            // Check file extension for PK3/PK7/ZIP files
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            if (extension == ".pk3" || extension == ".pk7" || extension == ".zip" || extension == ".ipk3")
            {
                return ParsePk3(filePath, info);
            }

            using var stream = File.OpenRead(filePath);
            using var reader = new BinaryReader(stream);

            // Read WAD header (12 bytes)
            if (stream.Length < 12)
            {
                info.Error = "File too small";
                return info;
            }

            // Read signature (4 bytes): "IWAD" or "PWAD"
            var signatureBytes = reader.ReadBytes(4);
            var signature = Encoding.ASCII.GetString(signatureBytes);
            
            // Check if it's actually a PK3/ZIP file with wrong extension
            if (signatureBytes[0] == 0x50 && signatureBytes[1] == 0x4B) // "PK"
            {
                stream.Seek(0, SeekOrigin.Begin);
                return ParsePk3(filePath, info);
            }
            
            if (signature != "IWAD" && signature != "PWAD")
            {
                info.Error = $"Invalid WAD signature: {signature}";
                return info;
            }

            info.WadType = signature;
            info.IsValid = true;

            // Read lump count (4 bytes)
            info.LumpCount = reader.ReadInt32();

            // Read directory offset (4 bytes)
            var directoryOffset = reader.ReadInt32();

            // Validate directory offset
            if (directoryOffset < 12 || directoryOffset >= stream.Length)
            {
                info.Error = "Invalid directory offset";
                info.IsValid = false;
                return info;
            }

            // Read directory entries to find maps
            stream.Seek(directoryOffset, SeekOrigin.Begin);

            for (int i = 0; i < info.LumpCount && stream.Position + 16 <= stream.Length; i++)
            {
                // Each directory entry is 16 bytes:
                // - 4 bytes: file offset
                // - 4 bytes: size
                // - 8 bytes: name (null-padded)
                reader.ReadInt32(); // offset (skip)
                reader.ReadInt32(); // size (skip)
                var nameBytes = reader.ReadBytes(8);
                var name = Encoding.ASCII.GetString(nameBytes).TrimEnd('\0').ToUpperInvariant();

                // Check if this is a map marker
                if (IsMapMarker(name))
                {
                    info.MapNames.Add(name);
                }
            }

            // Sort map names
            info.MapNames.Sort(CompareMapNames);
        }
        catch (Exception ex)
        {
            info.Error = $"Parse error: {ex.Message}";
            info.IsValid = false;
        }

        return info;
    }

    private static bool IsMapMarker(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;

        // Check for ExMy format (DOOM 1)
        if (name.Length == 4 && name[0] == 'E' && name[2] == 'M' &&
            char.IsDigit(name[1]) && char.IsDigit(name[3]))
        {
            return true;
        }

        // Check for MAPxx format (DOOM 2)
        if (name.Length >= 5 && name.StartsWith("MAP") &&
            char.IsDigit(name[3]) && char.IsDigit(name[4]))
        {
            return true;
        }

        return false;
    }

    private static int CompareMapNames(string a, string b)
    {
        // Sort E1M1 before E1M2, MAP01 before MAP02, etc.
        if (a.StartsWith("E") && b.StartsWith("E"))
        {
            // Compare episode first, then map
            var epA = a[1];
            var epB = b[1];
            if (epA != epB) return epA.CompareTo(epB);
            return string.Compare(a, b, StringComparison.Ordinal);
        }
        if (a.StartsWith("MAP") && b.StartsWith("MAP"))
        {
            // Extract map numbers
            if (int.TryParse(a.Substring(3), out var numA) &&
                int.TryParse(b.Substring(3), out var numB))
            {
                return numA.CompareTo(numB);
            }
        }
        return string.Compare(a, b, StringComparison.Ordinal);
    }

    private static WadInfo ParsePk3(string filePath, WadInfo info)
    {
        try
        {
            using var archive = ZipFile.OpenRead(filePath);
            
            info.WadType = "PK3";
            info.IsValid = true;
            info.LumpCount = archive.Entries.Count;

            // Look for map markers in the archive
            // Maps in PK3 can be in maps/ folder as WAD files or as UDMF text format
            foreach (var entry in archive.Entries)
            {
                var name = entry.FullName.ToUpperInvariant();
                var fileName = Path.GetFileNameWithoutExtension(entry.Name).ToUpperInvariant();
                
                // Check for maps in maps/ folder (e.g., maps/MAP01.wad or maps/E1M1.wad)
                if (name.StartsWith("MAPS/") && entry.Name.EndsWith(".wad", StringComparison.OrdinalIgnoreCase))
                {
                    if (IsMapMarker(fileName))
                    {
                        info.MapNames.Add(fileName);
                    }
                }
                // Check for UDMF maps (maps/MAPxx/ folder with TEXTMAP file)
                else if (name.StartsWith("MAPS/") && name.EndsWith("/TEXTMAP"))
                {
                    // Extract map name from path like "MAPS/MAP01/TEXTMAP"
                    var parts = name.Split('/');
                    if (parts.Length >= 2)
                    {
                        var mapName = parts[1];
                        if (IsMapMarker(mapName) && !info.MapNames.Contains(mapName))
                        {
                            info.MapNames.Add(mapName);
                        }
                    }
                }
                // Check for embedded WAD files that might contain maps
                else if (entry.Name.EndsWith(".wad", StringComparison.OrdinalIgnoreCase))
                {
                    // Try to parse embedded WAD for map markers
                    try
                    {
                        using var wadStream = entry.Open();
                        using var memStream = new MemoryStream();
                        wadStream.CopyTo(memStream);
                        memStream.Seek(0, SeekOrigin.Begin);
                        
                        var embeddedMaps = ParseWadStreamForMaps(memStream);
                        foreach (var map in embeddedMaps)
                        {
                            if (!info.MapNames.Contains(map))
                            {
                                info.MapNames.Add(map);
                            }
                        }
                    }
                    catch
                    {
                        // Ignore errors parsing embedded WADs
                    }
                }
            }

            // Sort map names
            info.MapNames.Sort(CompareMapNames);
        }
        catch (InvalidDataException)
        {
            info.Error = "Invalid PK3/ZIP archive";
            info.IsValid = false;
        }
        catch (Exception ex)
        {
            info.Error = $"Parse error: {ex.Message}";
            info.IsValid = false;
        }

        return info;
    }

    private static List<string> ParseWadStreamForMaps(Stream stream)
    {
        var maps = new List<string>();
        
        using var reader = new BinaryReader(stream, Encoding.ASCII, leaveOpen: true);
        
        if (stream.Length < 12) return maps;

        var signature = Encoding.ASCII.GetString(reader.ReadBytes(4));
        if (signature != "IWAD" && signature != "PWAD") return maps;

        var lumpCount = reader.ReadInt32();
        var directoryOffset = reader.ReadInt32();

        if (directoryOffset < 12 || directoryOffset >= stream.Length) return maps;

        stream.Seek(directoryOffset, SeekOrigin.Begin);

        for (int i = 0; i < lumpCount && stream.Position + 16 <= stream.Length; i++)
        {
            reader.ReadInt32(); // offset (skip)
            reader.ReadInt32(); // size (skip)
            var nameBytes = reader.ReadBytes(8);
            var name = Encoding.ASCII.GetString(nameBytes).TrimEnd('\0').ToUpperInvariant();

            if (IsMapMarker(name))
            {
                maps.Add(name);
            }
        }

        return maps;
    }
}
