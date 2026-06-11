using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using MiLauncher.launcher.ui.pages;
using MiLauncher.launcher.ui.pages.global;

namespace MiLauncher.launcher.ui
{
    public class GeneralSettingsViewModel : ViewModelBase
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

        public GeneralSettingsViewModel()
        {
            Pages = new ObservableCollection<BasePageViewModel>
            {
                new LauncherPageViewModel(),
                new JavaPageViewModel(),
                new AccountListPageViewModel(),
                new AppearancePageViewModel(),
                new SystemPageViewModel()
            };

            SelectedPage = Pages.FirstOrDefault();
        }
    }
}
