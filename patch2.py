import os
import re

pattern_sys = re.compile(r"((?:\[.*?\]\s*)*)(\s*)((?:public\s+|internal\s+|partial\s+|unsafe\s+|sealed\s+|abstract\s+)*)(struct|class)\s+(\w+)\s*:\s*(ISystem|SystemBase)", re.DOTALL)

for d in ["Assets", "Packages"]:
    for root, dirs, files in os.walk(d):
        for f in files:
            if f.endswith(".cs"):
                path = os.path.join(root, f)
                with open(path, "r", encoding="utf-8") as file:
                    content = file.read()
                
                new_content = content
                
                # We need to iterate over all matches and replace them if they lack the filter
                def repl(m):
                    attrs = m.group(1)
                    if "WorldSystemFilter" not in attrs and "BakingSystem" not in attrs:
                        filter_attr = "[Unity.Entities.WorldSystemFilter(Unity.Entities.WorldSystemFilterFlags.LocalSimulation | Unity.Entities.WorldSystemFilterFlags.ClientSimulation | Unity.Entities.WorldSystemFilterFlags.ServerSimulation)]\n"
                        # Insert before the access modifiers
                        return f"{attrs}{m.group(2)}{filter_attr}{m.group(3)}{m.group(4)} {m.group(5)} : {m.group(6)}"
                    return m.group(0)

                new_content = pattern_sys.sub(repl, content)
                
                if new_content != content:
                    with open(path, "w", encoding="utf-8") as file:
                        file.write(new_content)
                    print(f"Patched: {path}")
