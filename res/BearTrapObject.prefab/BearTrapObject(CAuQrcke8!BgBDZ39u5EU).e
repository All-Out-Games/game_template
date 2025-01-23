13
1662152343557
566128958130108 1729678704104935700
{
  "name": "BearTrapObject",
  "local_enabled": true,
  "local_position": {

  },
  "local_rotation": 0,
  "local_scale": {
    "X": 1,
    "Y": 1
  },
  "spawn_as_networked_entity": true
},
{
  "cid": 1,
  "aoid": "566317684344397:1729678793462220000",
  "component_type": "Mono_Component",
  "mono_component_type": "BearTrap",
  "data": {
    "Rig": "566324197286245:1729678796545939700"
  }
},
{
  "cid": 2,
  "aoid": "566324197286245:1729678796545939700",
  "component_type": "Internal_Component",
  "internal_component_type": "Spine_Animator",
  "data": {
    "skeleton_data_asset": "animations/bear_trap/RAMB132_beartrap.spine",
    "ordered_skins": [
      "default"
    ],
    "depth_offset": 3.5000000000000000,
    "mask_in_shadow": true
  }
},
{
  "cid": 3,
  "aoid": "566387435268474:1729678826487590300",
  "component_type": "Internal_Component",
  "internal_component_type": "Circle_Collider",
  "data": {
    "size": 0.5000000000000000,
    "is_trigger": true
  }
},
{
  "cid": 4,
  "aoid": "1980721022534156:1731095541855532500",
  "component_type": "Mono_Component",
  "mono_component_type": "ProjectileIgnore",
  "data": {

  }
}
