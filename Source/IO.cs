using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Celeste.Mod.MacroRoutingTool.Data;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.MacroRoutingTool;

public static partial class IO {
    public static Color WarnColor = Color.Orange;
    public static Color ErrorColor = Color.Red;

    public static string CurrentFullPath = "";

    public static string DisplayPath(string path) {
        string celesteDir = Engine.AssemblyDirectory;
        if (path.StartsWith(celesteDir)) {
            return "%CELESTE%" + (celesteDir.Length >= path.Length ? "" : path[celesteDir.Length..]);
        } else {
            //TODO check for $HOME on unix
            //TODO hide user folder on windows (can't just check for letter:\Users\ https://learn.microsoft.com/en-us/windows/win32/fileio/determining-whether-a-directory-is-a-volume-mount-point)
        }
        return path;
    }

    public static string CurrentDisplayPath => DisplayPath(CurrentFullPath);

    /// <summary>
    /// Files that store MRT data are expected to end with two extensions, e.g. <c>.graph.yaml</c>. This method returns
    /// the first extension of the filename in the given path (<c>graph</c> in this example), which MRT interprets
    /// as the type of object the file's contents should be parsed as (a <see cref="Graph"/> in this example).<br/>
    /// If the filename in the given path doesn't contain two extensions, this method returns null.
    /// </summary>
    /// <param name="path">Path to the file whose MRT extension is to be read.</param>
    public static string MRTExtension(string path) {
        string[] splitByExtChar = Path.GetFileName(path)?.Split('.');
        if (splitByExtChar?.Length > 2) {return splitByExtChar?[^2];}
        return null;
    }

    public static class FileType {
        public const string Any = nameof(Any);
        public const string Graph = nameof(Graph);
        public const string Route = nameof(Route);
    }
    public class FileTypeInfo {
        public Type Type;
        public string Extension;
    }
    public static Dictionary<string, FileTypeInfo> FileTypeInfos = new(){
        {FileType.Any, new(){Extension = "*"}},
        {FileType.Graph, new(){Extension = "graph", Type = typeof(Graph)}},
        {FileType.Route, new(){Extension = "route", Type = typeof(Route)}}
    };

    public static bool Working = true;

    public static void Initialize() {
        Working = true;
        if (!Path.Exists(MRTModule.Settings.MRTDirectoryAbsolute)) {
            try {
                Directory.CreateDirectory(MRTModule.Settings.MRTDirectoryAbsolute);
            } catch (Exception e) {
                Working = false;
                Logger.Error("MacroRoutingTool/IO", $"{e.Message}\n{e.StackTrace}");
                Engine.Commands.Open = true;
                Engine.Commands.Log(string.Format(MRTDialog.IOFailCreateRoot, MRTDialog.ModTitle, MRTModule.Settings.MRTDirectory, e.Message), Color.Red);
            }
        }
    }

    public static bool TryLoadAll(string type = FileType.Any) {
        if (FileTypeInfos.TryGetValue(type, out FileTypeInfo searchTypeInfo)) {
            try {
                string[] paths = Directory.GetFiles(MRTModule.Settings.MRTDirectoryAbsolute, $"*.{searchTypeInfo.Extension}.yaml", SearchOption.AllDirectories);
                string ioFails = "";
                foreach (var path in paths) {
                    string yaml = null;
                    try {
                        yaml = File.ReadAllText(path);
                        dynamic result = null;
                        if (FileTypeInfos.TryGetValue(MRTExtension(path), out FileTypeInfo resultTypeInfo)) {
                            if ((bool)resultTypeInfo.Type
                                .GetMethod(nameof(MRTExport<Graph>.TryParse), BindingFlags.Public | BindingFlags.Static)
                                .Invoke(null, [yaml, result])
                            ) {
                                dynamic list = resultTypeInfo.Type
                                    .GetField(nameof(Graph.List), BindingFlags.Public | BindingFlags.Static)
                                    .GetValue(null);
                                list.Add(result);
                            }
                        }
                    } catch (Exception e) {
                        ioFails += string.Format(MRTDialog.IOFailOpenFileItem, DisplayPath(path), e.Message);
                    }
                }
                if (ioFails.Length > 0) {
                    ioFails = string.Format(MRTDialog.IOFailOpenFileList, MRTDialog.ModTitle) + ioFails;
                    Logger.Warn("MacroRoutingTool/IO", ioFails);
                    Engine.Commands.Open = true;
                    Engine.Commands.Log(ioFails, WarnColor);
                }
                return true;
            } catch (Exception e) {
                Working = false;
                Logger.Error("MacroRoutingTool/IO", $"{e.Message}\n{e.StackTrace}");
                Engine.Commands.Open = true;
                Engine.Commands.Log(string.Format(MRTDialog.IOFailOpenRoot, MRTDialog.ModTitle, MRTModule.Settings.MRTDirectory, e.Message), ErrorColor);
                return false;
            }
        }
        return false;
    }

    public static bool TryOpen<T>(string path, Guid id, out T item) {
        item = default;
        string yaml = null;
        try {
            yaml = File.ReadAllText(path);
            return (bool)typeof(T)
                .GetMethod(nameof(MRTExport<Graph>.TryParse), BindingFlags.Public | BindingFlags.Static)
                .Invoke(null, [yaml, item]);
        } catch (FileNotFoundException) {
            string typetext = MRTExtension(path);
            Logger.Info("MacroRoutingTool/IO", string.Format(MRTDialog.IORelocate, typetext, id));
            if (TryLoadAll()) {
                List<T> list = (List<T>)typeof(T)
                    .GetField(nameof(Graph.List), BindingFlags.Public | BindingFlags.Static)
                    .GetValue(null);
                FieldInfo idGetter = typeof(T).GetField(nameof(Graph.ID));
                try {
                    item = list.First(listItem => (Guid)idGetter.GetValue(listItem) == id);
                    return true;
                } catch (InvalidOperationException) {
                    //can't use FirstOrDefault -- fallback can't be null bc T might be non-nullable, and shouldn't be default(T) bc that might be a possible value for a T.
                    //so instead i use First. it throws this exception if there's no match (including if the list is empty), and that shouldn't be fatal.
                    //instead, TODO prompt the user to either save the graph to a new file or discard the graph.
                    return false;
                }
            }
            return false;
        } catch (Exception e) {
            Logger.Error("MacroRoutingTool/IO", $"{e.Message}\n{e.StackTrace}");
            Engine.Commands.Open = true;
            Engine.Commands.Log(string.Format(MRTDialog.IOFailOpenFile, MRTDialog.ModTitle) + string.Format(MRTDialog.IOFailOpenFileItem, path, e.Message), ErrorColor);
        }
        return false;
    }
}