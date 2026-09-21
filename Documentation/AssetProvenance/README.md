# Asset provenance status

This folder is the canonical release-rights index for project-owned media in **Second Dimension - Guild of Worlds**. The machine-readable record is [`asset-provenance.v1.json`](asset-provenance.v1.json); the root [`THIRD_PARTY_NOTICES.txt`](../../THIRD_PARTY_NOTICES.txt) preserves verified attribution and source details that were previously held only in the Final030 archive.

The ledger is deliberately conservative. A filename, an `original` or `production` label, presence in a build, or a generated-asset note does not prove commercial-use rights. Only entries marked `VERIFIED_COMMERCIAL_USE` **and** `APPROVED_FOR_SHIP` are cleared by this record. Everything else remains an internal-build candidate until the missing evidence and approval are attached.

## Status meanings

| Status | Meaning | Release action |
|---|---|---|
| `APPROVED_FOR_SHIP` | Source, rights basis, binary hash, and studio approval are recorded. | May ship under the recorded terms. |
| `PENDING_RIGHTS_REVIEW` | Some local origin evidence exists, but rights evidence or approval is incomplete. | Keep internal; complete review before distribution. |
| `REPLACEMENT_REQUIRED` | Prototype, placeholder, schematic, or derived fallback. | Replace before presentation/release even if its license is permissive. |
| `EXCLUDED_CACHE` | Generated Unity cache or package material. | Never harvest or treat as authored source content. |

## Current family matrix

| Family | Locally verified evidence | Rights status | Presentation status | Next action |
|---|---|---|---|---|
| Enemy Art 700 recovered pack (70 families x 10 appearances) | Project-owner-supplied 25-part archive, exact archive/checksum-manifest hashes, complete 2,190-file checksum audit, 2,100-PNG decode, strict runtime catalog audit, and 700-appearance contact sheets. | `PENDING_RIGHTS_REVIEW` | Integrated internal-build enemy presentation set with paired idle/attack sprites and portraits. | Attach creator/provider terms and studio approval for the 70 base sources before external commercial distribution. |
| Campaign083 Tower plates (10 PNGs) | Adjacent provenance table, exact hashes, dimensions, and source artifact IDs for floors 2-10; floor 1 has a verified binary but no retained source filename. | `PENDING_RIGHTS_REVIEW` | Strong reusable floor/background set. | Attach provider/creator terms and approval; recover floor 1 source record if possible. |
| Board086 room plates (6 PNGs) | Adjacent ImageGen record, generation date, exact hashes/sizes/dimensions, and a source artifact filename for every plate; no external/reference input. | `PENDING_RIGHTS_REVIEW` | Original board-room reveal backdrops. | Attach applicable provider terms and studio release approval. |
| Race portrait variants 086 (12 PNGs) | Adjacent ImageGen record, generation date, exact hashes/sizes/dimensions, and a source artifact filename for every portrait; no external/reference or franchise/person input. | `PENDING_RIGHTS_REVIEW` | Original race/class-readable recruitment portraits. | Attach applicable provider terms and studio release approval. |
| Battle086 enemy standees (8 PNGs) | Adjacent ImageGen record documents each two-stage original generation (initial source plus flat-green background-isolation edit), deterministic `CheckerboardAlphaExtractor076` alpha cleanup, final hashes/sizes/dimensions, and pixel/visual inspection; no external/reference, franchise, or real-person input. | `PENDING_RIGHTS_REVIEW` | Original transparent production candidates for eight previously unillustrated Pass03 enemy families. | Attach applicable provider terms and studio release approval. |
| Gateworks battle background | Final030 source record and exact hash match. | `PENDING_RIGHTS_REVIEW` | Reusable after review. | Attach generation job/provider-terms evidence and studio approval. |
| Gate Gnawer Scout/Bulwark cutouts | Final030 source record and exact hashes match. | `PENDING_RIGHTS_REVIEW` | `REPLACEMENT_REQUIRED` prototype art. | Replace with final enemy art; retain hashes for traceability. |
| Quaternius Maren/equipment FBXs | Final030 URLs, CC0 1.0 basis, and exact hashes match. | `VERIFIED_COMMERCIAL_USE` | `REPLACEMENT_REQUIRED` pipeline proxies. | Preserve notice; do not present as final Maren or final equipment. |
| FirstHour076 portraits and battle standees | Detailed local build-evidence records exist, but source portrait rights and complete shipping hashes do not. | `PENDING_RIGHTS_REVIEW` | Candidate production art. | Add the original creation record, provider terms, and per-file hashes. |
| First-hour environments, Battle075, and ChapterTwo079 visuals | Inventory calls them original production assets; no primary rights chain was found. | `PENDING_RIGHTS_REVIEW` | High-impact reusable art if cleared. | Record creator/job/receipt, terms, hashes, and approval. |
| Recruit and applicant portraits | No primary creator/job/license record was found; six applicant portraits are reused as race fallbacks. | `PENDING_RIGHTS_REVIEW` | Repetition/identity risk. | Create unique final portraits and record provenance at creation time. |
| Battle011 audio | Runtime cue mapping exists; source/license evidence was not found. | `PENDING_RIGHTS_REVIEW` | Potentially reusable after audit. | Identify each source, record license/receipt, and hash files. |
| GuildCity017F and Campaign021 audio | Local manifests explicitly label cues placeholder/replaceable. | Unverified | `REPLACEMENT_REQUIRED` | Replace with licensed final audio and add cue-level provenance. |
| Schematic/proxy UI art and derived pose fallbacks | Local manifests identify schematic, symbolic, proxy, or transform-derived content. | Unverified | `REPLACEMENT_REQUIRED` | Replace rather than promoting to final art. |
| Unity `Library` / `PackageCache` trees | Generated cache only. | `EXCLUDED_CACHE` | Not source content. | Exclude from asset discovery, reuse, and release ledgers. |

## Recording new or cleared assets

For every shipping media file, record its project-relative path, SHA-256, creator/provider, original source or generation job, license/terms evidence, attribution requirement, intended use, reviewer, review date, and release decision. Generated assets must retain their job/artifact identifier and the applicable provider-terms snapshot; third-party assets must retain the original download page, license text, and receipt where applicable.

Keep private receipts or account records outside `Assets/StreamingAssets`. The manifest may reference an internal evidence ID or controlled documentation path, but no secrets should be copied into a player build.

Last local verification: **2026-09-06**.
