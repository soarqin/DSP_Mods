const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const test = require("node:test");
const vm = require("node:vm");
const { ROOT, createHarness } = require("./test_helpers");

test("the packed script matches its sources", () => {
  const names = ["header", "format", "client", "query", "text", "footer"];
  const sources = names.map(name => fs.readFileSync(path.join(ROOT, "src", name + ".js"), "utf8").replace(/\r\n/g, "\n")).join("");
  const packed = fs.readFileSync(path.join(ROOT, "livestream-assist.js"), "utf8").replace(/\r\n/g, "\n");
  assert.ok(packed === sources, "The bundle is stale; run python LiveStreamAssist/js/scripts/pack_js.py.");
});

test("empty and missing translations follow the game instead of English fallback", () => {
  const { api, sockets } = createHarness();
  const text = new api.GameText({
    languages: { 2052: { fallback: 1033 } },
    strings: { empty: { 2052: "", 1033: "English" }, missing: { 1033: "English" } }
  }, { language: 2052 });
  assert.equal(text.translate("empty"), "");
  assert.equal(text.translate("missing"), "missing");
  assert.equal(sockets.length, 0);
});

test("language aliases are case insensitive without metadata", () => {
  const { api } = createHarness();
  const text = new api.GameText({}, { language: "ZH-CN" });
  assert.equal(text.lcid, 2052);
  text.setLanguage("JA-JA");
  assert.equal(text.lcid, 1041);
});

test("JavaScript KMG formatting covers thresholds, signs and 64-bit values", () => {
  const { api } = createHarness();
  const cases = [
    [0, "count", "0"], [9999, "count", "9999"], [10000, "count", "10.0k"],
    [12345, "count", "12.3k"], [999999, "count", "999k"], [1000000, "count", "1.00M"],
    [-12345, "count", "-12.3k"], [12345, "power", "12.3kW"], [0, "power", "0W"],
    [12345, "energy", "12.3kJ"], [1000, "si1000", "1.00k"],
    ["9223372036854775807", "power", "9.22EW"]
  ];
  for (const [value, kind, expected] of cases) {
    assert.equal(api.format.write(value, { kind }).trim(), expected);
  }
  assert.equal(api.format.kmg(12345, { decimal: "," }), "12,3k");
  assert.ok(api.format.write(12345, { blank: true }).includes("\u2009k"));
});

test("generated prototype names are available offline without locale BOMs", () => {
  const { api, context } = createHarness();
  vm.runInContext(fs.readFileSync(path.join(ROOT, "generated", "dsp-text.js"), "utf8"), context);
  const text = api.GameText.fromGenerated({ language: "en" });
  assert.equal(text.translate("\u94c1\u77ff"), "Iron Ore");
  assert.ok(Object.keys(text.data.strings).every(key => !key.startsWith("\ufeff")));
});

test("README usage examples parse as classic scripts", () => {
  const readme = fs.readFileSync(path.join(ROOT, "README.md"), "utf8");
  for (const script of readme.matchAll(/^<script>([\s\S]*?)^<\/script>/gm)) {
    assert.doesNotThrow(() => new vm.Script(script[1]));
  }
});
