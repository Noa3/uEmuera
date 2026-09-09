(function () {
  'use strict';

  // Minimal SDK object representing what a packaged ERE bundle exposes before
  // uEmuera injects its native API bridge. The bridge preserves this object and
  // overlays the compatible era.* functions.
  window._era = window._era || {};
  window._era.version = window._era.version || {
    engine: '2200',
    sdk: '4.7.0'
  };
})();
