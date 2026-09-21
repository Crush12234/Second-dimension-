# Enemy Art 700 recovered pack provenance

Status: `PENDING_RIGHTS_REVIEW` for internal review builds. This record does not declare commercial-use clearance.

## Supplied source

- Supplied directly by the project owner on 2026-09-06 as 25 split archive parts, `ENEMY_ART_700.zip.part001` through `ENEMY_ART_700.zip.part025`, together with `reassemble_enemy700.py` and `REASSEMBLY_MANIFEST.json`.
- Reassembled archive: `SECOND_DIMENSION_ENEMY_ART_700_RECOVERED_V2.zip`
- Reassembled archive bytes: `1,175,099,715`
- Reassembled archive SHA-256: `D8224272A5D8D1F9F0ED83DDEC7CEE509B241C4CD56EB12AFDA250D725E523A8`
- Pack checksum ledger SHA-256 (`FILES.sha256`): `2CAABC6F555DED9FDEB733921FA0B9588E3584719E4E835C556B53082CAE13DA`
- Pack manifest SHA-256 (`PACKAGE_MANIFEST.json`): `3B6912A2967CA00343C9639D285AB4995751DC5E03F7F2F561DF99B1E55DE39C`

The source pack's own metadata describes records 001-030 as canonical Drive bases and 031-070 as project-owner-approved chat art. That description is retained as provenance metadata only; it is not treated as license evidence.

## Imported runtime boundary

Only the bounded `UNITY_DROP_IN/Assets/SecondDimension/EnemyArt700` payload was imported into the Unity project:

- 70 base enemy families.
- 10 appearances per family, for 700 stable appearance records.
- 1,400 battle sprites: one idle and one attack sprite per appearance.
- 700 portraits.
- Two JSON catalog/index files.
- 2,102 non-meta runtime files total.

Source pairs, QA utilities, recovery history, reference data, and authoring tools remain outside the live Unity `Assets` tree. The pack reports 193 byte-identical recovered triplets and 507 deterministic derived composite/color/effect variants; it does not claim 700 distinct species or newly redrawn anatomy.

## Local verification

- All 2,190 files in the supplied pack matched its checksum ledger.
- All 2,100 imported PNGs decoded successfully.
- The runtime catalog validated exactly 70 families by 10 variants, with no missing idle, attack, or portrait file.
- Automated contact-sheet QA drew all 2,100 sprite roles through the shipping runtime loader.

Verification evidence is under `BuildEvidence/EnemyArt700`. The Windows release manifest records the exact hashes of every copied runtime file after the Unity post-build copy.

## Rights action before external commercial distribution

Attach the original creator/provider records, applicable generation or source terms, and project-owner/studio approval for the 70 base sources. Until that review is complete, this family remains `PENDING_RIGHTS_REVIEW` even though its binary integrity and local origin chain are verified.
