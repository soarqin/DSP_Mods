/**
 * LiveStreamAssist browser client.
 * Client: JSON-RPC WebSocket wrapper.
 * GameQuery: production, research, and Dyson power helpers.
 * GameText: offline locale names and DSP KMG formatting.
 */
(function (root, factory) {
  var api = factory();
  if (typeof module === "object" && module.exports) module.exports = api;
  root.LiveStreamAssist = api;
})(typeof globalThis !== "undefined" ? globalThis : this, function () {
  "use strict";
  var ns = {};
