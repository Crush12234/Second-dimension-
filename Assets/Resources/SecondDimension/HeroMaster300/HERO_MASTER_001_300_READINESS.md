# Hero Master 300 data readiness

- Source archive: `SECOND_DIMENSION_HERO_MASTER_300_CODEX_READY.zip`
- Source archive SHA-256 (verified against the 90,234,932-byte user-supplied archive): `18B20C3F0BEF43888DB9774124E08F5BC4E708E68F8E8C76CF59B3AD28264612`
- Source entry: `Data/HERO_MASTER_001_300.json`
- Installed JSON SHA-256: `E2E186E217AA5B5D15A8C36366B2A3C192BD2FD71DB932040AC732D52A367845`
- Runtime catalog status: `HeroMaster300Catalog087` accepts 250 records and quarantines 50 malformed or placeholder-bearing records. Quarantined records cannot be previewed, recruited, redeemed, or used as visual-fallback identities.
- SS access: only validated `ss_generation_code` records are exposed to the existing Creator code service. SS records never enter normal Applicant Board eligibility.
- Exact art readiness: 57 accepted heroes have byte-identical standing/action sprite pairs promoted from the supplied archive, with each standing sprite also used as its same-identity dossier image. Nine additional accepted signature heroes already had complete exact dossier/standing/action sets. The honest exact-identity sprite-set total is therefore 66, not 250.
- Complete visual coverage: all 250 accepted heroes resolve a non-null sprite-form dossier, standing pose, and action pose. The 184 without an exact set receive a deterministic full-body procedural sprite fallback whose resource key begins `RUNTIME_HERO_SPRITE_FALLBACK_089/`. These fallbacks are deliberately labelled and are not represented as unique authored art.
- Framing: promoted PNG bytes remain unchanged. The scoped Unity importer allows a one-time alpha scan, and presentation caches a bounds-fitted Sprite so visible figures fill card and battle frames despite transparent padding.
- Rejected low-alpha sources: all 61 adapter-accepted low-alpha pairs were visually reviewed at their visible bounds. Every pair retained at least one fragmented, isolated-body-part, or effect-only pose; zero were promoted. Fully transparent, mismatched, composite-only, ambiguous, and quarantined identities remain excluded.
- Rights status: `PENDING_USER_RIGHTS_CONFIRMATION`. The user supplied the archive and requested review-build integration, but the archive contains no adequate commercial-use license or chain-of-title. This file does not claim commercial-use rights.
- Source preservation: the supplied archive and catalog entry were read without modification; the installed catalog is byte-identical to `Data/HERO_MASTER_001_300.json`, and every promoted destination hash matches its recorded source hash.
- Machine-readable promotion/provenance: `Data/HERO_300_REVIEW_SPRITE_PROMOTION_089.json`.
