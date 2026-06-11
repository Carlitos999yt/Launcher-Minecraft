Add-Type -AssemblyName PresentationCore
$families = [System.Windows.Media.Fonts]::GetFontFamilies("C:\Users\VERONICA\Documents\universidad1\launcher minecraft\MiLauncher\Resources\Fonts\MinecraftTen.ttf")
foreach($f in $families) {
    foreach($k in $f.FamilyNames.Keys) {
        Write-Output "$k = $($f.FamilyNames[$k])"
    }
}
