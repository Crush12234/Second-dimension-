# Final native regression verification — Update 165

All 24 native cases passed against the final pinned Gameplay candidate. The source map contains 195 Gameplay files, and every changed staged Gameplay source matched that compiled candidate when this approval was written.

The 17 opening-fate/Union cases verify Quest 1, Quest 2 and Quest 3 D20 and Fortune Wheel flow; seal/roll/collect timing; exact save/reload and loop resumption; tamper rejection; every wheel reward; natural-20 permanent hero check bonuses, caps and recovery; actual added Union admission to the next newly created opening battle; paid luck activation/consumption, higher-of-two rolls, and unchanged already sealed results; preserved supply, fatigue and threat effects; and 520 generated later decks covering all 130 boards at four Guild levels.

The seven merchant cases verify five distinct inventories, accessory power at market milestones, exact purchase debit/reload and stale quote rejection, actual hero acquisition followed by duplicate ascension and native stat changes, persistent single-use luck activation, native WorldGate blessing sealing/charge consumption/roll, and preserved historical merchant quote authority.

This is native command and controlled-cohort evidence. It does not replace the separately recorded actual UI pointer checks, and it is not a full playthrough of every generated board. No player saves were edited or packaged by these tests.

Files: NATIVE_COMBINED165_receipt165.json, NATIVE_COMBINED165_tests.log, NATIVE_COMBINED165_GAMEPLAY_SOURCE_MAP.json, NATIVE_COMBINED165_APPROVAL.json. Source tests: Assets/Tests/EditMode/OpeningQuestFate165Tests.cs and Merchant165Tests.cs.
