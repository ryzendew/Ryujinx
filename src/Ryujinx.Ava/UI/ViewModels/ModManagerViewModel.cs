using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using DynamicData;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.UI.Helpers;
using Ryujinx.Ava.UI.Models;
using Ryujinx.Common.Configuration;
using Ryujinx.Common.Logging;
using Ryujinx.Common.Utilities;
using Ryujinx.HLE.HOS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ryujinx.Ava.UI.ViewModels
{
    public class ModManagerViewModel : BaseModel
    {
        public ModMetadata _modData;
        public readonly string _modJsonPath;

        public AvaloniaList<ModModel> _mods = new();
        public AvaloniaList<ModModel> _views = new();
        public AvaloniaList<ModModel> _selectedMods = new();

        private ulong _titleId { get; }
        private string _titleName { get; }

        private string _search;

        private static readonly ModMetadataJsonSerializerContext SerializerContext = new(JsonHelper.GetDefaultSerializerOptions());

        public AvaloniaList<ModModel> Mods
        {
            get => _mods;
            set
            {
                _mods = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ModCount));
                Sort();
            }
        }

        public AvaloniaList<ModModel> Views
        {
            get => _views;
            set
            {
                _views = value;
                OnPropertyChanged();
            }
        }

        public AvaloniaList<ModModel> SelectedMods
        {
            get => _selectedMods;
            set
            {
                _selectedMods = value;
                OnPropertyChanged();
            }
        }

        public string Search
        {
            get => _search;
            set
            {
                _search = value;
                OnPropertyChanged();
                Sort();
            }
        }

        public string ModCount
        {
            get => string.Format(LocaleManager.Instance[LocaleKeys.ModWindowHeading], Mods.Count);
        }

        public ModManagerViewModel(ulong titleId, string titleName)
        {
            _titleId = titleId;
            _titleName = titleName;

            _modJsonPath = Path.Combine(AppDataManager.GamesDirPath, titleId.ToString("x16"), "mods.json");

            try
            {
                _modData = JsonHelper.DeserializeFromFile(_modJsonPath, SerializerContext.ModMetadata);
            }
            catch
            {
                Logger.Warning?.Print(LogClass.Application, $"Failed to deserialize mod data for {_titleId} at {_modJsonPath}");

                _modData = new ModMetadata
                {
                    Mods = new List<Mod>()
                };

                Save();
            }

            LoadMods(titleId);
        }

        private void LoadMods(ulong titleId)
        {
            string modsBasePath = ModLoader.GetModsBasePath();

            var modCache = new ModLoader.ModCache();
            ModLoader.QueryContentsDir(modCache, new DirectoryInfo(Path.Combine(modsBasePath, "contents")), titleId);

            foreach (var mod in modCache.RomfsDirs)
            {
                Mods.Add(new ModModel(mod.Path.FullName, mod.Name, mod.Enabled));
            }

            foreach (var mod in modCache.RomfsContainers)
            {
                Mods.Add(new ModModel(mod.Path.FullName, mod.Name, mod.Enabled));
            }

            foreach (var mod in modCache.ExefsDirs)
            {
                Mods.Add(new ModModel(mod.Path.FullName, mod.Name, mod.Enabled));
            }

            foreach (var mod in modCache.ExefsContainers)
            {
                Mods.Add(new ModModel(mod.Path.FullName, mod.Name, mod.Enabled));
            }

            SelectedMods = new(Mods.Where(x => x.Enabled));

            Sort();
        }

        public void Sort()
        {
            Mods.AsObservableChangeSet()
                .Filter(Filter)
                .Bind(out var view).AsObservableList();

            _views.Clear();
            _views.AddRange(view);
            OnPropertyChanged(nameof(ModCount));
        }

        private bool Filter(object arg)
        {
            if (arg is ModModel content)
            {
                return string.IsNullOrWhiteSpace(_search) || content.Name.ToLower().Contains(_search.ToLower());
            }

            return false;
        }

        public void Save()
        {
            _modData.Mods.Clear();

            foreach (ModModel mod in SelectedMods)
            {
                _modData.Mods.Add(new Mod
                {
                    Name = mod.Name,
                    Path = mod.Path,
                });
            }

            JsonHelper.SerializeToFile(_modJsonPath, _modData, SerializerContext.ModMetadata);
        }

        public void Remove(ModModel model)
        {
            Mods.Remove(model);
            OnPropertyChanged(nameof(ModCount));
            Sort();
        }

        private void AddMod(DirectoryInfo directory)
        {
            var directories = Directory.GetDirectories(directory.ToString(), "*", SearchOption.AllDirectories);
            var destinationDir = ModLoader.GetTitleDir(ModLoader.GetModsBasePath(), _titleId.ToString("x16"));

            foreach (var dir in directories)
            {
                string dirToCreate = dir.Replace(directory.Parent.ToString(), destinationDir);

                // Mod already exists
                if (Directory.Exists(dirToCreate))
                {
                    Dispatcher.UIThread.Post(async () =>
                    {
                        await ContentDialogHelper.CreateErrorDialog(LocaleManager.Instance.UpdateAndGetDynamicValue(LocaleKeys.DialogLoadModErrorMessage, "Director", dirToCreate));
                    });

                    return;
                }

                Directory.CreateDirectory(dirToCreate);
            }

            var files = Directory.GetFiles(directory.ToString(), "*", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                File.Copy(file, file.Replace(directory.Parent.ToString(), destinationDir), true);
            }

            LoadMods(_titleId);
        }

        public async void Add()
        {
            OpenFolderDialog dialog = new()
            {
                Title = LocaleManager.Instance[LocaleKeys.SelectModDialogTitle]
            };

            if (Avalonia.Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                string directory = await dialog.ShowAsync(desktop.MainWindow);

                if (directory != null)
                {
                    AddMod(new DirectoryInfo(directory));
                }
            }
        }

        public void RemoveAll()
        {
            Mods.Clear();
            OnPropertyChanged(nameof(ModCount));
            Sort();
        }

        public void EnableAll()
        {
            SelectedMods = new(Mods);
        }

        public void DisableAll()
        {
            SelectedMods.Clear();
        }
    }
}