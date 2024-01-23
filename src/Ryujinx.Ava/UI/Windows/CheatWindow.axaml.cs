using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;
using FluentAvalonia.UI.Controls;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.UI.Helpers;
using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.HLE.FileSystem;
using System.Threading.Tasks;

namespace Ryujinx.Ava.UI.Windows
{
    public partial class CheatWindow : UserControl
    {
        public CheatWindowViewModel ViewModel;

        public CheatWindow()
        {
            DataContext = this;

            InitializeComponent();
        }

        public CheatWindow(VirtualFileSystem virtualFileSystem, ulong titleId, string titleName, string titlePath)
        {
            DataContext = ViewModel = new CheatWindowViewModel(virtualFileSystem, titleId, titlePath);

            InitializeComponent();
        }

        public static async Task Show(VirtualFileSystem virtualFileSystem, ulong titleId, string titleName, string titlePath)
        {
            ContentDialog contentDialog = new()
            {
                PrimaryButtonText = "",
                SecondaryButtonText = "",
                CloseButtonText = "",
                Content = new CheatWindow(virtualFileSystem, titleId, titleName, titlePath),
                Title = string.Format(LocaleManager.Instance[LocaleKeys.CheatWindowHeading], titleName, titleId.ToString("X16")),
            };

            Style bottomBorder = new(x => x.OfType<Grid>().Name("DialogSpace").Child().OfType<Border>());
            bottomBorder.Setters.Add(new Setter(IsVisibleProperty, false));

            contentDialog.Styles.Add(bottomBorder);

            await ContentDialogHelper.ShowAsync(contentDialog);
        }

        private void SaveAndClose(object sender, RoutedEventArgs e)
        {
            ViewModel.Save();
            ((ContentDialog)Parent).Hide();
        }

        private void Close(object sender, RoutedEventArgs e)
        {
            ((ContentDialog)Parent).Hide();
        }
    }
}
