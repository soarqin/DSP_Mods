const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const test = require("node:test");
const vm = require("node:vm");
const { ROOT, createHarness, flush } = require("./test_helpers");

const TEXT_DATA = {
  languages: { "2052": { lcid: 2052, name: "简体中文", abbr: "zh-CN", abbr2: "zh" } },
  ids: {
    item: { 1101: "铁矿名", 1203: "电动机名" },
    tech: { 1001: "电磁学名", 1002: "高效学名" }
  },
  strings: {
    铁矿名: { "2052": "铁矿石" },
    电动机名: { "2052": "电动机" },
    电磁学名: { "2052": "电磁学" },
    高效学名: { "2052": "高效采集" }
  }
};

function createDocument() {
  const byId = new Map();
  const created = [];
  function makeElement(tag) {
    const element = {
      tagName: String(tag).toUpperCase(), children: [], parentNode: null,
      textContent: "", className: "", src: "", alt: "", hidden: false,
      rowSpan: 0, style: {},
      classList: {
        set: new Set(),
        add(token) { this.set.add(token); },
        remove(token) { this.set.delete(token); },
        toggle(token, force) {
          const next = force == null ? !this.set.has(token) : !!force;
          if (next) this.set.add(token); else this.set.delete(token);
          return next;
        },
        contains(token) { return this.set.has(token); }
      },
      appendChild(child) { child.parentNode = element; element.children.push(child); return child; },
      removeAttribute() {},
      setAttribute(name, value) { element[name] = value; }
    };
    Object.defineProperty(element, "id", {
      get() { return element._id || ""; },
      set(value) { element._id = value; if (value) byId.set(value, element); }
    });
    created.push(element);
    return element;
  }
  const body = makeElement("body");
  return { created, byId, document: { createElement: makeElement, getElementById: id => byId.get(id) || null, body } };
}

function openDashboard(options = {}) {
  const { context, clock } = createHarness();
  const { created, byId, document } = createDocument();
  const calls = [];
  const clients = [];
  const events = new Map();
  const base = context.LiveStreamAssist;
  class FakeClient {
    constructor(settings) { this.url = settings.url; this.connections = []; clients.push(this); }
    connect(url) {
      if (url != null) this.url = url;
      this.connections.push(url);
      if (options.failFirst && this.connections.length === 1) return Promise.reject({ kind: "CONNECT_FAILED" });
      return Promise.resolve(this);
    }
    close() { this.infoSnapshot = null; }
    systemInfo() { this.infoSnapshot = {}; return Promise.resolve(this.infoSnapshot); }
    validate() { return Promise.resolve({}); }
  }
  class FakeQuery {
    getItemRates(ids, opts) {
      calls.push(["rates", ids.slice(), opts]);
      if (options.pendingRates) return options.pendingRates;
      return Promise.resolve(ids.map(id => ({
        itemId: id, perMinute: true, production: 3, consumption: 1,
        theoreticalProduction: 4, theoreticalConsumption: 2
      })));
    }
    getTechState(id) {
      calls.push(["tech", id]);
      const custom = options.techStates && options.techStates[id];
      return Promise.resolve(custom || { techId: id, unlocked: false, curLevel: 2, maxLevel: 10 });
    }
    getCurrentResearch() {
      calls.push(["research"]);
      return Promise.resolve(options.research || {
        active: true, techId: 1001, curLevel: 0, progressPercent: 42.5,
        hashUploaded: "12345", hashNeeded: "67890"
      });
    }
    getDysonPower() {
      calls.push(["dyson"]);
      return Promise.resolve({ watts: "60900000" });
    }
  }
  context.LiveStreamAssist = Object.assign({}, base, {
    Client: FakeClient,
    GameQuery: FakeQuery,
    GameText: { fromGenerated(settings) { return new base.GameText(TEXT_DATA, settings); } },
    GameIcons: {
      fromGenerated() {
        return { itemIcon: id => "icon-item-" + id, techIcon: id => "icon-tech-" + id };
      }
    }
  });
  for (const id of ["panel", "status"]) {
    const element = document.createElement("div");
    element.id = id;
    document.body.appendChild(element);
  }
  Object.assign(context, {
    location: { search: options.search || "" },
    URLSearchParams,
    document,
    console,
    addEventListener(name, callback) { events.set(name, callback); }
  });
  const html = fs.readFileSync(path.join(ROOT, "..", "examples", "live-dashboard.html"), "utf8");
  const scripts = [...html.matchAll(/<script(?:[^>]*)>([\s\S]*?)<\/script>/g)];
  vm.runInContext(scripts.map(match => match[1]).join("\n"), context, { filename: "live-dashboard.html" });
  return { clock, calls, clients, events, byId, created };
}

