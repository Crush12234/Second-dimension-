# M1 visual asset import contract

All M1 visual files are optional at runtime. When a file is missing, the Presentation
layer shows a readable themed fallback and does not change campaign state.

## Backdrops

Import landscape art as **Sprite (2D and UI)**, **Single**, with alpha enabled only
when the source needs it. The authored reference size is 2796 x 1290.

- `Backgrounds/ANCHOR_01_TITLE.png` — Skyhome title and New Guild
- `Backgrounds/ANCHOR_02_HALL.png` — retained original guild-annex fallback
- `Backgrounds/ANCHOR_03_APPLICANTS.png` — live Applicant Board parchment bays
- `Backgrounds/BG_GUILD_HALL_STAGE_01.png` — occupied Stage-1 Ruined Annex hub
- `Backgrounds/BG_RECRUIT_DOSSIER_ALCOVE.png` — Recruit Detail interview alcove
- `Backgrounds/BG_QUARTERMASTER_ARMORY.png` — Equipment / Quartermaster location
- `Backgrounds/BG_UNION_STRATEGY_CHAMBER.png` — Union Builder formation chamber

## Recruit portraits

Import portrait art as **Sprite (2D and UI)**, **Single**, ideally from a square
2048 x 2048 master. Transparent portrait cutouts are supported.

Lookup is presentation-only and follows this order:

1. `Portraits/Recruits/<authored-stable-or-runtime-recruit-id>.png`
2. `Portraits/PORTRAIT_SIGNATURE_01.png` through `PORTRAIT_SIGNATURE_10.png`
3. `Portraits/Seeds/<visualSeed>.png`
4. `Portraits/Races/<RACE_ID>.png`
5. A stable selection from sprites under `Portraits/Races/<RACE_ID>/`
6. A styled initials-and-race card when no image exists

File stems are uppercase stable IDs. Do not put gameplay values, rarity frames,
random rolls, or runtime image generation in these assets. The same recruit identity
must continue to resolve to the same portrait across dossier, equipment, and Union UI.

## M2 battle art

- `Battle/BG_TUTORIAL_GATEWORKS_ARENA.png` — opaque landscape arena.
- `Battle/ENEMY_GATE_GNAWER_A.png` — transparent tutorial enemy standee.
- `Battle/STANDEE_<stable-recruit-identity>.png` — transparent full-body player
  battle standee tied to the same accepted portrait authority.
- `Battle/ACTION_<stable-recruit-identity>.png` — optional transparent alternate
  combat pose crossfaded only during that member's cinematic Art.
- `Battle/ACTION_ENEMY_GATE_GNAWER_A.png` — transparent attacking pose shared by
  the tutorial Gate Gnawer members.
- `Battle/VFX_WEAPON_ARC.png`, `VFX_MYSTIC_BURST.png`,
  `VFX_RESTORATION_BLOOM.png`, and `VFX_GUARD_IMPACT.png` — transparent reusable
  cinematic action overlays. They are presentation-only and never enter a state hash.

Import standees as **Sprite (2D and UI)**, **Single**, with alpha preserved and
mesh type **Full Rect**. Preserve aspect; do not crop feet, weapons, or silhouette.
Action poses use the same import settings and identity contract as their matching
standees. A missing action pose falls back to animated movement of the idle standee.
The arena may be Texture2D or Sprite (Single). `M1VisualAssets` also supports a
runtime Texture2D-to-Sprite fallback, so an importer default cannot block battle
entry. Missing standees fall back to the approved portrait and then a deterministic
class/race card. No missing artwork changes combat state.

Import VFX textures with alpha preserved, clamp wrapping, and no readable text.
The runtime Texture2D-to-Sprite fallback also applies to VFX, so the guarded update
does not depend on package-owned per-bitmap importer metadata.
