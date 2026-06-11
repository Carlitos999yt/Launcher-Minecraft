using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MiLauncher.Models
{
    public class PlayerSkin : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string _id = System.Guid.NewGuid().ToString();
        public string Id
        {
            get => _id;
            set { if (_id != value) { _id = value; OnPropertyChanged(); } }
        }

        private string _name = "aspecto sin nombre";
        public string Name
        {
            get => _name;
            set { if (_name != value) { _name = value; OnPropertyChanged(); } }
        }

        private string _skinPath = string.Empty;
        public string SkinPath
        {
            get => _skinPath;
            set { if (_skinPath != value) { _skinPath = value; OnPropertyChanged(); } }
        }

        private string _capePath = string.Empty;
        public string CapePath
        {
            get => _capePath;
            set { if (_capePath != value) { _capePath = value; OnPropertyChanged(); } }
        }

        private bool _isSlimModel = false;
        public bool IsSlimModel
        {
            get => _isSlimModel;
            set { if (_isSlimModel != value) { _isSlimModel = value; OnPropertyChanged(); } }
        }

        private string _previewPath = string.Empty;
        public string PreviewPath
        {
            get => _previewPath;
            set { if (_previewPath != value) { _previewPath = value; OnPropertyChanged(); } }
        }
    }
}
