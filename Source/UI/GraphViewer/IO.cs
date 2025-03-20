using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.MacroRoutingTool.UI;

public static partial class GraphViewer {
    public static class IO {
        public static string CurrentFullPath = "";

        public static string CurrentDisplayPath {get {
            string path = CurrentFullPath;
            if (path.StartsWith(Engine.AssemblyDirectory)) {
                path.Replace(Engine.AssemblyDirectory, "%CELESTE%");
            }
            return path;
        }}

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
                    Engine.Commands.Log(string.Format(MRTDialog.IOFailCreateRoot, MRTDialog.ModSettingsSectionTitle, MRTModule.Settings.MRTDirectoryAbsolute, e.Message), Color.Red);
                }
            }
        }

        public static bool TryGetPaths(out string[] paths, FileType type = FileType.Any) {
            string[] filenames = null;
            try {
                filenames = Directory.GetFiles(MRTModule.Settings.MRTDirectoryAbsolute, $"*.{FileTypeText[type]}.yaml", SearchOption.AllDirectories);
            } catch (Exception e) {
                Working = false;
                Logger.Error("MacroRoutingTool/IO", $"{e.Message}\n{e.StackTrace}");
                Engine.Commands.Open = true;
                Engine.Commands.Log(string.Format(MRTDialog.IOFailOpenRoot, MRTDialog.ModSettingsSectionTitle, MRTModule.Settings.MRTDirectoryAbsolute, e.Message), Color.Red);
            }
            paths = filenames;
            return paths == null;
        }

        public static bool TryOpenGraphFile(Guid id) {
            if (TryGetPaths(out string[] graphFiles, FileType.Graph)) {
                foreach (string graphPath in graphFiles) {
                    
                }
            }
        }

        public static bool TryOpenGraphFile(string path) {
            string yaml = null;
            try {
                yaml = File.ReadAllText(path);
            } catch (Exception e) {
                Logger.Error("MacroRoutingTool/IO", $"{e.Message}\n{e.StackTrace}");
                Engine.Commands.Open = true;
                Engine.Commands.Log(string.Format(MRTDialog.IOFailOpenFile, MRTDialog.ModSettingsSectionTitle) + string.Format(MRTDialog.IOFailOpenFileItem, path, e.Message), Color.Red);
            }
            bool success = Data.Graph.TryParse(yaml, out Data.Graph newGraph);
            if (success) {Graph = newGraph;}
            return success;
        }

        public static bool TryOpenRouteFile(Guid id) {

        }

        public static bool TryOpenRouteFile(string path) {

        }
    }
}