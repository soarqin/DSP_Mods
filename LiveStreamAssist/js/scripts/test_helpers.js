const fs = require("node:fs");
const path = require("node:path");
const vm = require("node:vm");

const ROOT = path.resolve(__dirname, "..");

function createHarness() {
  let now = 0;
  let sequence = 0;
  const timers = new Map();
  const sockets = [];
  const clock = {
    setTimeout(callback, delay) {
      const timerId = ++sequence;
      timers.set(timerId, { callback, due: now + delay });
      return timerId;
    },
    setInterval(callback, delay) {
      const timerId = ++sequence;
      timers.set(timerId, { callback, due: now + delay, interval: delay });
      return timerId;
    },
    clearTimeout(timerId) { timers.delete(timerId); },
    advance(duration) {
      const end = now + duration;
      while (true) {
        const next = [...timers.entries()].filter(([, timer]) => timer.due <= end)
          .sort((left, right) => left[1].due - right[1].due)[0];
        if (!next) break;
        timers.delete(next[0]);
        now = next[1].due;
        if (next[1].interval) timers.set(next[0], { ...next[1], due: now + next[1].interval });
        next[1].callback();
      }
      now = end;
    },
    get size() { return timers.size; }
  };

  class FakeWebSocket {
    constructor(url) {
      if (!/^wss?:\/\//.test(url)) throw new SyntaxError("Invalid WebSocket URL");
      this.url = url;
      this.readyState = 0;
      this.sent = [];
      sockets.push(this);
    }
    open() { this.readyState = 1; if (this.onopen) this.onopen(); }
    close(code = 1000) {
      this.readyState = 3;
      if (this.onclose) this.onclose({ code });
    }
    send(body) {
      if (this.readyState !== 1) throw new Error("Socket is not open");
      this.sent.push(JSON.parse(body));
    }
    reply(request, result) {
      this.onmessage({ data: JSON.stringify({ jsonrpc: "2.0", id: request.id, result }) });
    }
  }

  const context = vm.createContext({
    console, WebSocket: FakeWebSocket,
    setTimeout: clock.setTimeout, clearTimeout: clock.clearTimeout
  });
  vm.runInContext(fs.readFileSync(path.join(ROOT, "livestream-assist.js"), "utf8"), context);
  return { api: context.LiveStreamAssist, context, clock, sockets };
}

function observe(promise) {
  const outcome = { status: "pending" };
  promise.then(
    value => Object.assign(outcome, { status: "fulfilled", value }),
    error => Object.assign(outcome, { status: "rejected", error })
  );
  return outcome;
}

function flush() { return new Promise(resolve => setImmediate(resolve)); }

function createQuery(handler) {
  const { api } = createHarness();
  const calls = [];
  const client = {
    read(params) {
      calls.push(params);
      return Promise.resolve().then(() => handler(params));
    }
  };
  return { query: new api.GameQuery(client), calls, api };
}

function result(value, sessionId = "save-a", gameTick = "100") {
  return { value, sessionId, gameTick };
}

module.exports = { ROOT, createHarness, createQuery, observe, flush, result };
