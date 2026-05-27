import os
import re

filter_attr = "\n[WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation)]\n"

for root, dirs, files in os.walk("Assets"):
    for file in files:
        if file.endswith(".cs"):
            path = os.path.join(root, file)
            with open(path, "r", encoding="utf-8") as f:
                content = f.read()
            
            # Find all struct/class definitions implementing ISystem or inheriting SystemBase
            # that do NOT have [WorldSystemFilter] directly above them or anywhere in the class attributes.
            
            # We'll just do a simpler pass: if the file contains ISystem or SystemBase but does NOT contain WorldSystemFilter
            # we'll add it before the struct/class. (This assumes one system per file, or we just regex replace).
            
            if "ISystem" in content or "SystemBase" in content:
                if "WorldSystemFilter" not in content and "BakingSystem" not in content:
                    # Regex to find struct/class definition
                    new_content = re.sub(r'(\s*)(public\s+|internal\s+|partial\s+)*(struct|class)\s+(\w+)\s*:\s*(ISystem|SystemBase)',
                                         lambda m: f"{m.group(1)}[Unity.Entities.WorldSystemFilter(Unity.Entities.WorldSystemFilterFlags.LocalSimulation | Unity.Entities.WorldSystemFilterFlags.ClientSimulation | Unity.Entities.WorldSystemFilterFlags.ServerSimulation)]{m.group(1)}{m.group(2) or ''}{m.group(3)} {m.group(4)} : {m.group(5)}",
                                         content)
                    if new_content != content:
                        with open(path, "w", encoding="utf-8") as f:
                            f.write(new_content)
                        print("Patched:", path)
