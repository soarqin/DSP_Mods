const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const test = require("node:test");
const vm = require("node:vm");
const { ROOT, createHarness, flush } = require("./test_helpers");

function openOverlay(filename, options = {}) {
  const { context, clock, api } = createHarness();
  const elements = new Map();
  const events = new Map();
  const clients = [];
  let reads = 0;
  const html = fs.readFileSync(path.join(ROOT, "..", "examples", filename), "utf8");
  function element(id) {
    if (!elements.has(id)) elements.set(id, { value: "", textContent: "", style: {} });
    return elements.get(id);
  }
  for (const match of html.matchAll(/<input id="([^"]+)"[^>]*value="([^"]+)"/g)) element(match[1]).value = match[2];
  const languages = [...html.matchAll(/<option value="([^"]+)"/g)].map(match => match[1]);
  let language = languages[0];
  Object.defineProperty(element("lang"), "value", {
    get() { return language; },
    set(value) { language = languages.includes(String(value)) ? String(value) : ""; }
  });
  class Client {
    constructor(settings) { this.url = settings.url; this.settings = settings; this.connections = []; clients.push(this); }
    connect(url) {
      if (url != null) this.url = url;
      this.connections.push(this.url);
      if (options.failFirst && this.connections.length === 1) return Promise.reject({ kind: "CONNECT_FAILED" });
      return Promise.resolve(this);
    }
    close() { this.infoSnapshot = null; }
    systemInfo() { this.infoSnapshot = {}; return Promise.resolve(this.infoSnapshot); }
    validate() { return Promise.resolve({}); }
  }
  class GameQuery {
    getCurrentResearch() {
      reads += 1;
      return options.research ? options.research(reads) : Promise.resolve({ active: false });
    }
    getItemRates() { return Promise.resolve({ production: 1, consumption: 1, theoreticalProduction: 1, theoreticalConsumption: 1 }); }
    getDysonPower() { return Promise.resolve({ watts: "60" }); }
  }
  context.LiveStreamAssist = {
    Client, GameQuery,
    GameText: { fromGenerated(settings) { return new api.GameText({}, settings); } }
  };
  Object.assign(context, {
    location: { search: options.search || "" }, URLSearchParams,
    document: { getElementById: element },
    setInterval: clock.setInterval, clearInterval: clock.clearTimeout,
    addEventListener(name, callback) { events.set(name, callback); }
  });
  const scripts = [...html.matchAll(/<script(?:[^>]*)>([\s\S]*?)<\/script>/g)];
  vm.runInContext(scripts.map(match => match[1]).join("\n"), context, { filename });
  return { clock, element, events, clients, get reads() { return reads; } };
}

for (const filename of ["research-overlay.html", "stats-overlay.html"]) {
  test(filename + " resumes polling after the first connection fails", async () => {
    const overlay = openOverlay(filename, { failFirst: true });
    await flush();
    assert.equal(overlay.reads, 0);
    overlay.clock.advance(2000);
    await flush();
    assert.equal(overlay.reads, 1);
    assert.equal(overlay.element("status").textContent, "connected");
  });

  test(filename + " does not overlap slow polls", async () => {
    let finish;
    const pending = new Promise(resolve => { finish = resolve; });
    const overlay = openOverlay(filename, { research: () => pending });
    await flush();
    overlay.clock.advance(10000);
    await flush();
    assert.equal(overlay.reads, 1);
    finish({ active: false });
    await flush();
  });

  test(filename + " ignores old results after a URL change", async () => {
    let finish;
    const pending = new Promise(resolve => { finish = resolve; });
    const current = { active: true, techId: 1002, curLevel: 1, hashUploaded: "1", hashNeeded: "2", progressPercent: 50 };
    const overlay = openOverlay(filename, { research: count => count === 1 ? pending : Promise.resolve(current) });
    await flush();
    overlay.element("url").value = "ws://localhost:19090/api/v1";
    assert.equal(typeof overlay.element("url").onchange, "function");
    overlay.element("url").onchange();
    await flush();
    finish({ ...current, techId: 1001 });
    await flush();
    const title = overlay.element(filename === "research-overlay.html" ? "title" : "techName");
    assert.equal(title.textContent, "Tech 1002");
    assert.equal(overlay.clients[0].connections.at(-1), "ws://localhost:19090/api/v1");
  });

  test(filename + " accepts LCID query parameters and stops on pagehide", async () => {
    const overlay = openOverlay(filename, { search: "?lang=2052" });
    await flush();
    assert.equal(overlay.element("lang").value, "2052");
    assert.equal(typeof overlay.events.get("pagehide"), "function");
    overlay.events.get("pagehide")();
    assert.equal(overlay.clock.size, 0);
  });
}
