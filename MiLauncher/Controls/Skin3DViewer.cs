using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace MiLauncher.Controls
{
    public class Skin3DViewer : ContentControl
    {
        public static readonly DependencyProperty SkinPathProperty = DependencyProperty.Register(
            "SkinPath", typeof(string), typeof(Skin3DViewer), new PropertyMetadata(null, OnSkinChanged));

        public static readonly DependencyProperty CapePathProperty = DependencyProperty.Register(
            "CapePath", typeof(string), typeof(Skin3DViewer), new PropertyMetadata(null, OnSkinChanged));

        public static readonly DependencyProperty IsSlimModelProperty = DependencyProperty.Register(
            "IsSlimModel", typeof(bool), typeof(Skin3DViewer), new PropertyMetadata(false, OnSkinChanged));

        public static readonly DependencyProperty IsStaticProperty = DependencyProperty.Register(
            "IsStatic", typeof(bool), typeof(Skin3DViewer), new PropertyMetadata(false, OnSkinChanged));

        public static readonly DependencyProperty RotationYProperty = DependencyProperty.Register(
            "RotationY", typeof(double), typeof(Skin3DViewer), new PropertyMetadata(0.785, OnSkinChanged)); // Sureste por defecto (45 grados)

        public static readonly DependencyProperty AllowAutoRenderProperty = DependencyProperty.Register(
            "AllowAutoRender", typeof(bool), typeof(Skin3DViewer), new PropertyMetadata(true, OnSkinChanged));

        public string SkinPath { get => (string)GetValue(SkinPathProperty); set => SetValue(SkinPathProperty, value); }
        public string CapePath { get => (string)GetValue(CapePathProperty); set => SetValue(CapePathProperty, value); }
        public bool IsSlimModel { get => (bool)GetValue(IsSlimModelProperty); set => SetValue(IsSlimModelProperty, value); }
        public bool IsStatic { get => (bool)GetValue(IsStaticProperty); set => SetValue(IsStaticProperty, value); }
        public double RotationY { get => (double)GetValue(RotationYProperty); set => SetValue(RotationYProperty, value); }
        public bool AllowAutoRender { get => (bool)GetValue(AllowAutoRenderProperty); set => SetValue(AllowAutoRenderProperty, value); }

        private WebView2 _webView;
        private bool _isWebViewReady = false;
        private bool _isInitialized = false;

        private static System.Threading.Tasks.Task<CoreWebView2Environment>? _sharedEnvironmentTask;
        private static string? _cachedThreeJs;
        private static string? _cachedSkinview3dJs;

        public Skin3DViewer()
        {
            this.Unloaded += Skin3DViewer_Unloaded;
            this.IsVisibleChanged += Skin3DViewer_IsVisibleChanged;
            
            if (this.IsVisible)
            {
                TryInitialize();
            }
        }

        private void Skin3DViewer_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.IsVisible)
            {
                TryInitialize();
                UpdateModel();
                SetMemoryUsage(false);
                ResumeRendering();
                ResetPose();
            }
            else
            {
                SetMemoryUsage(true);
                PauseRendering();
            }
        }

        private void TryInitialize()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            _webView = new WebView2();
            _webView.DefaultBackgroundColor = System.Drawing.Color.Transparent;
            this.Content = _webView;

            // Arranke diferido para permitir que WPF complete el layout antes de inicializar WebView2
            Dispatcher.BeginInvoke(new Action(() => InitializeAsync()), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void SetMemoryUsage(bool low)
        {
            try
            {
                if (_isWebViewReady && _webView != null && _webView.CoreWebView2 != null)
                {
                    _webView.CoreWebView2.MemoryUsageTargetLevel = low 
                        ? CoreWebView2MemoryUsageTargetLevel.Low 
                        : CoreWebView2MemoryUsageTargetLevel.Normal;
                }
            }
            catch { }
        }

        private async void PauseRendering()
        {
            try
            {
                if (!_isWebViewReady || _webView == null) return;
                var cw = _webView.CoreWebView2;
                if (cw == null) return;

                await cw.ExecuteScriptAsync("pauseRendering();");
                
                cw = _webView?.CoreWebView2;
                if (cw == null) return;
                await cw.TrySuspendAsync();
            }
            catch { }
        }

        private async void ResumeRendering()
        {
            try
            {
                if (!_isWebViewReady || _webView == null || !AllowAutoRender) return;
                var cw = _webView.CoreWebView2;
                if (cw == null) return;

                try
                {
                    cw.Resume();
                }
                catch { }

                cw = _webView?.CoreWebView2;
                if (cw == null) return;
                await cw.ExecuteScriptAsync("resumeRendering();");
            }
            catch { }
        }

        public async void ResetPose()
        {
            try
            {
                if (!_isWebViewReady || _webView == null) return;
                var cw = _webView.CoreWebView2;
                if (cw == null) return;

                await cw.ExecuteScriptAsync("resetViewerPose();");
            }
            catch { }
        }

        private void Skin3DViewer_Unloaded(object sender, RoutedEventArgs e)
        {
            Dispose();
        }

        public void Dispose()
        {
            if (!_isInitialized) return;
            _isInitialized = false;
            _isWebViewReady = false;

            if (_webView != null)
            {
                try
                {
                    this.Content = null;
                    _webView.Dispose();
                }
                catch { }
                _webView = null;
            }
        }

        private async void InitializeAsync()
        {
            try
            {
                if (_sharedEnvironmentTask == null)
                {
                    string localUserDataFolder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MiLauncher", "WebView2");
                    System.IO.Directory.CreateDirectory(localUserDataFolder);
                    
                    // Optimizar consumo de RAM limitando heap JS a 64MB y caché de disco a 10MB
                    var options = new CoreWebView2EnvironmentOptions(
                        additionalBrowserArguments: "--js-flags=\"--max-old-space-size=64\" --disk-cache-size=10485760 --disable-background-networking --disable-logging"
                    );
                    _sharedEnvironmentTask = CoreWebView2Environment.CreateAsync(null, localUserDataFolder, options);
                }
                var env = await _sharedEnvironmentTask;
                if (_webView == null) return;
                await _webView.EnsureCoreWebView2Async(env);
                
                if (_webView == null) return;
                _isWebViewReady = true;

                // Aplicar estado inicial de recursos y renderizado
                SetMemoryUsage(!this.IsVisible || !AllowAutoRender);

                string threeJs = "";
                string skinview3dJs = "";
                try
                {
                    if (_cachedThreeJs == null || _cachedSkinview3dJs == null)
                    {
                        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                        string threePath = Path.Combine(baseDir, "Resources", "three.min.js");
                        string skinviewPath = Path.Combine(baseDir, "Resources", "skinview3d.bundle.js");
                        if (File.Exists(threePath)) _cachedThreeJs = File.ReadAllText(threePath);
                        if (File.Exists(skinviewPath)) _cachedSkinview3dJs = File.ReadAllText(skinviewPath);
                    }
                    threeJs = _cachedThreeJs ?? "";
                    skinview3dJs = _cachedSkinview3dJs ?? "";
                }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error reading local JS files: " + ex.Message);
            }

            string html = @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <title>Skin Viewer</title>
    <style>
        body, html { margin: 0; padding: 0; width: 100%; height: 100%; overflow: hidden; background: transparent; }
        canvas { display: block; outline: none; }
    </style>
</head>
<body>
    <canvas id=""skin_container""></canvas>
    
    <script>" + threeJs + @"</script>
    <script>" + skinview3dJs + @"</script>
    
    <script>
        let skinViewer = new skinview3d.SkinViewer({
            canvas: document.getElementById(""skin_container""),
            width: window.innerWidth,
            height: window.innerHeight,
            skin: null,
            preserveDrawingBuffer: true // Requerido para capturar el canvas como imagen
        });
        
        // Configuración visual
        skinViewer.zoom = 0.9;
        
        let orbitControls = null;
        let walkAnimEnabled = true;

        function setAnimate(animate) {
            walkAnimEnabled = animate;
            if (!skinViewer.animation) {
                skinViewer.animation = new skinview3d.WalkingAnimation();
            }
            skinViewer.animation.speed = 0.7;
            skinViewer.animation.paused = false;
        }

        function setInteractive(interactive) {
            if (interactive) {
                if (!orbitControls) {
                    orbitControls = skinview3d.createOrbitControls(skinViewer);
                }
                orbitControls.enableRotate = true;
                orbitControls.enableZoom = true;
                orbitControls.enablePan = false;
            } else {
                if (orbitControls) {
                    orbitControls.enableRotate = false;
                    orbitControls.enableZoom = false;
                    orbitControls.enablePan = false;
                }
            }
        }

        function setRotationY(rad) {
            if (skinViewer) {
                skinViewer.playerObject.rotation.y = rad;
                if (orbitControls) {
                    orbitControls.update();
                }
            }
        }

        // Suscribirse al render loop para posicionar y animar la capa dinámicamente
        skinViewer.on(""render"", () => {
            try {
                if (skinViewer && skinViewer.playerObject) {
                    let targetRotation = 0.8;
                    if (walkAnimEnabled && skinViewer.animation && !skinViewer.animation.paused) {
                        let time = Date.now() / 1000;
                        targetRotation = 0.8 + Math.sin(time * 5) * 0.1;
                    }
                    if (skinViewer.playerObject.cape) {
                        skinViewer.playerObject.cape.rotation.x = targetRotation;
                    }
                    skinViewer.playerObject.traverse(function(child) {
                        if (child.name === ""cape"") {
                            child.rotation.x = targetRotation;
                        }
                    });
                }
            } catch(e) {}
        });

        // Valores iniciales por defecto
        setAnimate(true);
        setInteractive(true);
        setRotationY(0.785); // Sureste por defecto (45 grados)

        // Ajuste responsivo
        window.addEventListener(""resize"", () => {
            if (skinViewer) {
                skinViewer.width = window.innerWidth;
                skinViewer.height = window.innerHeight;
            }
        });

        function pauseRendering() {
            if (skinViewer) {
                skinViewer.renderPaused = true;
                if (skinViewer.animation) {
                    skinViewer.animation.paused = true;
                }
            }
        }

        function resumeRendering() {
            if (skinViewer) {
                skinViewer.renderPaused = false;
                if (skinViewer.animation) {
                    skinViewer.animation.paused = !walkAnimEnabled;
                }
            }
        }

        function forceResize() {
            if (skinViewer) {
                skinViewer.width = window.innerWidth;
                skinViewer.height = window.innerHeight;
            }
        }

        function resetViewerPose() {
            if (skinViewer) {
                skinViewer.resetCameraPose();
                skinViewer.zoom = 0.9;
                setRotationY(0.785);
                if (orbitControls) {
                    orbitControls.target.set(0, 0, 0);
                    orbitControls.update();
                }
                skinViewer.render();
            }
        }

        // Funciones que llamaremos desde C#
        function updateSkin(dataUri, isSlim) {
            if(skinViewer && dataUri) {
                skinViewer.loadSkin(dataUri, { model: isSlim ? ""slim"" : ""classic"" });
            }
        }

        // Funciones que llamaremos desde C#
        function updateCape(dataUri) {
            if(skinViewer) {
                if(dataUri) {
                    skinViewer.loadCape(dataUri);
                } else {
                    skinViewer.resetCape();
                }
            }
        }

        function makeStaticImage() {
            // Esperar un momento a que las texturas carguen y WebGL dibuje
            setTimeout(() => {
                try {
                    if (!skinViewer) return;

                    // Detener la animación de caminata en la pose del frame 0.3
                    if (skinViewer.animation) {
                        skinViewer.animation.progress = 0.3;
                        skinViewer.animation.paused = true;
                    }

                    // Ajustar pose de la capa levantada (ángulo positivo para que vaya hacia atrás)
                    if (skinViewer.playerObject.cape) {
                        skinViewer.playerObject.cape.rotation.x = 0.8;
                    }
                    skinViewer.playerObject.traverse(function(child) {
                        if (child.name === ""cape"") {
                            child.rotation.x = 0.8;
                        }
                    });

                    // Forzar renderizado final con la pose congelada y la capa levantada
                    skinViewer.render();

                    // Capturar el canvas como una imagen PNG base64
                    let dataUrl = skinViewer.canvas.toDataURL(""image/png"");

                    // Crear y colocar elemento img HTML plano
                    let img = document.createElement(""img"");
                    img.src = dataUrl;
                    img.style.width = ""100%"";
                    img.style.height = ""100%"";
                    img.style.objectFit = ""contain"";
                    document.body.appendChild(img);

                    // Ocultar y remover el canvas del DOM
                    let canvas = document.getElementById(""skin_container"");
                    if (canvas) {
                        canvas.style.display = ""none"";
                        canvas.remove();
                    }

                    // Destruir por completo la instancia de skinview3d y liberar memoria GPU
                    skinViewer.dispose();
                    skinViewer = null;
                    orbitControls = null;
                } catch(e) {
                    console.error(""Error al generar imagen estática: "", e);
                }
            }, 200);
        }
    </script>
</body>
</html>";
            _webView.CoreWebView2.NavigationCompleted += async (s, e) =>
            {
                UpdateModel();
                try
                {
                    if (this.IsVisible && AllowAutoRender)
                    {
                        var cw = _webView?.CoreWebView2;
                        if (cw != null)
                        {
                            await cw.ExecuteScriptAsync("resumeRendering();");
                        }
                    }
                    else
                    {
                        var cw = _webView?.CoreWebView2;
                        if (cw != null)
                        {
                            await cw.ExecuteScriptAsync("pauseRendering();");
                        }
                    }
                }
                catch { }
            };
            _webView.NavigateToString(html);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error in InitializeAsync: " + ex.Message);
            }
        }

        private static void OnSkinChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Skin3DViewer viewer) viewer.UpdateModel();
        }

        private async void UpdateModel()
        {
            try
            {
                if (!_isWebViewReady || _webView == null) return;
                var cw = _webView.CoreWebView2;
                if (cw == null) return;

                // Forzar sincronización de dimensiones al actualizar el modelo
                await cw.ExecuteScriptAsync("forceResize();");

                cw = _webView?.CoreWebView2;
                if (cw == null) return;
                // Configurar interactividad, animación y rotación primero
                await cw.ExecuteScriptAsync($"setAnimate({(!IsStatic && AllowAutoRender).ToString().ToLower()});");
                
                cw = _webView?.CoreWebView2;
                if (cw == null) return;
                await cw.ExecuteScriptAsync($"setInteractive({(!IsStatic && AllowAutoRender).ToString().ToLower()});");
                
                cw = _webView?.CoreWebView2;
                if (cw == null) return;
                await cw.ExecuteScriptAsync($"setRotationY({RotationY.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture)});");

                string skinDataUri = "null";
                if (!string.IsNullOrEmpty(SkinPath) && File.Exists(SkinPath))
                {
                    byte[] bytes = File.ReadAllBytes(SkinPath);
                    skinDataUri = $"'data:image/png;base64,{Convert.ToBase64String(bytes)}'";
                }
                else
                {
                    // Fallback oficial local de Steve si no hay skin.
                    string defaultPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Images", "default_skin.png");
                    if (File.Exists(defaultPath))
                    {
                        byte[] bytes = File.ReadAllBytes(defaultPath);
                        skinDataUri = $"'data:image/png;base64,{Convert.ToBase64String(bytes)}'";
                    }
                }
                
                cw = _webView?.CoreWebView2;
                if (cw == null) return;
                await cw.ExecuteScriptAsync($"updateSkin({skinDataUri}, {IsSlimModel.ToString().ToLower()});");

                string capeDataUri = "null";
                if (!string.IsNullOrEmpty(CapePath) && CapePath != "Sin capa")
                {
                    string capesDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Capes");
                    string path = System.IO.Path.Combine(capesDir, CapePath + ".png");
                    
                    if (File.Exists(path))
                    {
                        try
                        {
                            byte[] bytes = File.ReadAllBytes(path);
                            capeDataUri = $"'data:image/png;base64,{Convert.ToBase64String(bytes)}'";
                        }
                        catch { }
                    }
                    else if (File.Exists(CapePath))
                    {
                        byte[] bytes = File.ReadAllBytes(CapePath);
                        capeDataUri = $"'data:image/png;base64,{Convert.ToBase64String(bytes)}'";
                    }
                }
                
                cw = _webView?.CoreWebView2;
                if (cw == null) return;
                await cw.ExecuteScriptAsync($"updateCape({capeDataUri});");

                if (IsStatic)
                {
                    cw = _webView?.CoreWebView2;
                    if (cw == null) return;
                    await cw.ExecuteScriptAsync("makeStaticImage();");
                }
                else if (!AllowAutoRender || !this.IsVisible)
                {
                    cw = _webView?.CoreWebView2;
                    if (cw == null) return;
                    await cw.ExecuteScriptAsync("pauseRendering();");
                }
            }
            catch
            {
                // Silenciar excepciones al reciclar la vista (cierre asíncrono o nulo)
            }
        }

        public async System.Threading.Tasks.Task<string> CaptureScreenshotAsync()
        {
            try
            {
                if (!_isWebViewReady || _webView == null) return null;
                var cw = _webView.CoreWebView2;
                if (cw == null) return null;

                string script = @"
                    (function() {
                        if (typeof skinViewer === 'undefined' || !skinViewer) return '';
                        
                        // Guardar rotación y estado de la cámara viejos
                        let oldRotX = skinViewer.playerObject.rotation.x;
                        let oldRotY = skinViewer.playerObject.rotation.y;
                        let oldRotZ = skinViewer.playerObject.rotation.z;
                        
                        let oldCamPosition = skinViewer.camera.position.clone();
                        let oldCamQuaternion = skinViewer.camera.quaternion.clone();
                        
                        let oldTargetX = 0, oldTargetY = 0, oldTargetZ = 0;
                        if (typeof orbitControls !== 'undefined' && orbitControls) {
                            oldTargetX = orbitControls.target.x;
                            oldTargetY = orbitControls.target.y;
                            oldTargetZ = orbitControls.target.z;
                            // Desactivar temporalmente orbitControls para que no interfiera
                            orbitControls.enabled = false;
                        }
                        
                        // Forzar posición y rotación de cámara predeterminadas
                        skinViewer.resetCameraPose();
                        
                        // Forzar rotación y pose estándar de previsualización (Sureste 45º)
                        skinViewer.playerObject.rotation.set(0, 0.785, 0);
                        
                        let oldPaused = false;
                        let oldProgress = 0;
                        if (skinViewer.animation) {
                            oldPaused = skinViewer.animation.paused;
                            oldProgress = skinViewer.animation.progress;
                            skinViewer.animation.progress = 0.3;
                            skinViewer.animation.paused = true;
                        }
                        
                        if (skinViewer.playerObject.cape) {
                            skinViewer.playerObject.cape.rotation.x = 0.8;
                        }
                        skinViewer.playerObject.traverse(function(child) {
                            if (child.name === 'cape') {
                                child.rotation.x = 0.8;
                            }
                        });
                        
                        skinViewer.render();
                        let url = skinViewer.canvas.toDataURL('image/png');
                        
                        // Restaurar rotaciones y estado original
                        skinViewer.playerObject.rotation.set(oldRotX, oldRotY, oldRotZ);
                        if (skinViewer.animation) {
                            skinViewer.animation.progress = oldProgress;
                            skinViewer.animation.paused = oldPaused;
                        }
                        
                        // Restaurar cámara y re-habilitar orbitControls
                        skinViewer.camera.position.copy(oldCamPosition);
                        skinViewer.camera.quaternion.copy(oldCamQuaternion);
                        if (typeof orbitControls !== 'undefined' && orbitControls) {
                            orbitControls.target.set(oldTargetX, oldTargetY, oldTargetZ);
                            orbitControls.enabled = true;
                            orbitControls.update();
                        }
                        
                        return url;
                    })()";

                string resultJson = await cw.ExecuteScriptAsync(script);
                if (string.IsNullOrEmpty(resultJson) || resultJson == "null" || resultJson == "\"\"") return null;

                string dataUrl = System.Text.Json.JsonSerializer.Deserialize<string>(resultJson);
                return dataUrl;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error capturing screenshot: " + ex.Message);
                return null;
            }
        }
    }
}
