import os
import json
import zipfile
import shutil
import tempfile
import urllib.request
import subprocess
from concurrent.futures import ThreadPoolExecutor

REPO_PATH = os.path.abspath(os.path.join(os.path.dirname(__file__), "Modpack-Servers"))

def run_git(args, cwd=REPO_PATH):
    result = subprocess.run(["git"] + args, cwd=cwd, capture_output=True, text=True)
    return result

def ensure_branch_clean(branch_name, orphan=False):
    # Asegurar que estamos en el branch limpio y actualizado
    run_git(["checkout", "main"])
    run_git(["pull", "origin", "main"])
    
    if orphan:
        # Borrar la rama local si existe para evitar conflictos de historial
        run_git(["branch", "-D", branch_name])
        # Crear rama huérfana limpia para no arrastrar historial ni archivos viejos
        run_git(["checkout", "--orphan", branch_name])
    else:
        # Rama normal, conservar historial
        branches_res = run_git(["branch", "-a"])
        exists_local = branch_name in branches_res.stdout
        exists_remote = f"remotes/origin/{branch_name}" in branches_res.stdout
        
        if exists_local:
            run_git(["checkout", branch_name])
            run_git(["pull", "origin", branch_name])
        elif exists_remote:
            run_git(["checkout", "-b", branch_name, f"origin/{branch_name}"])
        else:
            run_git(["checkout", "-b", branch_name])

def load_json_file(branch, filename, default_val):
    # Guardamos la rama actual para regresar
    branches_res = run_git(["branch", "--show-current"])
    current_branch = branches_res.stdout.strip()
    
    ensure_branch_clean(branch)
    
    filepath = os.path.join(REPO_PATH, filename)
    data = default_val
    if os.path.exists(filepath):
        try:
            with open(filepath, "r", encoding="utf-8") as f:
                data = json.load(f)
        except Exception as e:
            print(f"Error al leer {filename} en la rama {branch}: {e}")
            
    run_git(["checkout", current_branch])
    return data

def save_and_push_json_files(branch, files_data):
    # Guardamos la rama actual
    branches_res = run_git(["branch", "--show-current"])
    current_branch = branches_res.stdout.strip()
    
    ensure_branch_clean(branch)
    
    staged_files = []
    for filename, data in files_data.items():
        filepath = os.path.join(REPO_PATH, filename)
        with open(filepath, "w", encoding="utf-8") as f:
            json.dump(data, f, indent=2, ensure_ascii=False)
        staged_files.append(filename)
        
    for filename in staged_files:
        run_git(["add", filename])
        
    commit_res = run_git(["commit", "-m", f"update {', '.join(staged_files)}"])
    push_res = run_git(["push", "origin", branch])
    
    print(commit_res.stdout or commit_res.stderr)
    print(push_res.stdout or push_res.stderr)
    
    run_git(["checkout", current_branch])

def download_file(url, dest):
    try:
        os.makedirs(os.path.dirname(dest), exist_ok=True)
        # User agent para evitar bloqueos
        req = urllib.request.Request(url, headers={'User-Agent': 'MiLauncher-Admin/1.0'})
        with urllib.request.urlopen(req) as response, open(dest, 'wb') as out_file:
            shutil.copyfileobj(response, out_file)
        return True
    except Exception as e:
        print(f"\nError al descargar {url}: {e}")
        return False

