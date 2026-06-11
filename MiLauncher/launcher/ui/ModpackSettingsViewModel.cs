using System.Collections.ObjectModel;
using System.Linq;
using MiLauncher.launcher.ui.pages;
using MiLauncher.launcher.ui.pages.instance;

namespace MiLauncher.launcher.ui
{
    public class ModpackSettingsViewModel : ViewModelBase
    {
        public ObservableCollection<BasePageViewModel> Pages { get; }

        private BasePageViewModel _selectedPage;
        public BasePageViewModel SelectedPage
        {
            get => _selectedPage;
            set
            {
                if (_selectedPage != null)
                {
                    _selectedPage.OnPageUnloaded();
                }

                if (SetProperty(ref _selectedPage, value))
                {
                    if (_selectedPage != null)
                    {
                        _selectedPage.OnPageLoaded();
                    }
                }
            }
        }

        public ModpackSettingsViewModel(string instancePath)
        {
            Pages = new ObservableCollection<BasePageViewModel>
            {
                new InstanceSettingsPageViewModel(instancePath),
                new ModFolderPageViewModel(instancePath),
                new ResourcePackPageViewModel(instancePath),
                new GameOptionsPageViewModel(instancePath),
                new LogPageViewModel(instancePath)
            };

            SelectedPage = Pages.FirstOrDefault();
        }
    }
}
