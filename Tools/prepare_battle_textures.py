"""Extract original packed texture bytes without changing source Blender files."""
import bpy
import json
from pathlib import Path

source = Path(r"C:\Users\Xuyn\Downloads\modela")
output = Path(r"D:\My project (2)\Assets\MonsterArena\Models\BattleReady")
for folder, filename in [("ShadowFox", "MonsterArenaFox_Source.blend"), ("Trainer", "character_Source.blend")]:
    bpy.ops.wm.open_mainfile(filepath=str(source / filename), load_ui=False, use_scripts=False)
    target = output / folder / "Textures"
    target.mkdir(parents=True, exist_ok=True)
    for image in bpy.data.images:
        if not image.packed_file:
            continue
        data = bytes(image.packed_file.data)
        extension = ".png" if data.startswith(b"\x89PNG") else ".jpg" if data.startswith(b"\xff\xd8") else None
        if extension is None:
            raise RuntimeError("Unsupported packed image format: " + image.name)
        path = target / (image.name + extension)
        if path.exists() and path.read_bytes() != data:
            raise RuntimeError("Refusing to overwrite different texture: " + str(path))
        path.write_bytes(data)
        print("EXTRACTED=" + str(path))
    for material in bpy.data.materials:
        if material.use_nodes:
            print("MATERIAL_LINKS=" + json.dumps([{
                "from": link.from_node.name, "from_socket": link.from_socket.name,
                "image": link.from_node.image.name if link.from_node.type == "TEX_IMAGE" and link.from_node.image else None,
                "to": link.to_node.name, "to_socket": link.to_socket.name
            } for link in material.node_tree.links]))