def import_mrpack(mrpack_path, modpack_id):
    if not os.path.exists(mrpack_path):
        print("El archivo .mrpack no existe.")
        return
        
    print(f"\nProcesando modpack '{modpack_id}' desde {mrpack_path}...")
    
    with tempfile.TemporaryDirectory() as temp_dir:
        # Extraer el mrpack
        with zipfile.ZipFile(mrpack_path, 'r') as zip_ref:
            zip_ref.extractall(temp_dir)
            
        index_path = os.path.join(temp_dir, "modrinth.index.json")
        if not os.path.exists(index_path):
            print("Archivo modrinth.index.json no encontrado en el .mrpack.")
            return
            
        with open(index_path, "r", encoding="utf-8") as f:
            index_data = json.load(f)
            
        game_version = index_data.get("dependencies", {}).get("minecraft", "1.20.1")
        loader_version = index_data.get("dependencies", {}).get("fabric-loader", "")
        mod_loader = "Fabric" if loader_version else "Vanilla"
        if not loader_version:
            loader_version = index_data.get("dependencies", {}).get("forge", "")
            if loader_version:
                mod_loader = "Forge"
                
        modpack_name = index_data.get("name", modpack_id.upper())
        modpack_version = index_data.get("versionId", "1.0.0")
        
        print(f"Modpack: {modpack_name} | Versión: {modpack_version}")
        print(f"Juego: Minecraft {game_version} | Loader: {mod_loader} {loader_version}")
        
        # Recolectar descargas de mods
        files_to_download = []
        for file_info in index_data.get("files", []):
            path = file_info.get("path")
            downloads = file_info.get("downloads", [])
            env = file_info.get("env", {})
            
            # Solo descargar si es cliente requerido u opcional
            if env.get("client") != "unsupported" and downloads:
                files_to_download.append((downloads[0], os.path.join(temp_dir, path)))
                
        total_files = len(files_to_download)
        print(f"Descargando {total_files} mods de forma concurrente...")
        
        downloaded = 0
        def download_task(item):
            nonlocal downloaded
            url, dest = item
            success = download_file(url, dest)
            if success:
                downloaded += 1
                print(f"\rProgreso: {downloaded}/{total_files} mods descargados...", end="")
                
        with ThreadPoolExecutor(max_workers=8) as executor:
            executor.map(download_task, files_to_download)
            
        print("\nDescarga de mods completada.")
        
        # Procesar overrides
        overrides_dir = os.path.join(temp_dir, "overrides")
        if os.path.exists(overrides_dir):
            print("Copiando overrides...")
            for root, dirs, files in os.walk(overrides_dir):
                for file in files:
                    src_file = os.path.join(root, file)
                    rel_path = os.path.relpath(src_file, overrides_dir)
                    dest_file = os.path.join(temp_dir, rel_path)
                    os.makedirs(os.path.dirname(dest_file), exist_ok=True)
                    shutil.copy2(src_file, dest_file)
            shutil.rmtree(overrides_dir)
            
        # Preparar archivos para la rama
        ensure_branch_clean(modpack_id, orphan=True)
        
        # Limpiar directorio de la rama (excepto .git)
        for item in os.listdir(REPO_PATH):
            if item == ".git":
                continue
            item_path = os.path.join(REPO_PATH, item)
            if os.path.isdir(item_path):
                shutil.rmtree(item_path)
            else:
                os.remove(item_path)
                
        # Construir mapeo de archivos de mods a sus URLs de descarga
        mod_urls = {}
        for file_info in index_data.get("files", []):
            path = file_info.get("path")
            downloads = file_info.get("downloads", [])
            if downloads:
                mod_urls[os.path.basename(path)] = downloads[0]

        # Copiar mods
        dest_mods_dir = os.path.join(REPO_PATH, "mods")
        os.makedirs(dest_mods_dir, exist_ok=True)
        src_mods_dir = os.path.join(temp_dir, "mods")
        external_mods = []
        if os.path.exists(src_mods_dir):
            for file in os.listdir(src_mods_dir):
                src_file = os.path.join(src_mods_dir, file)
                file_size = os.path.getsize(src_file)
                if file_size > 40 * 1024 * 1024:
                    url = mod_urls.get(file, "")
                    if url:
                        external_mods.append({
                            "filename": file,
                            "url": url
                        })
                        print(f"Mod {file} es muy grande ({file_size / (1024*1024):.2f}MB). Registrado como mod externo.")
                        continue
                shutil.copy2(src_file, os.path.join(dest_mods_dir, file))
                
        # Guardar external_mods.json en la raíz de la rama si existen mods grandes
        ext_mods_path = os.path.join(REPO_PATH, "external_mods.json")
        if external_mods:
            with open(ext_mods_path, "w", encoding="utf-8") as f:
                json.dump(external_mods, f, indent=2, ensure_ascii=False)
            print(f"Creado external_mods.json con {len(external_mods)} mods.")
        else:
            if os.path.exists(ext_mods_path):
                os.remove(ext_mods_path)

        # Copiar configuraciones y otros archivos de Minecraft (config/, shaderpacks/, etc.)
        for folder in ["config", "shaderpacks", "resourcepacks"]:
            src_folder = os.path.join(temp_dir, folder)
            if os.path.exists(src_folder):
                shutil.copytree(src_folder, os.path.join(REPO_PATH, folder))
                
        # Crear mods.zip
        print("Creando mods.zip para descarga rápida...")
        mods_zip_path = os.path.join(REPO_PATH, "mods.zip")
        with zipfile.ZipFile(mods_zip_path, 'w', zipfile.ZIP_DEFLATED) as zipf:
            for file in os.listdir(dest_mods_dir):
                file_path = os.path.join(dest_mods_dir, file)
                zipf.write(file_path, arcname=file)
                
        # Si mods.zip supera los 50MB, lo eliminamos y preferimos descarga individual de mods
        if os.path.exists(mods_zip_path) and os.path.getsize(mods_zip_path) > 50 * 1024 * 1024:
            print(f"mods.zip es muy grande ({os.path.getsize(mods_zip_path)/(1024*1024):.2f}MB). Se elimina para evitar límites de GitHub; el launcher descargará mods individuales.")
            os.remove(mods_zip_path)
                
        # Crear config-modpack.json
        config_data = {
            "name": modpack_name,
            "version": modpack_version,
            "gameVersion": game_version,
            "modLoader": mod_loader,
            "loaderVersion": loader_version,
            "minMem": 2048,
            "maxMem": 4096,
            "jvmArgs": "-XX:+UseG1GC -XX:+UnlockExperimentalVMOptions",
            "wrapperCommand": "",
            "preLaunchCommand": "",
            "postExitCommand": ""
        }
        with open(os.path.join(REPO_PATH, "config-modpack.json"), "w", encoding="utf-8") as f:
            json.dump(config_data, f, indent=2, ensure_ascii=False)
            
        # Buscar fondo e icono promocionales en la carpeta original del mrpack
        src_folder_mrpack = os.path.dirname(mrpack_path)
        bg_jpg = os.path.join(src_folder_mrpack, "background.jpg")
        bg_png = os.path.join(src_folder_mrpack, "background.png")
        
        bg_ext = "png"
        bg_copied = False
        if os.path.exists(bg_jpg):
            shutil.copy2(bg_jpg, os.path.join(REPO_PATH, "background-modpack.jpg"))
            bg_ext = "jpg"
            bg_copied = True
        elif os.path.exists(bg_png):
            shutil.copy2(bg_png, os.path.join(REPO_PATH, "background-modpack.png"))
            bg_copied = True
            
        # Si no hay fondo, copiar por defecto
        if not bg_copied:
            default_bg = os.path.join(os.path.dirname(__file__), "MiLauncher", "background.png")
            if os.path.exists(default_bg):
                shutil.copy2(default_bg, os.path.join(REPO_PATH, "background-modpack.png"))
                
        # Buscar custom_icon
        default_icon = os.path.join(os.path.dirname(__file__), "MiLauncher", "program_info", "milauncher_icon.png")
        if os.path.exists(default_icon):
            shutil.copy2(default_icon, os.path.join(REPO_PATH, "custom_icon.png"))
            
        # Copiar galería de capturas si existen (image-1.jpg etc. -> image-1-modpack.png)
        for i in range(1, 11):
            img_jpg = os.path.join(src_folder_mrpack, f"image-{i}.jpg")
            img_png = os.path.join(src_folder_mrpack, f"image-{i}.png")
            if os.path.exists(img_jpg):
                shutil.copy2(img_jpg, os.path.join(REPO_PATH, f"image-{i}-modpack.jpg"))
            elif os.path.exists(img_png):
                shutil.copy2(img_png, os.path.join(REPO_PATH, f"image-{i}-modpack.png"))
                
        # Stage, commit y push de la rama
        print(f"Guardando cambios en la rama '{modpack_id}' de GitHub...")
        run_git(["add", "."])
        run_git(["commit", "-m", f"feat: import modpack files for {modpack_id}"])
        push_res = run_git(["push", "-f", "origin", modpack_id])
        print(push_res.stdout or push_res.stderr)
        
        # Volver a main
        run_git(["checkout", "main"])
        
        # Registrar en la whitelist
        print("Registrando modpack en la whitelist.json...")
        whitelist = load_json_file("whitelist", "whitelist.json", [])
        
        # Quitar duplicados si existen
        whitelist = [m for m in whitelist if m.get("id") != modpack_id]
        
        whitelist.append({
            "id": modpack_id,
            "name": modpack_name,
            "version": modpack_version,
            "description": f"Modpack oficial de {modpack_name} sincronizado desde GitHub.",
            "branch": modpack_id,
            "gameVersion": game_version,
            "modLoader": mod_loader,
            "loaderVersion": loader_version,
            "titleColor": "#FFFFFF" if modpack_id != "coblemon" else "#33CCFF",
            "titleFontSize": 44.0,
            "titleFontFamily": "Segoe UI"
        })
        
        save_and_push_json_files("whitelist", {"whitelist.json": whitelist})
        print(f"¡Modpack '{modpack_id}' importado y registrado con éxito!")

