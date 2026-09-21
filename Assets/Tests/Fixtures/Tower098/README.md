# Tower098 legacy compatibility fixtures

These JSON files are exact-byte copies of actual earlier isolated QA saves. No chapter, floor, battle result, HP, roster, XP, or reward field was edited to construct them. Runtime tests copy them to a GUID-named temporary directory before using the existing coordinator/save authorities.

- `R108_OldPendingFloor001.json`: original `C:/SecondDimension/BuildEvidence/SSSTenV4/Deployment_R108_Actual60_20260908/EarnedReviewSave097.json`; SHA-256 `65BB98079D6F3FC0CFD928E6DDF72891EA36326D652F2EF5A479D1D41D23A7DB`. An old094-policy floor-one pending battle with 60 deployed members.
- `R100_OldCompletedFloor010.json`: original `C:/SecondDimension/BuildEvidence/SSSTenV4/Tower_R100_Actual12_20260908/floor_0010_earned.json`; SHA-256 `0734B7E3B6B5EBFF346B2E39D512F4DA91D52059DA6416328C742E2EE5F1707A`. Ten genuinely earned Tower clears; that historical run had 59 battle-eligible members out of 60 assigned.

The tests do not certify high-floor balance. They check unchanged old commitments and legal new-floor preparation/entry plus deterministic request/save identity. Separate numeric fixtures check monotonic floor multipliers and explicit technical ceilings.
