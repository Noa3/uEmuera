# uEmuera Resource Pipeline (Phase 6)

## Problem (audited)

The same game directories are enumerated by several independent systems today:

* `uEmuera.Utils.ResourcePrepare()` (called from `FirstWindow.Run` before boot);
* `SpriteManager.InitializeFileIndex()` (called from `AppContents.LoadContents`);
* `AppContents` auto-discovery of images from subdirectories;
* CSV resource loading.

This means multiple directory walks, image searches, CSV reads, path-normalization
passes, and registrations per startup. `ResourcePrepare` can additionally decode images
(allocating `Texture2D` via `SpriteManager.GetTextureInfo`) merely to learn dimensions.

## Landed this pass

### `uEmuera.ImageHeaderProbe`

Reads width/height/format from header bytes only — **no decode, no `Texture2D`**:

* PNG (IHDR), GIF (logical screen), BMP (BITMAPINFOHEADER, top-down normalized),
  JPEG (SOF marker walk), WebP (`VP8 ` lossy 14-bit, `VP8L` lossless, `VP8X` extended 24-bit).
* Fully bounds-checked; returns `false` instead of throwing on malformed/truncated input.
* `TryReadFile` reads only a bounded prefix (64 KB), streaming further **only** for JPEG
  whose SOF may sit past EXIF/thumbnail data.
* Unit tests: `Assets/Tests/EditMode/ImageHeaderProbeTests.cs`.

## Target: one `GameResourceCatalog` (PLANNED)

A single authoritative catalog built from **one** directory scan per game:

* canonical relative path + case-insensitive key + aliases;
* resource CSV crop / offset / animation / alias (CSV **always** beats auto-discovery);
* source image, source rectangle, destination/base offset;
* image dimensions (via `ImageHeaderProbe`, not decode);
* file size + last-write timestamp.

Rules:

* Do **not** globally map a bare basename (`FACE.PNG`) to one file when `A/FACE.PNG` and
  `B/FACE.PNG` both exist — use canonical relative paths and report ambiguity.
* CSV-defined crop/offset/animation/alias must never be overridden by lazy discovery.
* Texture **decode stays demand-driven** — metadata may be indexed for thousands of
  images, but only visible/near-future images are decoded.

## Target: persistent index (PLANNED)

Small per-game index under the uEmuera cache dir storing game path, file path, size,
mtime, optional hash, aliases, image header size. On next launch, load it and rescan only
changed files. Cache key = relative path + size + mtime (+ optional fast hash). Per-file
invalidation — one changed PNG must not invalidate the whole game.

## Target: async image loading (PLANNED)

Stages: resolve metadata → worker reads bytes → optional worker decode where safe →
main thread creates/uploads `Texture2D` → renderer receives it. Never call Unity object
APIs from worker threads. Bounded I/O concurrency (2–4 reads) with a priority queue
(visible-now > title > current-scene preload > background). Layout is created from
metadata immediately; a late texture fills the existing render slot without changing
position/size/layer/hitbox/order.

Shared transparent placeholder (not a fresh `Texture2D`/`Sprite`/`Color[]` per request);
request generations (`GameSessionId` + `ViewGeneration` + `RequestId`) so a late callback
from an old screen / hover / pooled CBG object is ignored.