def print_separator():
    print("-" * 50)

def manage_players_menu():
    while True:
        print("\n--- GESTIÓN DE JUGADORES (WHITELIST) ---")
        print("1. Listar todos los jugadores")
        print("2. Filtrar jugadores por modpack")
        print("3. Agregar nuevo jugador")
        print("4. Eliminar jugador")
        print("5. Modificar modpacks permitidos a un jugador")
        print("6. Volver al menú principal")
        
        choice = input("Selecciona una opción: ").strip()
        if choice == "1":
            players = load_json_file("whitelist", "players.json", [])
            print_separator()
            if not players:
                print("No hay jugadores registrados.")
            for p in players:
                print(f"ID: {p.get('id')} | Nombre: {p.get('minecraft_name')} | Discord: {p.get('discord_name', 'N/A')}")
                print(f"  Modpacks permitidos: {', '.join(p.get('modpacks', []))}")
            print_separator()
        elif choice == "2":
            modpack_id = input("ID del modpack (ej. coblemon, slime): ").strip()
            players = load_json_file("whitelist", "players.json", [])
            print_separator()
            found = False
            for p in players:
                if modpack_id in p.get("modpacks", []):
                    print(f"ID: {p.get('id')} | Nombre: {p.get('minecraft_name')} | Discord: {p.get('discord_name', 'N/A')}")
                    found = True
            if not found:
                print(f"No hay jugadores con acceso al modpack '{modpack_id}'.")
            print_separator()
        elif choice == "3":
            is_premium = input("¿El jugador es Premium? (s/n): ").strip().lower() == "s"
            if is_premium:
                mc_name = input("Nombre de usuario de Minecraft Premium: ").strip()
                player_id = mc_name
            else:
                mc_name = input("Nombre de usuario de Minecraft No-Premium: ").strip()
                player_id = input("Código ID Seguro (SecureID del launcher): ").strip()
                
            discord_name = input("Usuario de Discord (opcional): ").strip()
            print("Modpacks disponibles:")
            whitelist = load_json_file("whitelist", "whitelist.json", [])
            for m in whitelist:
                print(f" - {m.get('id')} ({m.get('name')})")
                
            allowed = input("IDs de modpacks permitidos (separados por coma, ej: coblemon,slime): ").strip().split(",")
            allowed = [a.strip() for a in allowed if a.strip()]
            
            players = load_json_file("whitelist", "players.json", [])
            # Evitar duplicados
            players = [p for p in players if p.get("id") != player_id]
            players.append({
                "id": player_id,
                "minecraft_name": mc_name,
                "discord_name": discord_name,
                "modpacks": allowed
            })
            
            save_and_push_json_files("whitelist", {"players.json": players})
            print(f"Jugador {mc_name} agregado y subido con éxito.")
        elif choice == "4":
            player_id = input("ID o SecureID del jugador a eliminar: ").strip()
            players = load_json_file("whitelist", "players.json", [])
            original_len = len(players)
            players = [p for p in players if p.get("id") != player_id]
            if len(players) < original_len:
                save_and_push_json_files("whitelist", {"players.json": players})
                print("Jugador eliminado con éxito.")
            else:
                print("No se encontró ningún jugador con ese ID.")
        elif choice == "5":
            player_id = input("ID o SecureID del jugador: ").strip()
            players = load_json_file("whitelist", "players.json", [])
            player = next((p for p in players if p.get("id") == player_id), None)
            if player:
                print(f"Modpacks actuales: {', '.join(player.get('modpacks', []))}")
                allowed = input("Nuevos IDs de modpacks permitidos (separados por coma): ").strip().split(",")
                player["modpacks"] = [a.strip() for a in allowed if a.strip()]
                save_and_push_json_files("whitelist", {"players.json": players})
                print("Modpacks actualizados correctamente.")
            else:
                print("Jugador no encontrado.")
        elif choice == "6":
            break

