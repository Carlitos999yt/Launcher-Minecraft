using System;
using System.Collections.Generic;
using System.Windows.Input;
using MaterialDesignThemes.Wpf;
using MiLauncher.launcher.settings;

namespace MiLauncher.launcher.ui.pages
{
    public abstract class BasePageViewModel : ViewModelBase
    {
        private static readonly List<WeakReference<BasePageViewModel>> _instances = new List<WeakReference<BasePageViewModel>>();

        protected BasePageViewModel()
        {
            lock (_instances)
            {
                _instances.Add(new WeakReference<BasePageViewModel>(this));
            }
        }

        public bool IsDeveloperMode
        {
            get => SettingsManager.LoadSettings().IsDeveloperMode;
            set
            {
                var c = SettingsManager.LoadSettings();
                c.IsDeveloperMode = value;
                SettingsManager.SaveSettings(c);

                lock (_instances)
                {
                    for (int i = _instances.Count - 1; i >= 0; i--)
                    {
                        if (_instances[i].TryGetTarget(out var target))
                        {
                            target.OnPropertyChanged(nameof(IsDeveloperMode));
                            target.OnPropertyChanged("IsLoaderVersionEnabled");
                            target.OnPropertyChanged("IsDeleteAllowed");
                        }
                        else
                        {
                            _instances.RemoveAt(i);
                        }
                    }
                }
            }
        }

        public abstract string DisplayName { get; }
        public abstract PackIconKind Icon { get; }
        public virtual string Description => "";

        // Si la página tiene botones de "Aplicar", "Restaurar", etc.
        public virtual ICommand ApplyCommand => null;
        public virtual ICommand ResetCommand => null;

        // Hook para inicializar datos cuando se carga la página
        public virtual void OnPageLoaded() { }
        
        // Hook para guardar datos cuando se sale de la página
        public virtual void OnPageUnloaded() { }
    }
}
