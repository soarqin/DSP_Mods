const assert = require("node:assert/strict");
const test = require("node:test");
const { createQuery, result, observe, flush } = require("./test_helpers");

function itemReader(params, poolIndex = 1, sessionId = "save-a") {
  const member = params.path[params.path.length - 1];
  if (member === "factoryCount") return result(1, sessionId);
  if (params.path.includes("productIndices")) return result(poolIndex, sessionId);
  if (member === "total") {
    const totals = Array(14).fill("0");
    totals[1] = "60";
    totals[8] = "20";
    return result({ items: totals.slice(params.offset || 0, (params.offset || 0) + params.limit) }, sessionId);
  }
  return result({ refProductSpeed: 120, refConsumeSpeed: 40 }, sessionId);
}

test("a missing product is rediscovered when production starts", async () => {
  let poolIndex = 0;
  const { query } = createQuery(params => itemReader(params, poolIndex));
  assert.equal((await query.getItemRates(1101)).production, 0);
  poolIndex = 1;
  const rates = await query.getItemRates(1101);
  assert.equal(rates.production, 60);
  assert.equal(rates.consumption, 20);
  assert.equal(rates.theoreticalProduction, 120);
});

test("duplicate item ids share reads without doubling totals", async () => {
  const { query, calls } = createQuery(itemReader);
  const rates = await query.getItemRates([1101, 1101]);
  assert.equal(rates.length, 2);
  assert.equal(rates[0].production, 60);
  assert.equal(rates[1].production, 60);
  assert.equal(calls.filter(params => params.path.includes("productIndices")).length, 1);
});

test("unbuilt local planets return zero rates without negative indexes", async () => {
  const { query, calls } = createQuery(params => {
    if (params.root === "localPlanet") return result(-1);
    throw new Error("An unbuilt planet has no factory statistics");
  });
  const rates = await query.getItemRates(1101, { scope: "localPlanet" });
  assert.equal(rates.production, 0);
  assert.equal(rates.factories, 0);
  assert.equal(rates.sessionId, "save-a");
  assert.equal(calls.length, 1);
});

test("empty production results retain session metadata", async () => {
  const { query } = createQuery(() => result(0));
  const rates = await query.getItemRates(1101);
  assert.equal(rates.sessionId, "save-a");
  assert.equal(rates.gameTick, "100");
});

test("explicit factory scopes refresh cached indices after a save change", async () => {
  let sessionId = "save-a";
  let poolIndex = 1;
  const { query, calls } = createQuery(params => itemReader(params, poolIndex, sessionId));
  await query.getItemRates(1101, { scope: { factoryIndex: 0 } });
  await query.getItemRates(1101, { scope: { factoryIndex: 0 } });
  calls.length = 0;
  sessionId = "save-b";
  poolIndex = 2;
  await query.getItemRates(1101, { scope: { factoryIndex: 0 } });
  const productReads = calls.filter(params => params.path.includes("productPool"));
  assert.equal(productReads.length, 2);
  for (const params of productReads) assert.equal(params.path[4], 2);
});

test("research from different saves is not combined", async () => {
  const { query } = createQuery(params => params.path[0] === "currentTech"
    ? result(1001)
    : result({ hashUploaded: "50", hashNeeded: "100", curLevel: 1 }, "save-b"));
  await assert.rejects(query.getCurrentResearch(), { kind: "SESSION_CHANGED" });
});

test("research progress keeps integer precision", async () => {
  const { query } = createQuery(() => result({
    hashUploaded: "9007199254740993", hashNeeded: "18014398509481986", curLevel: 1
  }));
  const research = await query.getTechState(1001);
  assert.equal(research.progressPercent, 50);
  assert.equal(research.hashUploaded, "9007199254740993");
});

test("Dyson collection pages project energy instead of rereading each sphere", async () => {
  const energy = { energyGenCurrentTick: "100", energyGenOriginalCurrentTick: "200", energyReqCurrentTick: "5" };
  const { query, calls } = createQuery(params => {
    if (params.path.length === 1) {
      const entries = Array(130).fill(null);
      entries[0] = energy;
      entries[129] = energy;
      const offset = params.offset || 0;
      return result({ items: entries.slice(offset, offset + params.limit), offset, hasMore: offset + params.limit < entries.length });
    }
    if (params.path[2] === "starData") return result({ id: params.path[1] + 1, name: "Star" });
    return result(energy);
  });
  const power = await query.getDysonPower();
  assert.equal(power.watts, "12000");
  assert.equal(power.originalWatts, "24000");
  assert.equal(power.sphereCount, 2);
  assert.equal(calls.length, 4);
  for (const page of calls.filter(params => params.path.length === 1)) {
    assert.ok(page.select.includes("energyGenCurrentTick"));
  }
});

test("a targeted Dyson query does not scan all stars", async () => {
  const { query, calls } = createQuery(params => {
    if (params.path.length === 1) return result({ items: [null, {}], offset: 0, hasMore: false });
    if (params.path[2] === "starData") return result({ id: 2, name: "Star" });
    return result({ energyGenCurrentTick: "9007199254740993", energyGenOriginalCurrentTick: "9007199254740993" });
  });
  const power = await query.getDysonPower({ starIndex: 1 });
  assert.equal(power.watts, "540431955284459580");
  assert.equal(calls.length, 2);
  assert.equal(calls[0].path[1], 1);
});

test("star lookup failures are not reported as successful power reads", async () => {
  const { query } = createQuery(params => {
    if (params.path.length === 1) return result({ items: [{}], offset: 0, hasMore: false });
    if (params.path[2] === "starData") throw { kind: "SESSION_CHANGED" };
    return result({ energyGenCurrentTick: "100" });
  });
  await assert.rejects(query.getDysonPower(), { kind: "SESSION_CHANGED" });
});

test("empty Dyson results retain session metadata", async () => {
  const { query } = createQuery(() => result({ items: [], offset: 0, hasMore: false }));
  const power = await query.getDysonPower();
  assert.equal(power.watts, "0");
  assert.equal(power.sessionId, "save-a");
  assert.equal(power.gameTick, "100");
});

test("late responses cannot restore a previous session", async () => {
  let finish;
  const pending = new Promise(resolve => { finish = resolve; });
  const { query } = createQuery(params => params.path[0] === "techStates" ? pending : result(0, "save-b"));
  const old = observe(query.getTechState(1001));
  await flush();
  await query.getCurrentResearch();
  finish(result({ hashUploaded: "1", hashNeeded: "2" }, "save-a"));
  await flush();
  assert.equal(old.error.kind, "SESSION_CHANGED");
  assert.equal(query._sessionId, "save-b");
});

test("failed queries wait for their other reads before settling", async () => {
  let finish;
  const pending = new Promise(resolve => { finish = resolve; });
  const { query } = createQuery(params => {
    if (params.path.at(-1) === "total") return pending;
    if (params.select) throw { kind: "SERVER_BUSY" };
    return itemReader(params);
  });
  const rates = observe(query.getItemRates(1101));
  await flush();
  assert.equal(rates.status, "pending");
  finish(result({ items: Array(8).fill("0") }));
  await flush();
  assert.equal(rates.error.kind, "SERVER_BUSY");
});
