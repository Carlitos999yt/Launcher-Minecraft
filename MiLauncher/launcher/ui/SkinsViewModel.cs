using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media;
using MiLauncher.Models;
using MiLauncher.launcher.tools;
using MiLauncher.launcher.minecraft;
using MiLauncher.launcher.net;
using MiLauncher.launcher.settings;
using MiLauncher.launcher.launch;
using Microsoft.Win32;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace MiLauncher.launcher.ui
{
    public class SkinsViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainViewModel;
        
        public ICommand GoBackCommand { get; }
        public ICommand NewSkinCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand SaveSkinCommand { get; }
        public ICommand BrowseSkinCommand { get; }
        public ICommand SelectSkinCommand { get; }
        public ICommand SelectCapeCommand { get; }
        
        // CRUD Commands
        public ICommand DeleteSkinCommand { get; }
        public ICommand DuplicateSkinCommand { get; }
        public ICommand EditExistingSkinCommand { get; }

        private bool _isEditing;
        public bool IsEditing
        {
            get => _isEditing;
            set => SetProperty(ref _isEditing, value);
        }

        public Func<System.Threading.Tasks.Task<string>>? CaptureScreenshotFunc { get; set; }

        public ObservableCollection<PlayerSkin> SavedSkins { get; }
        public ObservableCollection<CapeModel> AvailableCapes { get; }
        
        private PlayerSkin _currentSkin;
        public PlayerSkin CurrentSkin
        {
            get => _currentSkin;
            set 
            {
                if (SetProperty(ref _currentSkin, value))
                {
                    OnPropertyChanged(nameof(CurrentFaceImage));
                    OnPropertyChanged(nameof(CurrentBodyImage));
                    _mainViewModel.UpdateProfileFace(_currentSkin?.SkinPath);
                    SkinManager.SaveSkins(SavedSkins, _currentSkin);
                }
            }
        }

        public ImageSource CurrentFaceImage => _currentSkin != null ? SkinProcessor.GetFaceFromSkin(_currentSkin.SkinPath) : null;
        public ImageSource CurrentBodyImage => _currentSkin != null ? SkinRenderer.GetFrontBody(_currentSkin.SkinPath, _currentSkin.CapePath, _currentSkin.IsSlimModel) : null;

        // Editor properties
        private PlayerSkin _editingSkin;
        
        private string _editName;
        public string EditName { get => _editName; set => SetProperty(ref _editName, value); }
        
        private string _editSkinPath;
        public string EditSkinPath 
        { 
            get => _editSkinPath; 
            set 
            {
                if(SetProperty(ref _editSkinPath, value))
                {
                    OnPropertyChanged(nameof(EditFaceImage));
                    OnPropertyChanged(nameof(EditBodyImage));
                    OnPropertyChanged(nameof(EditSkinFileName));
                }
            } 
        }
        
        public string EditSkinFileName => string.IsNullOrEmpty(EditSkinPath) ? "Seleccionar archivo de aspecto..." : Path.GetFileName(EditSkinPath);
        
        public ImageSource EditFaceImage => !string.IsNullOrEmpty(_editSkinPath) ? SkinProcessor.GetFaceFromSkin(_editSkinPath) : null;
        public ImageSource EditBodyImage => !string.IsNullOrEmpty(_editSkinPath) ? SkinRenderer.GetFrontBody(_editSkinPath, _editCapePath, IsSlimModel) : null;

        private string _editCapePath;
        public string EditCapePath 
        { 
            get => _editCapePath; 
            set 
            {
                if(SetProperty(ref _editCapePath, value))
                {
                    OnPropertyChanged(nameof(EditBodyImage));
                }
            } 
        }

        private bool _isSlimModel;
        public bool IsSlimModel 
        { 
            get => _isSlimModel; 
            set 
            {
                if(SetProperty(ref _isSlimModel, value))
                {
                    OnPropertyChanged(nameof(EditBodyImage));
                }
            } 
        }

        public SkinsViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            
            // Load saved data
            var data = SkinManager.LoadSkins();
            SavedSkins = new ObservableCollection<PlayerSkin>(data.Skins);
            
            // Load available capes dynamically
            AvailableCapes = new ObservableCollection<CapeModel>();
            AvailableCapes.Add(new CapeModel { Name = "Sin capa", Path = "Sin capa", FullPath = null });
            
            string capesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Capes");
            if (Directory.Exists(capesDir))
            {
                var files = Directory.GetFiles(capesDir, "*.png");
                foreach (var file in files)
                {
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    AvailableCapes.Add(new CapeModel { Name = fileName.Replace("_", " "), Path = fileName, FullPath = file });
                }
            }
            
            if (!string.IsNullOrEmpty(data.CurrentSkinId))
            {
                CurrentSkin = SavedSkins.FirstOrDefault(s => s.Id == data.CurrentSkinId);
            }

            GoBackCommand = new RelayCommand(_ => 
            {
                IsEditing = false;
                _mainViewModel.NavigateHomeCommand.Execute(null);
            });
            
            NewSkinCommand = new RelayCommand(_ => 
            {
                IsEditing = true;
                _editingSkin = null;
                EditName = "aspecto sin nombre";
                EditSkinPath = "";
                EditCapePath = "Sin capa";
                IsSlimModel = false;
            });
            
            CancelEditCommand = new RelayCommand(_ => IsEditing = false);
            
            BrowseSkinCommand = new RelayCommand(_ => 
            {
                var dialog = new OpenFileDialog { Filter = "Archivos PNG (*.png)|*.png" };
                if (dialog.ShowDialog() == true)
                {
                    EditSkinPath = dialog.FileName;
                }
            });

            SelectSkinCommand = new RelayCommand(skinObj => 
            {
                if (skinObj is PlayerSkin skin)
                {
                    CurrentSkin = skin;
                }
            });

            SelectCapeCommand = new RelayCommand(cape => 
            {
                EditCapePath = cape?.ToString();
            });

            SaveSkinCommand = new RelayCommand(async _ => 
            {
                string localSkinsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MiLauncher", "Skins");
                Directory.CreateDirectory(localSkinsDir);
                
                string finalSkinPath = EditSkinPath;
                if (!string.IsNullOrEmpty(EditSkinPath) && File.Exists(EditSkinPath))
                {
                    if (!EditSkinPath.StartsWith(localSkinsDir, StringComparison.OrdinalIgnoreCase))
                    {
                        string uniqueName = Guid.NewGuid().ToString() + ".png";
                        string destPath = Path.Combine(localSkinsDir, uniqueName);
                        File.Copy(EditSkinPath, destPath, true);
                        finalSkinPath = destPath;
                    }
                }
                
                string finalPreviewPath = "";
                if (CaptureScreenshotFunc != null)
                {
                    try
                    {
                        string dataUrl = await CaptureScreenshotFunc();
                        if (!string.IsNullOrEmpty(dataUrl) && dataUrl.StartsWith("data:image/png;base64,"))
                        {
                            string base64 = dataUrl.Substring("data:image/png;base64,".Length);
                            byte[] bytes = Convert.FromBase64String(base64);
                            bytes = SkinProcessor.CropTransparentMarginsAndSave(bytes);
                            string previewsDir = Path.Combine(localSkinsDir, "Previews");
                            Directory.CreateDirectory(previewsDir);
                            string previewPath = Path.Combine(previewsDir, "preview_" + Guid.NewGuid().ToString() + ".png");
                            File.WriteAllBytes(previewPath, bytes);
                            finalPreviewPath = previewPath;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("Error capturing screenshot in SaveSkinCommand: " + ex.Message);
                    }
                }

                if (string.IsNullOrEmpty(finalPreviewPath))
                {
                    finalPreviewPath = GenerateAndSavePreview(finalSkinPath, EditCapePath, IsSlimModel);
                }

                if (_editingSkin != null)
                {
                    // Delete old skin/preview if they changed and were in local dir
                    if (_editingSkin.SkinPath != finalSkinPath && !string.IsNullOrEmpty(_editingSkin.SkinPath) && _editingSkin.SkinPath.StartsWith(localSkinsDir, StringComparison.OrdinalIgnoreCase))
                    {
                        try { File.Delete(_editingSkin.SkinPath); } catch {}
                    }
                    if (!string.IsNullOrEmpty(_editingSkin.PreviewPath) && _editingSkin.PreviewPath != finalPreviewPath && File.Exists(_editingSkin.PreviewPath))
                    {
                        try { File.Delete(_editingSkin.PreviewPath); } catch {}
                    }

                    _editingSkin.Name = EditName;
                    _editingSkin.SkinPath = finalSkinPath;
                    _editingSkin.CapePath = EditCapePath;
                    _editingSkin.IsSlimModel = IsSlimModel;
                    _editingSkin.PreviewPath = finalPreviewPath;
                }
                else
                {
                    var newSkin = new PlayerSkin 
                    { 
                        Name = EditName, 
                        SkinPath = finalSkinPath, 
                        CapePath = EditCapePath,
                        IsSlimModel = IsSlimModel,
                        PreviewPath = finalPreviewPath
                    };
                    SavedSkins.Add(newSkin);
                    CurrentSkin = newSkin;
                }
                
                // Refresh list for UI
                var temp = new List<PlayerSkin>(SavedSkins);
                SavedSkins.Clear();
                foreach (var s in temp) SavedSkins.Add(s);

                SkinManager.SaveSkins(SavedSkins, CurrentSkin);
                IsEditing = false;
                
                // Force update UI images
                OnPropertyChanged(nameof(CurrentFaceImage));
                OnPropertyChanged(nameof(CurrentBodyImage));
            });

            DeleteSkinCommand = new RelayCommand(skinObj => 
            {
                if (skinObj is PlayerSkin skin)
                {
                    SavedSkins.Remove(skin);
                    if (CurrentSkin == skin) CurrentSkin = null;
                    SkinManager.SaveSkins(SavedSkins, CurrentSkin);

                    string localSkinsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MiLauncher", "Skins");
                    if (!string.IsNullOrEmpty(skin.SkinPath) && skin.SkinPath.StartsWith(localSkinsDir, StringComparison.OrdinalIgnoreCase))
                    {
                        try { File.Delete(skin.SkinPath); } catch {}
                    }
                    if (!string.IsNullOrEmpty(skin.PreviewPath) && File.Exists(skin.PreviewPath))
                    {
                        try { File.Delete(skin.PreviewPath); } catch {}
                    }
                }
            });

            DuplicateSkinCommand = new RelayCommand(skinObj => 
            {
                if (skinObj is PlayerSkin skin)
                {
                    string localSkinsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MiLauncher", "Skins");
                    Directory.CreateDirectory(localSkinsDir);
                    
                    string duplicatedSkinPath = skin.SkinPath;
                    if (File.Exists(skin.SkinPath))
                    {
                        string uniqueName = Guid.NewGuid().ToString() + ".png";
                        duplicatedSkinPath = Path.Combine(localSkinsDir, uniqueName);
                        File.Copy(skin.SkinPath, duplicatedSkinPath, true);
                    }
                    
                    string duplicatedPreviewPath = "";
                    if (File.Exists(skin.PreviewPath))
                    {
                        string previewsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MiLauncher", "Skins", "Previews");
                        string uniquePreviewName = "preview_" + Guid.NewGuid().ToString() + ".png";
                        duplicatedPreviewPath = Path.Combine(previewsDir, uniquePreviewName);
                        File.Copy(skin.PreviewPath, duplicatedPreviewPath, true);
                    }
                    else
                    {
                        duplicatedPreviewPath = GenerateAndSavePreview(duplicatedSkinPath, skin.CapePath, skin.IsSlimModel);
                    }

                    var copy = new PlayerSkin
                    {
                        Name = skin.Name + " (Copia)",
                        SkinPath = duplicatedSkinPath,
                        CapePath = skin.CapePath,
                        IsSlimModel = skin.IsSlimModel,
                        PreviewPath = duplicatedPreviewPath
                    };
                    SavedSkins.Add(copy);
                    SkinManager.SaveSkins(SavedSkins, CurrentSkin);
                }
            });

            EditExistingSkinCommand = new RelayCommand(skinObj => 
            {
                if (skinObj is PlayerSkin skin)
                {
                    IsEditing = true;
                    _editingSkin = skin;
                    EditName = skin.Name;
                    EditSkinPath = skin.SkinPath;
                    EditCapePath = skin.CapePath;
                    IsSlimModel = skin.IsSlimModel;
                }
            });
        }

        private string GenerateAndSavePreview(string skinPath, string capePath, bool isSlim)
        {
            try
            {
                var imgSource = SkinRenderer.GetFrontBody(skinPath, capePath, isSlim);
                if (imgSource is System.Windows.Media.Imaging.BitmapSource bmpSource)
                {
                    string previewsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MiLauncher", "Skins", "Previews");
                    Directory.CreateDirectory(previewsDir);
                    string previewPath = Path.Combine(previewsDir, "preview_" + Guid.NewGuid().ToString() + ".png");
                    
                    var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                    encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bmpSource));
                    using (var stream = File.Create(previewPath))
                    {
                        encoder.Save(stream);
                    }
                    return previewPath;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error generating skin preview: " + ex.Message);
            }
            return string.Empty;
        }
    }
}