test("live-dashboard renders the requested sections", async () => {
  const dashboard = openDashboard({ search: "?items=1101,1203&techs=1001,1002&research=1&dyson=1&lang=zh-CN" });
  await flush();
  assert.equal(dashboard.byId.get("item-1101-prod").textContent, "3.00");
  assert.equal(dashboard.byId.get("item-1101-prodRef").textContent, "4.00");
  assert.equal(dashboard.byId.get("item-1101-cons").textContent, "1.00");
  assert.equal(dashboard.byId.get("item-1101-consRef").textContent, "2.00");
  assert.equal(dashboard.byId.get("item-1203-prod").textContent, "3.00");
  assert.equal(dashboard.byId.get("tech-1001").textContent, "2/10");
  assert.equal(dashboard.byId.get("tech-1002").textContent, "2/10");
  assert.equal(dashboard.byId.get("researchName").textContent, "电磁学");
  assert.equal(dashboard.byId.get("researchPct").textContent, "42.5%");
  assert.equal(dashboard.byId.get("researchHash").textContent, "12.3k / 67.8k 哈希");
  assert.equal(dashboard.byId.get("dysonWatts").textContent, "60.9MW");
  assert.equal(dashboard.byId.get("status").textContent, "已连接");
  assert.ok(dashboard.created.some(element => element.tagName === "IMG" && element.src === "icon-item-1101"));
  assert.ok(dashboard.calls.some(([name, ids]) => name === "rates" && ids.join() === "1101,1203"));
});

test("live-dashboard defaults to research and Dyson sections", async () => {
  const dashboard = openDashboard({});
  await flush();
  assert.equal(dashboard.byId.get("researchName").textContent, "电磁学");
  assert.equal(dashboard.byId.get("dysonWatts").textContent, "60.9MW");
  assert.ok(!dashboard.byId.has("item-1101-prod"));
  assert.ok(!dashboard.byId.has("tech-1001"));
  assert.ok(!dashboard.calls.some(([name]) => name === "rates" || name === "tech"));
});

test("live-dashboard shows a repeatable and a one-time tech level", async () => {
  const dashboard = openDashboard({
    search: "?techs=1001,1002",
    techStates: {
      1001: { techId: 1001, unlocked: false, curLevel: 5, maxLevel: 20 },
      1002: { techId: 1002, unlocked: true, curLevel: 0, maxLevel: 0 }
    }
  });
  await flush();
  assert.equal(dashboard.byId.get("tech-1001").textContent, "5/20");
  assert.equal(dashboard.byId.get("tech-1002").textContent, "已解锁");
});

test("live-dashboard resumes polling after the first connection fails", async () => {
  const dashboard = openDashboard({ search: "?items=1101", failFirst: true });
  await flush();
  assert.equal(dashboard.calls.length, 0);
  dashboard.clock.advance(2000);
  await flush();
  assert.equal(dashboard.byId.get("item-1101-prod").textContent, "3.00");
  assert.equal(dashboard.byId.get("status").textContent, "已连接");
});

test("live-dashboard does not overlap slow polls", async () => {
  let finish;
  const pending = new Promise(resolve => { finish = resolve; });
  const dashboard = openDashboard({ search: "?items=1101", pendingRates: pending });
  await flush();
  dashboard.clock.advance(10000);
  await flush();
  assert.equal(dashboard.calls.length, 1);
  finish([{ itemId: 1101, perMinute: true, production: 6, consumption: 2, theoreticalProduction: 8, theoreticalConsumption: 4 }]);
  await flush();
  assert.equal(dashboard.byId.get("item-1101-prod").textContent, "6.00");
  dashboard.clock.advance(2000);
  await flush();
  assert.equal(dashboard.calls.length, 2);
});

test("live-dashboard stops polling on pagehide", async () => {
  const dashboard = openDashboard({ search: "?research=1" });
  await flush();
  assert.equal(typeof dashboard.events.get("pagehide"), "function");
  dashboard.events.get("pagehide")();
  dashboard.clock.advance(20000);
  await flush();
  assert.equal(dashboard.calls.length, 1);
  assert.equal(dashboard.clock.size, 0);
});

test("live-dashboard reports initialization errors without polling", async () => {
  for (const search of ["?window=5min&research=1", "?techs=abc", "?items=1101&techs=0"]) {
    const dashboard = openDashboard({ search });
    await flush();
    assert.match(dashboard.byId.get("status").textContent, /初始化失败/);
    assert.equal(dashboard.calls.length, 0);
    dashboard.clock.advance(10000);
    await flush();
    assert.equal(dashboard.calls.length, 0);
  }
});

test("live-dashboard can disable the default sections explicitly", async () => {
  const dashboard = openDashboard({ search: "?research=0" });
  await flush();
  assert.ok(!dashboard.byId.has("researchName"));
  assert.ok(!dashboard.byId.has("dysonWatts"));
  assert.equal(dashboard.byId.get("status").textContent, "已连接");
});