def manage_modpacks_menu():
    while True:
        print("\n--- GESTIÓN DE MODPACKS ---")
        print("1. Listar modpacks de la whitelist")
        print("2. Importar modpack desde un archivo .mrpack")
        print("3. Eliminar modpack de la whitelist")
        print("4. Volver al menú principal")
        
        choice = input("Selecciona una opción: ").strip()
        if choice == "1":
            whitelist = load_json_file("whitelist", "whitelist.json", [])
            print_separator()
            if not whitelist:
                print("La whitelist de modpacks está vacía.")
            for m in whitelist:
                print(f"ID: {m.get('id')} | Nombre: {m.get('name')} | Versión: {m.get('version')}")
                print(f"  Minecraft: {m.get('gameVersion')} | Loader: {m.get('modLoader')} ({m.get('loaderVersion')})")
            print_separator()
        elif choice == "2":
            mrpack_path = input("Ruta al archivo .mrpack (ej. modpacks/coblemon/Cobblemon Modpack [Fabric] 1.5.2.mrpack): ").strip()
            modpack_id = input("Introduce el ID único del modpack (ej. coblemon, slime): ").strip().lower()
            import_mrpack(mrpack_path, modpack_id)
        elif choice == "3":
            modpack_id = input("ID del modpack a eliminar: ").strip()
            whitelist = load_json_file("whitelist", "whitelist.json", [])
            original_len = len(whitelist)
            whitelist = [m for m in whitelist if m.get("id") != modpack_id]
            if len(whitelist) < original_len:
                save_and_push_json_files("whitelist", {"whitelist.json": whitelist})
                print("Modpack eliminado de la whitelist con éxito.")
            else:
                print("No se encontró ningún modpack con ese ID.")
        elif choice == "4":
            break

def main():
    if not os.path.exists(REPO_PATH):
        print(f"Error: No se encontró la carpeta del repositorio Modpack-Servers en {REPO_PATH}")
        return
        
    while True:
        print("\n=== MILAUNCHER ADMIN TOOL ===")
        print("1. Gestionar jugadores de la whitelist")
        print("2. Gestionar modpacks y ramas de GitHub")
        print("3. Salir")
        
        choice = input("Selecciona una opción: ").strip()
        if choice == "1":
            manage_players_menu()
        elif choice == "2":
            manage_modpacks_menu()
        elif choice == "3":
            print("Saliendo de la herramienta de administración.")
            break

if __name__ == "__main__":
    main()
