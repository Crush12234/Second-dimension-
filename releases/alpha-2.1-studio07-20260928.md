# Alpha 2.1 — Studio07 (Windows, 2026-09-28)

[Download the alpha from Google Drive](https://drive.google.com/drive/folders/1neRAI3JwowR3VVveXhJ9yYKjgkHesDUF). Drive access remains controlled by the owner; sharing permissions were not changed.

This repository update publishes release notes and the download location. The playable binary is hosted on Drive, not as a GitHub Release asset. It does not update the repository's Unity source.

Download all 25 archive parts (.001–.025) and JOIN_ALPHA.cmd into the same folder with at least 10 GB free. Run JOIN_ALPHA.cmd to join and verify the ZIP, extract the entire archive to a short writable path, then run A21S07/START_GAME.cmd. DOWNLOAD_AND_PLAY.txt and PARTS_MANIFEST.json provide instructions and individual checksums.

## Changes

Taller Union character displays, a newly illustrated Union hall, shared captions replacing individual panels, a quieter Guild Hall, consistent character art in replacement reviews, and Armory readability fixes.

## Validation and known issues

Unity 6000.3.22f1 build: zero errors/warnings. Thirty-three focused presentation testcase identities passed across reruns; actual native visual review was completed. Every ZIP entry passed CRC verification; the 25 uploaded parts were read back with their expected sizes.

This remains an alpha. Small-window companion labels and secondary menus need polish. Full campaign 1–82 twice, Tower 500 and all city buildings at level 10 are unfinished; chapter-21 boss pacing remains under review. A later QA session reported slow shutdown followed by an exit error; protected saves remained unchanged.

The distribution contains an empty portable SaveData directory. Personal and QA saves are excluded. Preserve previous builds and saves separately; no migration is included.

ZIP size: 2,303,248,798 bytes.

SHA256: `c6c5572af85f823735009475245301828823503cec88484450b27f24e46c4c9f`
