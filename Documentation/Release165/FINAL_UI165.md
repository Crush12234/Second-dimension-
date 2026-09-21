# Update165 focused UI acceptance

Accepted 20 September 2026. Final candidate run: R561_FinalUpdate165, Passed=true. Shipping source and art pins are checked again by verify_release165.py before delivery.

The running Windows Unity player rendered the final Waystation, Quest 3 campaign, Union builder, specialist merchants, equipment and hero offers, luck tonic, and Travel Pouch. Screens were inspected at 812 x 375 with 145% text and at 1280 x 720 with 100% text. The illustrated hub title, destination cards, service buttons, shop controls, and pouch state were readable in these captures.

Actual Unity EventSystem hit testing exercised the controls. A new Union accepted an owned reserve hero, survived reload, retained the campaign position, and left the committed battle unchanged. Quest 3 displayed scenic card rooms without the former route graph. An accessory purchase spent the quoted 45 XP and equipped successfully. A tonic purchase spent 60 XP; activation consumed one item, saved its ready charge, and survived reload. Repeated purchase receipts did not charge twice. The hero exchange correctly disabled a 1,200 XP offer when only 590 XP was available. Funded hero acquisition and duplicate ascension are covered by the separate passing native suite.

Earlier focused opening-card run R556_OpeningFate165 passed the actual Quest 1 and Quest 2 D20/wheel, explicit collect, reload, and Town return cases. It predates final hub and merchant UI polish and is supporting evidence only, not the final source-pin acceptance. The combined native suite passed all 24 cases, including Quest 1-3 fate and later-board availability; see NATIVE_COMBINED165.md.

Limits: these are Windows player tests and rendered landscape phone-size checks. No physical phone, second PC, physical USB transfer, full clean Unity Editor rebuild, or new exhaustive audit of unrelated historical loops is claimed. OS-level pointer injection was unavailable; the UI checks used Unity's real EventSystem. Public evidence excludes private playthrough saves.
