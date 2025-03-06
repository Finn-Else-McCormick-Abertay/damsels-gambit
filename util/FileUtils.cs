using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Godot;

namespace DamselsGambit.Util;

public static class FileUtils
{
    public static string RelativePath(string absolutePath, string relativeTo) {
        absolutePath = absolutePath.Replace('\\', '/').StripFront("res://");
        relativeTo = $"{relativeTo.Replace('\\', '/').StripFront("res://").StripBack('/', true)}/";

        if (absolutePath.StartsWith(relativeTo)) return absolutePath.StripFront(relativeTo);

        var separatorIndices = absolutePath.FindAll('/');

        int fileLevels = separatorIndices.Count();
        int otherLevels = relativeTo.FindAll('/').Count();

        int sharedLevels = 0;
        foreach (var index in separatorIndices) if (relativeTo.StartsWith(absolutePath[..(index + 1)])) sharedLevels++; else break;

        var returnString = string.Concat(Enumerable.Repeat("../", otherLevels - sharedLevels));
        returnString += separatorIndices.Skip(sharedLevels - 1).Aggregate("", (working, index) => {
            var nextIndex = absolutePath.Find('/', index + 1);
            GD.Print($"{index} -> {nextIndex}, {(nextIndex != -1 ? absolutePath[(index + 1)..nextIndex] : absolutePath[(index + 1)..])}");
            return $"{working}{(nextIndex != -1 ? $"{absolutePath[(index + 1)..nextIndex]}/" : absolutePath[(index + 1)..])}";
        });
        return returnString;
    }


    public static Collection<string> GetFiles(string folder = "res://", bool recursive = true, bool relative = false, IEnumerable<string> fileExtensions = null) {
        // Normalisation
        folder = $"res://{folder.Replace('\\', '/').StripEdges("res://", "/")}/";
        fileExtensions = fileExtensions?.Select(x => x.StripFront('.', true));

        // In an exported build, the files DirAccess will find have '.remap' on the end, but the paths you need to pass as arguments don't
        var files = DirAccess.GetFilesAt(folder)
            .Select(file => file.StripBack(".remap"))
            .Select(file => relative ? file : $"{folder}{file}")
            .Where(file => fileExtensions is null || fileExtensions.Contains(file.GetExtension())) // Skip if not one of the valid extensions. Null for 'all extensions valid'
            .ToList();

        if (recursive) foreach (var directory in DirAccess.GetDirectoriesAt(folder))
            files.AddRange(GetFiles($"{folder}{directory}", recursive, relative, fileExtensions).Select(file => relative ? $"{directory}/{file}" : file));

        return [..files];
    }


    public static Collection<string> GetFilesOfType(string fileExtension, string folder = "res://", bool recursive = true, bool relative = false) => GetFiles(folder, recursive, relative, [fileExtension]);
    public static Collection<string> GetFilesOfType(IEnumerable<string> fileExtensions, string folder = "res://", bool recursive = true, bool relative = false) => GetFiles(folder, recursive, relative, fileExtensions);
    public static Collection<string> GetFilesOfType<T>(string folder = "res://", bool recursive = true, bool relative = false) where T : Resource
        => GetFiles(folder, recursive, relative, 0 switch {
            _ when typeof(T).IsSubclassOf(typeof(Texture)) => [ ".bmp", ".dds", ".ktx", ".exr", ".hdr", ".jpg", ".jpeg", ".png", ".tga", ".svg", ".webp" ],
            _ when typeof(T) == typeof(FontFile) => [ ".ttf", ".otf", ".ttc", ".otc", ".woff", ".woff2", ".pfb", ".pfm" ],
            _ when typeof(T) == typeof(PackedScene) => [ ".tscn", ".scn" ],
            _ when typeof(T) == typeof(Theme) => [ ".theme", ".tres", ".res" ],
            _ when typeof(T).IsSubclassOf(typeof(Resource)) => [ ".tres", ".res" ],
            _ => []
        });

    public static Collection<T> LoadFilesOfType<T>(string folder = "res://", bool recursive = true) where T : Resource
        => [..GetFilesOfType<T>(folder, recursive).Select(filePath => ResourceLoader.Load<T>(filePath))];
    
    
    public static Collection<(string Absolute, string Relative)> GetFilesAbsoluteAndRelative(string folder = "res://", bool recursive = true, IEnumerable<string> fileExtensions = null)
        => [..GetFiles(folder, recursive, false, fileExtensions).Select(filePath => (filePath, RelativePath(filePath, folder)))];

    public static Collection<(string Absolute, string Relative)> GetFilesOfTypeAbsoluteAndRelative(string fileExtension, string folder = "res://", bool recursive = true)
        => [..GetFilesOfType(fileExtension, folder, recursive, false).Select(filePath => (filePath, RelativePath(filePath, folder)))];
    
    public static Collection<(string Absolute, string Relative)> GetFilesOfTypeAbsoluteAndRelative<T>(string folder = "res://", bool recursive = true) where T : Resource
        => [..GetFilesOfType<T>(folder, recursive, false).Select(filePath => (filePath, RelativePath(filePath, folder)))];
}