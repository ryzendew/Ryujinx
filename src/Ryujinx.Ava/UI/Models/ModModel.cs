using Ryujinx.Ava.UI.ViewModels;
using System.IO;

namespace Ryujinx.Ava.UI.Models
{
    public class ModModel : BaseModel
    {
        private bool _enabled;

        public bool Enabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
                OnPropertyChanged();
            }
        }

        public string ContainerPath { get; }
        public string FullPath { get; }
        public string FileName => Path.GetFileName(ContainerPath);

        public ModModel(string containerPath, string fullPath, bool enabled)
        {
            ContainerPath = containerPath;
            FullPath = fullPath;
            Enabled = enabled;
        }
    }
}