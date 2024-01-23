using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls.ApplicationLifetimes;
using Ryujinx.Ava.UI.Models;
using Ryujinx.HLE.FileSystem;
using Ryujinx.HLE.HOS;
using Ryujinx.Ui.App.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ryujinx.Ava.UI.ViewModels
{
    public class CheatWindowViewModel : BaseModel
    {
        private readonly string _enabledCheatsPath;
        public AvaloniaList<CheatNode> LoadedCheats { get; } = new();
        public string BuildId { get; }

        public CheatWindowViewModel(VirtualFileSystem virtualFileSystem, ulong titleId, string titlePath)
        {
            BuildId = ApplicationData.GetApplicationBuildId(virtualFileSystem, titlePath);

            string modsBasePath = ModLoader.GetModsBasePath();
            string titleModsPath = ModLoader.GetTitleDir(modsBasePath, titleId.ToString("x16"));

            _enabledCheatsPath = Path.Combine(titleModsPath, "cheats", "enabled.txt");

            string[] enabled = Array.Empty<string>();

            if (File.Exists(_enabledCheatsPath))
            {
                enabled = File.ReadAllLines(_enabledCheatsPath);
            }

            var mods = new ModLoader.ModCache();

            ModLoader.QueryContentsDir(mods, new DirectoryInfo(Path.Combine(modsBasePath, "contents")), titleId);

            string currentCheatFile = string.Empty;
            string buildId = string.Empty;

            CheatNode currentGroup = null;

            foreach (var cheat in mods.Cheats)
            {
                if (cheat.Path.FullName != currentCheatFile)
                {
                    currentCheatFile = cheat.Path.FullName;
                    string parentPath = currentCheatFile.Replace(titleModsPath, "");

                    buildId = Path.GetFileNameWithoutExtension(currentCheatFile).ToUpper();
                    currentGroup = new CheatNode("", buildId, parentPath, true);

                    LoadedCheats.Add(currentGroup);
                }

                var model = new CheatNode(cheat.Name, buildId, "", false, enabled.Contains($"{buildId}-{cheat.Name}"));
                currentGroup?.SubNodes.Add(model);
            }
        }

        public async void CopyToClipboard()
        {
            if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                await desktop.MainWindow.Clipboard.SetTextAsync(BuildId);
            }
        }

        public void Save()
        {
            List<string> enabledCheats = new();

            foreach (var cheats in LoadedCheats)
            {
                foreach (var cheat in cheats.SubNodes)
                {
                    if (cheat.IsEnabled)
                    {
                        enabledCheats.Add(cheat.BuildIdKey);
                    }
                }
            }

            Directory.CreateDirectory(Path.GetDirectoryName(_enabledCheatsPath));

            File.WriteAllLines(_enabledCheatsPath, enabledCheats);
        }
    }
}
