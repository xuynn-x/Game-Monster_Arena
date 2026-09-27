"""Read-only Blender/FBX audit. Run with Blender --background --python ... -- paths."""
import bpy
import json
import sys

for path in sys.argv[sys.argv.index("--") + 1:]:
    if path.lower().endswith(".blend"):
        bpy.ops.wm.open_mainfile(filepath=path, load_ui=False, use_scripts=False)
    else:
        bpy.ops.wm.read_factory_settings(use_empty=True)
        bpy.ops.import_scene.fbx(filepath=path)
    result = {
        "source": path,
        "objects": [{"name": o.name, "type": o.type,
                     "dimensions": list(o.dimensions),
                     "vertices": len(o.data.vertices) if o.type == "MESH" else None,
                     "polygons": len(o.data.polygons) if o.type == "MESH" else None,
                     "triangles": sum(len(p.vertices) - 2 for p in o.data.polygons) if o.type == "MESH" else None,
                     "uv_layers": len(o.data.uv_layers) if o.type == "MESH" else None,
                     "vertex_groups": len(o.vertex_groups) if o.type == "MESH" else None,
                     "bones": len(o.data.bones) if o.type == "ARMATURE" else None,
                     "modifiers": [m.type for m in o.modifiers],
                     "materials": [s.material.name if s.material else None for s in o.material_slots]}
                    for o in bpy.context.scene.objects],
        "actions": [{"name": a.name, "frame_range": list(a.frame_range)} for a in bpy.data.actions],
        "images": [{"name": i.name, "path": i.filepath, "packed": bool(i.packed_file),
                    "size": list(i.size), "source": i.source} for i in bpy.data.images],
        "materials": [{"name": m.name, "nodes": [{"type": n.type, "image": n.image.name if n.type == "TEX_IMAGE" and n.image else None}
                        for n in m.node_tree.nodes] if m.use_nodes else []} for m in bpy.data.materials]
    }
    print("MODEL_AUDIT=" + json.dumps(result, ensure_ascii=False))
