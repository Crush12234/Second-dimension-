SECOND DIMENSION — MAREN HOLT PRODUCTION 3D PROOF (UPDATE 014)
SETUP BUILD 014.5 — QUIET-PHASE / NO-RETRY-LOOP REPAIR

This folder is owned by the Update 014 character installer. On import, Unity will:

1. Configure the free CC0 Adventurer with its native Generic rig, or configure a
   portrait-matched production model as Humanoid.
2. Use the embedded CC0 animation takes by semantic name; portrait-matched
   animation files remain Humanoid-retargeted.
3. Wait for Unity's artifact pipeline to become quiet, then build an Animator
   Controller, isolated-preview character prefab, equipment mounts and proof scene
   in separate bounded phases.
4. Apply the navy / ivory / leather / steel proxy palette.
5. Open the proof scene automatically when no unsaved scene would be displaced.

An automatic failure is latched after one attempt so imports cannot trigger an
infinite rebuild loop. The manual Rebuild menu clears that latch for one safe retry.

Generated outputs appear under:
    Assets/SecondDimension/ProductionSlice014/MarenHolt/Proof

Proof scene:
    Proof/Scenes/Maren_Holt_Production3D_Proof.unity

Manual rebuild menu:
    Second Dimension > Production 3D > Rebuild Maren Proof (Update 014)

MODEL MODE

The included CC0 character is intentionally reported as PROXY in the scene overlay.
It proves the Generic rig, native motion selection, equipment, lighting and camera language;
it is not represented as the final commissioned likeness.

When a final authored Maren model replaces the proxy, add an empty file named
FINAL_MODEL.marker inside MarenHolt/Generated and rebuild. To force proxy mode, use
PROXY_MODEL.marker instead.

For the custom Meshy route, downloaded Texture_<index>_base_color / normal /
metallic / roughness maps are imported automatically. Unity preserves any character
material that already has a valid base-color texture and applies generated PBR
fallback materials only to untextured slots. Multiple texture sets are matched by
material/renderer/slot index when possible.

No Cinemachine, Input System, URP or HDRP API is required by this proof. The scripts
select a compatible lit shader at build time and otherwise use UnityEngine only.
