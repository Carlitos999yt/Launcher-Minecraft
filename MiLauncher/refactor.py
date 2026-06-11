import os
import glob

replacements = {
    "MiLauncher.Views": "MiLauncher.launcher.ui",
    "MiLauncher.ViewModels": "MiLauncher.launcher.ui",
    "MiLauncher.Services.GameLauncher": "MiLauncher.launcher.launch.LaunchController",
    "MiLauncher.Services": "MiLauncher.launcher.minecraft",
    "MiLauncher.Utilities": "MiLauncher.launcher.tools",
    "using MiLauncher.launcher.minecraft;": "using MiLauncher.launcher.minecraft;\nusing MiLauncher.launcher.net;\nusing MiLauncher.launcher.settings;\nusing MiLauncher.launcher.launch;"
}

def process_file(filepath):
    try:
        with open(filepath, 'r', encoding='utf-8') as f:
            content = f.read()
        
        original_content = content
        for old, new in replacements.items():
            content = content.replace(old, new)
            
        if content != original_content:
            with open(filepath, 'w', encoding='utf-8') as f:
                f.write(content)
            print(f"Updated: {filepath}")
    except Exception as e:
        print(f"Error processing {filepath}: {e}")

# Process all .cs and .xaml files
for root, dirs, files in os.walk('.'):
    for file in files:
        if file.endswith('.cs') or file.endswith('.xaml'):
            process_file(os.path.join(root, file))

print("Done.")
