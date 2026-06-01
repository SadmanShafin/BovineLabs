import os
import re

for search_dir in ["Assets", "Packages"]:
    for root, dirs, files in os.walk(search_dir):
        for file in files:
            if file.endswith(".cs"):
                path = os.path.join(root, file)
                with open(path, "r", encoding="utf-8") as f:
                    content = f.read()
                
                if "ISystem" in content or "SystemBase" in content:
                    if "WorldSystemFilter" not in content and "BakingSystem" not in content:
                        new_content = re.sub(r'(\s*)((?:public\s+|internal\s+|partial\s+|unsafe\s+|sealed\s+|abstract\s+)*)(struct|class)\s+(\w+)\s*:\s*(ISystem|SystemBase)',
                                             lambda m: f"{m.group(1)}[Unity.Entities.WorldSystemFilter(Unity.Entities.WorldSystemFilterFlags.LocalSimulation | Unity.Entities.WorldSystemFilterFlags.ClientSimulation | Unity.Entities.WorldSystemFilterFlags.ServerSimulation)]{m.group(1)}{m.group(2) or ''}{m.group(3)} {m.group(4)} : {m.group(5)}",
                                             content)
                        if new_content != content:
                            with open(path, "w", encoding="utf-8") as f:
                                f.write(new_content)
                            print("Patched:", path)
