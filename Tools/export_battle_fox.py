"""Export a Unity-oriented staging FBX from source without saving the .blend."""
import bpy
from mathutils import Quaternion

bpy.ops.wm.open_mainfile(filepath=r"C:\Users\Xuyn\Downloads\modela\MonsterArenaFox_Source.blend", load_ui=False, use_scripts=False)
for obj in bpy.context.scene.objects:
    print("SOURCE_TRANSFORM", obj.name, list(obj.rotation_euler), list(obj.scale), obj.animation_data.action.name if obj.animation_data and obj.animation_data.action else None)
    if obj.animation_data:
        obj.animation_data.action = None
        for track in obj.animation_data.nla_tracks:
            track.mute = True
    if obj.type == "ARMATURE":
        for bone in obj.pose.bones:
            bone.location = (0, 0, 0)
            bone.rotation_quaternion = Quaternion((1, 0, 0, 0))
            bone.rotation_euler = (0, 0, 0)
            bone.scale = (1, 1, 1)
bpy.context.scene.frame_set(1)
for action in bpy.data.actions:
    print("ACTION_PATHS", action.name, sorted(set(curve.data_path for curve in action.fcurves if not curve.data_path.startswith('pose.bones'))))
bpy.ops.object.select_all(action="SELECT")
bpy.ops.export_scene.fbx(
    filepath=r"D:\My project (2)\Assets\MonsterArena\Models\BattleReady\ShadowFox\ShadowFox.fbx",
    use_selection=True, object_types={"MESH", "ARMATURE"},
    add_leaf_bones=False, axis_forward="-Z", axis_up="Y",
    bake_anim=True, bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False,
    bake_anim_simplify_factor=0, path_mode="STRIP", embed_textures=False)
