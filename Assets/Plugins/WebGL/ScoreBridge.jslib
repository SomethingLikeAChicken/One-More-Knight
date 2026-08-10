// Hands a finished Run to the hosting page. The website's play page exposes
// window.__omk.submitScore(score, meta) (repo one-more-knight-web); pages without
// the bridge (stock index.html, index-dev.html) make this a silent no-op.
mergeInto(LibraryManager.library, {
  OMK_SubmitScore: function (score, metaJsonPtr) {
    try {
      var omk = typeof window !== "undefined" && window.__omk;
      if (!omk || typeof omk.submitScore !== "function") return;
      var meta = {};
      try {
        meta = JSON.parse(UTF8ToString(metaJsonPtr));
      } catch (e) {}
      omk.submitScore(score, meta);
    } catch (e) {}
  },
});
