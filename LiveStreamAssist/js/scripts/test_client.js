const assert = require("node:assert/strict");
const test = require("node:test");
const { createHarness, observe, flush } = require("./test_helpers");

test("close cancels a connecting socket and permits an explicit reconnect", async () => {
  const { api, sockets } = createHarness();
  const client = new api.Client();
  const first = observe(client.connect());
  client.close();
  await flush();
  assert.equal(first.status, "rejected");
  assert.equal(first.error.kind, "DISCONNECTED");
  assert.equal(sockets[0].readyState, 3);
  const second = observe(client.connect());
  assert.equal(sockets.length, 2);
  sockets[1].open();
  await flush();
  assert.equal(second.status, "fulfilled");
  assert.equal(client.autoReconnect, true);
  client.close();
});

test("a malformed URL does not poison later connections", async () => {
  const { api, sockets } = createHarness();
  const client = new api.Client({ url: "not a URL" });
  const failed = observe(client.connect());
  await flush();
  assert.equal(failed.status, "rejected");
  const retry = observe(client.connect("ws://localhost:18080/api/v1"));
  assert.equal(sockets.length, 1);
  sockets[0].open();
  await flush();
  assert.equal(retry.status, "fulfilled");
  client.close();
});

test("connect with another URL replaces an open connection", async () => {
  const { api, sockets } = createHarness();
  const client = new api.Client();
  const first = client.connect();
  sockets[0].open();
  await first;
  const second = observe(client.connect("ws://localhost:19090/api/v1"));
  assert.equal(sockets[0].readyState, 3);
  assert.equal(sockets.length, 2);
  sockets[1].open();
  await flush();
  assert.equal(second.status, "fulfilled");
  client.close();
});

test("disconnect rejects sent and queued calls without leaking capacity", async () => {
  const { api, sockets } = createHarness();
  const client = new api.Client({ maxPending: 1, autoReconnect: false });
  const requests = [observe(client.ping()), observe(client.ping()), observe(client.ping())];
  sockets[0].open();
  await flush();
  assert.equal(sockets[0].sent.length, 1);
  sockets[0].close(1006);
  await flush();
  for (const request of requests) {
    assert.equal(request.status, "rejected");
    assert.equal(request.error.kind, "DISCONNECTED");
  }
  assert.equal(client._active, 0);
  const retry = observe(client.ping());
  sockets[1].open();
  await flush();
  assert.equal(sockets[1].sent.length, 1);
  sockets[1].reply(sockets[1].sent[0], "fresh");
  await flush();
  assert.equal(retry.value, "fresh");
  client.close();
});

test("requests do not implicitly reopen an explicitly closed client", async () => {
  const { api, sockets } = createHarness();
  const client = new api.Client();
  client.close();
  const request = observe(client.ping());
  await flush();
  assert.equal(request.status, "rejected");
  assert.equal(request.error.kind, "DISCONNECTED");
  assert.equal(sockets.length, 0);
});

test("responses correlate by id and preserve Int64 strings", async () => {
  const { api, sockets } = createHarness();
  const client = new api.Client({ maxPending: 2 });
  const first = observe(client.ping());
  const second = observe(client.ping());
  sockets[0].open();
  await flush();
  sockets[0].reply(sockets[0].sent[1], "9223372036854775807");
  sockets[0].reply(sockets[0].sent[0], "first");
  await flush();
  assert.equal(first.value, "first");
  assert.equal(second.value, "9223372036854775807");
  assert.equal(client._active, 0);
  client.close();
});

test("timeouts release the next queued call exactly once", async () => {
  const { api, sockets, clock } = createHarness();
  const client = new api.Client({ maxPending: 1, requestTimeoutMs: 50 });
  const first = observe(client.ping());
  const second = observe(client.ping());
  sockets[0].open();
  await flush();
  clock.advance(50);
  await flush();
  assert.equal(first.error.kind, "CLIENT_TIMEOUT");
  assert.equal(sockets[0].sent.length, 2);
  sockets[0].reply(sockets[0].sent[0], "late");
  sockets[0].reply(sockets[0].sent[1], "second");
  await flush();
  assert.equal(second.value, "second");
  assert.equal(client._active, 0);
  assert.equal(clock.size, 0);
  client.close();
});

test("server limits respect a lower caller limit", async () => {
  const { api, sockets } = createHarness();
  const client = new api.Client({ maxPending: 1 });
  const info = client.systemInfo();
  sockets[0].open();
  await flush();
  sockets[0].reply(sockets[0].sent[0], { limits: { maxPendingRequestsPerConnection: 8 } });
  await info;
  assert.equal(client.maxPending, 1);
  client.close();
});

test("close cancels automatic reconnect timers", async () => {
  const { api, sockets, clock } = createHarness();
  const client = new api.Client();
  const connection = client.connect();
  sockets[0].open();
  await connection;
  sockets[0].close(1006);
  assert.equal(clock.size, 1);
  client.close();
  assert.equal(clock.size, 0);
  clock.advance(10000);
  assert.equal(sockets.length, 1);
});

test("invalid request limits fail instead of hanging the queue", () => {
  const { api } = createHarness();
  for (const maxPending of [0, -1, NaN, 1.5]) {
    assert.throws(() => new api.Client({ maxPending }), { name: "RangeError" });
  }
});

test("a slot released just before reconnect cannot send on the new socket", async () => {
  const { api, sockets } = createHarness();
  const client = new api.Client({ maxPending: 1 });
  observe(client.ping());
  const queued = observe(client.ping());
  sockets[0].open();
  await flush();
  sockets[0].reply(sockets[0].sent[0], "complete");
  client.close();
  const connection = client.connect();
  sockets[1].open();
  await connection;
  const current = observe(client.ping());
  await flush();
  assert.equal(queued.error.kind, "DISCONNECTED");
  assert.equal(sockets[1].sent.length, 1);
  assert.equal(client._active, 1);
  sockets[1].reply(sockets[1].sent[0], "current");
  await flush();
  assert.equal(current.value, "current");
  assert.equal(client._active, 0);
  client.close();
});

test("closing after open but before request continuations rejects old work", async () => {
  const { api, sockets } = createHarness();
  const client = new api.Client();
  const old = observe(client.ping());
  sockets[0].open();
  client.close();
  const connection = client.connect();
  sockets[1].open();
  await connection;
  await flush();
  assert.equal(old.error.kind, "DISCONNECTED");
  assert.equal(sockets[1].sent.length, 0);
  client.close();
});

test("status callback failures do not strand connections", async () => {
  const { api, sockets } = createHarness();
  const client = new api.Client({ onStatus() { throw new Error("Consumer callback"); } });
  const connection = observe(client.connect());
  sockets[0].open();
  await flush();
  assert.equal(connection.status, "fulfilled");
  client.close();
});

test("automatic reconnect recovers from an initial handshake failure", async () => {
  const { api, sockets, clock } = createHarness();
  const client = new api.Client();
  const initial = observe(client.connect());
  sockets[0].close(1006);
  await flush();
  assert.equal(initial.error.kind, "CONNECT_FAILED");
  clock.advance(500);
  assert.equal(sockets.length, 2);
  sockets[1].open();
  const request = observe(client.ping());
  await flush();
  sockets[1].reply(sockets[1].sent[0], "reconnected");
  await flush();
  assert.equal(request.value, "reconnected");
  client.close();
});
