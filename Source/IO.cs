using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Celeste.Mod.MacroRoutingTool.Data;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.MacroRoutingTool;

public static partial class IO {
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

    public enum FileType {
        Any,
        Graph,
        Route
    }
    public static Dictionary<FileType, string> FileTypeText = new(){
        {FileType.Any, "*"},
        {FileType.Graph, "graph"},
        {FileType.Route, "route"}
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

    public static bool TryLoadAll(FileType type = FileType.Any) {
        try {
            string[] paths = Directory.GetFiles(MRTModule.Settings.MRTDirectoryAbsolute, $"*.{FileTypeText[type]}.yaml", SearchOption.AllDirectories);
            string ioFails = "";
            foreach (var path in paths) {
                string yaml = null;
                try {
                    yaml = File.ReadAllText(path);
                    string[] splitByExtension = path.Split('.');
                    string typetext = splitByExtension[^2]; //the last one is "yaml", the 2nd last is what MRT cares about
                    //TODO unhardcode the association between file type and parsing method
                    if (typetext == FileTypeText[FileType.Graph]) {
                        if (Graph.TryParse(yaml, out Graph graph)) {
                            Graph.List.Add(graph);
                        }
                    } else if (typetext == FileTypeText[FileType.Route]) {
                        if (Route.TryParse(yaml, out Route route)) {
                            Route.List.Add(route);
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
                Engine.Commands.Log(ioFails, Color.Orange);
            }
            return true;
        } catch (Exception e) {
            Working = false;
            Logger.Error("MacroRoutingTool/IO", $"{e.Message}\n{e.StackTrace}");
            Engine.Commands.Open = true;
            Engine.Commands.Log(string.Format(MRTDialog.IOFailOpenRoot, MRTDialog.ModTitle, MRTModule.Settings.MRTDirectory, e.Message), Color.Red);
            return false;
        }
    }

    public static bool TryOpen<T>(string path, Guid id, out T item) where T : MRTExport {
        item = null;
        string yaml = null;
        try {
            yaml = File.ReadAllText(path);
            return (bool)typeof(T)
                .GetMethod(nameof(MRTExport.TryParse), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                .Invoke(null, [yaml, item]);
        } catch (FileNotFoundException) {
            string[] splitByExtension = path.Split('.');
            string typetext = splitByExtension[^2];
            Logger.Info("MacroRoutingTool/IO", string.Format(MRTDialog.IORelocate, typetext, id));
            if (TryLoadAll()) {
                //TODO unhardcode association between type text and list
                if (typetext == FileTypeText[FileType.Graph]) {
                    item = (T)(object)Graph.List.FirstOrDefault(graph => graph.ID == id, null);
                } else if (typetext == FileTypeText[FileType.Route]) {
                    item = (T)(object)Route.List.FirstOrDefault(route => route.ID == id, null);
                }
                return item != null;
            }
            return false;
        } catch (Exception e) {
            Logger.Error("MacroRoutingTool/IO", $"{e.Message}\n{e.StackTrace}");
            Engine.Commands.Open = true;
            Engine.Commands.Log(string.Format(MRTDialog.IOFailOpenFile, MRTDialog.ModTitle) + string.Format(MRTDialog.IOFailOpenFileItem, path, e.Message), Color.Red);
        }
        return false;
    }
}