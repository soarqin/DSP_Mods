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
  var TICKS_PER_SECOND = 60;
  var THIN_SPACE = "\u2009";
  var DIGITS = "0123456789";
  var DEFAULT_URL = "ws://127.0.0.1:18080/api/v1";
  var RATE_WINDOWS = {
    "1min": { productionIndex: 1, consumptionIndex: 8, divisor: 1, perMinute: true, timeLevel: 0 },
    "10min": { productionIndex: 2, consumptionIndex: 9, divisor: 10, perMinute: true, timeLevel: 1 },
    "1hour": { productionIndex: 3, consumptionIndex: 10, divisor: 60, perMinute: true, timeLevel: 2 },
    "10hour": { productionIndex: 4, consumptionIndex: 11, divisor: 600, perMinute: true, timeLevel: 3 },
    "100hour": { productionIndex: 5, consumptionIndex: 12, divisor: 6000, perMinute: true, timeLevel: 4 },
    total: { productionIndex: 6, consumptionIndex: 13, divisor: 1, perMinute: false, timeLevel: 5 }
  };

  function key(value) { return { key: value }; }

  function toBigInt(value) {
    if (typeof value === "bigint") return value;
    if (typeof value === "number") return BigInt(Math.trunc(value));
    if (value == null || value === "") return 0n;
    return BigInt(String(value));
  }

  function toNumber(value) {
    if (typeof value === "number") return value;
    if (typeof value === "bigint") {
      var n = Number(value);
      return Number.isFinite(n) ? n : null;
    }
    if (value == null || value === "") return 0;
    var parsed = Number(value);
    return Number.isFinite(parsed) ? parsed : null;
  }

  function writeKmg(value, options) {
    options = options || {};
    var lastIndex = options.lastIndex == null ? 8 : options.lastIndex;
    var blank = !!options.blank;
    var blankChar = options.blankChar || THIN_SPACE;
    var fillChar = options.fillChar != null ? options.fillChar : " ";
    var decimal = options.decimal || ".";
    var kind = options.kind || "count";
    var buf = [];
    var i;
    for (i = 0; i <= lastIndex; i++) buf[i] = fillChar;
    var n = toBigInt(value);
    var sign = 0;
    if (n > 0n) sign = 1;
    else if (n < 0n) { sign = -1; n = -n; }
    var pos = lastIndex;
    if (kind === "power") buf[pos--] = "W";
    else if (kind === "energy") buf[pos--] = "J";
    var threshold = kind === "si1000" ? 1000n : 10000n;
    if (n < threshold) {
      if (blank) buf[pos--] = blankChar;
      while (n > 0n) {
        buf[pos--] = DIGITS[Number(n % 10n)];
        if (pos < 0) return buf.join("");
        n /= 10n;
      }
    } else {
      var suffix = fillChar;
      var scale = 1n;
      if (n < 1000000n) { suffix = "k"; scale = 1000n; }
      else if (n < 1000000000n) { suffix = "M"; scale = 1000000n; }
      else if (n < 1000000000000n) { suffix = "G"; scale = 1000000000n; }
      else if (n < 1000000000000000n) { suffix = "T"; scale = 1000000000000n; }
      else if (n < 1000000000000000000n) { suffix = "P"; scale = 1000000000000000n; }
      else { suffix = "E"; scale = 1000000000000000000n; }
      buf[pos--] = suffix;
      if (blank) buf[pos--] = blankChar;
      var acc = 1n;
      while (n >= 1000n) { n /= 10n; acc *= 10n; }
      while (n > 0n) {
        buf[pos--] = DIGITS[Number(n % 10n)];
        if (pos < 0) return buf.join("");
        n /= 10n;
        acc *= 10n;
        if (acc === scale) {
          buf[pos--] = decimal;
          if (pos < 0) return buf.join("");
        }
      }
    }
    if (sign === 0) buf[pos--] = "0";
    else if (sign < 0) buf[pos--] = "-";
    return buf.join("");
  }

  function formatKmg(value, options) {
    options = options || {};
    var formatted = writeKmg(value, options);
    return options.trim === false ? formatted : formatted.replace(/^\s+|\s+$/g, "");
  }

  ns.format = {
    kmg: function (value, options) { return formatKmg(value, Object.assign({ kind: "count" }, options)); },
    kmg1000: function (value, options) { return formatKmg(value, Object.assign({ kind: "si1000" }, options)); },
    power: function (value, options) { return formatKmg(value, Object.assign({ kind: "power" }, options)); },
    energy: function (value, options) { return formatKmg(value, Object.assign({ kind: "energy" }, options)); },
    write: writeKmg
  };
  ns.key = key;
  ns.DEFAULT_URL = DEFAULT_URL;
  ns.TICKS_PER_SECOND = TICKS_PER_SECOND;
  ns.RATE_WINDOWS = RATE_WINDOWS;
  function ApiError(init) {
    var message = (init && init.message) || "LiveStreamAssist API error";
    Error.call(this, message);
    this.name = "ApiError";
    this.message = message;
    this.code = init && init.code;
    this.kind = init && init.kind;
    this.id = init && init.id;
    this.data = (init && init.data) || null;
    if (typeof Error.captureStackTrace === "function") Error.captureStackTrace(this, ApiError);
  }
  ApiError.prototype = Object.create(Error.prototype);
  ApiError.prototype.constructor = ApiError;

  function errorFromResponse(id, error) {
    var data = error && error.data;
    return new ApiError({
      id: id,
      code: error && error.code,
      message: error && error.message,
      kind: data && data.kind,
      data: data || null
    });
  }

  function Client(options) {
    options = options || {};
    this.url = options.url || DEFAULT_URL;
    this.autoReconnect = options.autoReconnect !== false;
    this.requestTimeoutMs = options.requestTimeoutMs == null ? 8000 : options.requestTimeoutMs;
    this.maxPending = options.maxPending == null ? 8 : options.maxPending;
    if (!Number.isInteger(this.maxPending) || this.maxPending < 1) {
      throw new RangeError("maxPending must be a positive integer.");
    }
    if (!Number.isFinite(this.requestTimeoutMs) || this.requestTimeoutMs <= 0) {
      throw new RangeError("requestTimeoutMs must be positive and finite.");
    }
    this._configuredMaxPending = options.maxPending;
    this._ws = null;
    this._connectionUrl = null;
    this._seq = 0;
    this._pending = new Map();
    this._waiters = [];
    this._active = 0;
    this._backoff = 500;
    this._closed = false;
    this._connecting = null;
    this._rejectConnecting = null;
    this._reconnectTimer = null;
    this._socketGen = 0;
    this.infoSnapshot = null;
    this.onStatus = options.onStatus || null;
  }

  Client.prototype._setStatus = function (status, detail) {
    if (typeof this.onStatus === "function") {
      try { this.onStatus(status, detail); } catch (error) {}
    }
  };

  Client.prototype.connect = function (url) {
    if (url != null) this.url = url;
    if (this._connectionUrl != null && this._connectionUrl !== this.url) this.close();
    this._closed = false;
    if (this._ws && this._ws.readyState === 1) return Promise.resolve(this);
    if (this._connecting) return this._connecting;
    if (this._ws) this._disconnect(new ApiError({ kind: "DISCONNECTED", message: "Connection replaced." }));
    clearTimeout(this._reconnectTimer);
    this._reconnectTimer = null;
    var self = this;
    var generation = ++this._socketGen;
    var resolveConnection;
    var connection = new Promise(function (resolve, reject) {
      resolveConnection = resolve;
      self._rejectConnecting = reject;
    });
    this._connecting = connection;
    this._connectionUrl = this.url;
    this.infoSnapshot = null;
    this.maxPending = this._configuredMaxPending == null ? 8 : this._configuredMaxPending;
    var socket;
    try { socket = this._ws = new WebSocket(this.url); }
    catch (error) {
      this._disconnect(new ApiError({ kind: "CONNECT_FAILED", message: String(error.message || error) }));
      this._setStatus("disconnected");
      return connection;
    }
    socket.onopen = function () {
      if (generation !== self._socketGen) return;
      self._backoff = 500;
      self._connecting = null;
      self._rejectConnecting = null;
      resolveConnection(self);
      self._setStatus("connected", { url: self.url });
    };
    socket.onmessage = function (event) {
      if (generation === self._socketGen) self._onMessage(event.data);
    };
    socket.onerror = function () {};
    socket.onclose = function (event) {
      if (generation !== self._socketGen) return;
      self._disconnect(new ApiError({
        kind: self._connecting ? "CONNECT_FAILED" : "DISCONNECTED",
        message: "WebSocket disconnected.", data: { code: event.code }
      }));
      var reconnectGeneration = self._socketGen;
      self._setStatus("disconnected", { code: event.code });
      if (!self._closed && self.autoReconnect && reconnectGeneration === self._socketGen) {
        var delay = self._backoff;
        self._backoff = Math.min(self._backoff * 2, 8000);
        self._reconnectTimer = setTimeout(function () {
          if (!self._closed && reconnectGeneration === self._socketGen) self.connect().catch(function () {});
        }, delay);
      }
    };
    this._setStatus("connecting", { url: this.url });
    return connection;
  };

  Client.prototype._disconnect = function (error) {
    this._socketGen += 1;
    clearTimeout(this._reconnectTimer);
    this._reconnectTimer = null;
    var socket = this._ws;
    this._ws = null;
    this._connectionUrl = null;
    this.infoSnapshot = null;
    if (this._rejectConnecting) this._rejectConnecting(error);
    this._rejectConnecting = null;
    this._connecting = null;
    this._failPending(error);
    if (socket) {
      socket.onopen = socket.onmessage = socket.onerror = socket.onclose = null;
      if (socket.readyState < 2) {
        try { socket.close(); } catch (closeError) {}
      }
    }
  };

  Client.prototype.close = function () {
    this._closed = true;
    this._disconnect(new ApiError({ kind: "DISCONNECTED", message: "Client closed." }));
    this._backoff = 500;
    this._setStatus("disconnected");
  };

  Client.prototype._failPending = function (error) {
    this._pending.forEach(function (waiter) {
      clearTimeout(waiter.timer);
      waiter.reject(error);
    });
    this._pending.clear();
    this._active = 0;
    var waiters = this._waiters.splice(0, this._waiters.length);
    waiters.forEach(function (waiter) { waiter.reject(error); });
  };

  Client.prototype._settlePending = function (id, error, result) {
    var waiter = this._pending.get(id);
    if (!waiter) return;
    this._pending.delete(id);
    clearTimeout(waiter.timer);
    this._release(waiter.generation);
    if (error) waiter.reject(error);
    else waiter.resolve(result);
  };

  Client.prototype._onMessage = function (raw) {
    var message;
    try { message = JSON.parse(raw); } catch (error) { return; }
    if (!message || message.id == null || message.jsonrpc !== "2.0") return;
    if (!message.error && !Object.prototype.hasOwnProperty.call(message, "result")) return;
    this._settlePending(message.id, message.error && errorFromResponse(message.id, message.error), message.result);
  };

  Client.prototype._drainWaiters = function () {
    if (this._closed || !this._ws || this._ws.readyState !== 1) return;
    while (this._active < this.maxPending && this._waiters.length) {
      this._active += 1;
      this._waiters.shift().resolve();
    }
  };

  Client.prototype._release = function (generation) {
    if (generation !== this._socketGen) return;
    this._active = Math.max(0, this._active - 1);
    this._drainWaiters();
  };

  Client.prototype._acquire = function (generation) {
    if (this._closed || generation !== this._socketGen || !this._ws || this._ws.readyState !== 1) {
      return Promise.reject(new ApiError({ kind: "DISCONNECTED", message: "WebSocket is not open." }));
    }
    var self = this;
    return new Promise(function (resolve, reject) {
      self._waiters.push({ resolve: resolve, reject: reject });
      self._drainWaiters();
    });
  };

  Client.prototype.request = function (method, params) {
    if (this._closed) return Promise.reject(new ApiError({ kind: "DISCONNECTED", message: "Client closed." }));
    var self = this;
    var connection = this.connect();
    var generation = this._socketGen;
    return connection.then(function () { return self._acquire(generation); }).then(function () {
      if (generation !== self._socketGen || !self._ws || self._ws.readyState !== 1) {
        self._release(generation);
        throw new ApiError({ kind: "DISCONNECTED", message: "WebSocket is not open." });
      }
      var id = "n-" + (++self._seq);
      var body = { jsonrpc: "2.0", id: id, method: method };
      if (params != null) body.params = params;
      return new Promise(function (resolve, reject) {
        var timer = setTimeout(function () {
          self._settlePending(id, new ApiError({ kind: "CLIENT_TIMEOUT", message: "Client request timed out.", id: id }));
        }, self.requestTimeoutMs);
        self._pending.set(id, { resolve: resolve, reject: reject, timer: timer, generation: generation });
        try { self._ws.send(JSON.stringify(body)); }
        catch (err) {
          self._settlePending(id, new ApiError({ kind: "DISCONNECTED", message: String(err && err.message || err), id: id }));
        }
      });
    });
  };

  Client.prototype.ping = function () { return this.request("system.ping", {}); };
  Client.prototype.stats = function () { return this.request("system.stats", {}); };
  Client.prototype.roots = function () { return this.request("data.roots", {}); };

  Client.prototype.systemInfo = function () {
    var self = this;
    var request = this.request("system.info", {});
    var generation = this._socketGen;
    return request.then(function (result) {
      if (generation !== self._socketGen) throw new ApiError({ kind: "DISCONNECTED", message: "Connection replaced." });
      self.infoSnapshot = result;
      var limit = result && result.limits && result.limits.maxPendingRequestsPerConnection;
      if (Number.isInteger(limit) && limit > 0) {
        self.maxPending = self._configuredMaxPending == null ? limit : Math.min(limit, self._configuredMaxPending);
        self._drainWaiters();
      }
      return result;
    });
  };

  Client.prototype.validate = function (params) {
    params = params || {};
    var body = { apiMajor: params.apiMajor == null ? 1 : params.apiMajor };
    if (params.requiredCapabilities) body.requiredCapabilities = params.requiredCapabilities;
    return this.request("system.validate", body);
  };

  Client.prototype.describe = function (params) {
    var body = { root: params.root };
    if (params.path) body.path = params.path;
    return this.request("data.describe", body);
  };

  Client.prototype.read = function (params) {
    var body = { root: params.root };
    if (params.path) body.path = params.path;
    if (params.select) body.select = params.select;
    if (params.offset != null) body.offset = params.offset;
    if (params.limit != null) body.limit = params.limit;
    return this.request("data.read", body);
  };

  Client.prototype.info = function () { return this.systemInfo(); };

  ns.ApiError = ApiError;
  ns.Client = Client;
  function allReads(promises) {
    var failure = null;
    return Promise.all(promises.map(function (promise) {
      return promise.catch(function (error) { if (!failure) failure = error; });
    })).then(function (results) {
      if (failure) throw failure;
      return results;
    });
  }

  function sessionChanged() {
    return new ApiError({ kind: "SESSION_CHANGED", message: "The game session changed during the query. Retry the query." });
  }

  function decorateTech(techId, result) {
    var value = result.value || {};
    var uploaded = toBigInt(value.hashUploaded);
    var needed = toBigInt(value.hashNeeded);
    var percent = 0;
    if (needed > 0n) percent = Number((uploaded * 10000n) / needed) / 100;
    return {
      techId: techId,
      unlocked: !!value.unlocked,
      curLevel: value.curLevel || 0,
      maxLevel: value.maxLevel || 0,
      hashUploaded: String(value.hashUploaded || "0"),
      hashNeeded: String(value.hashNeeded || "0"),
      progress: percent / 100,
      progressPercent: percent,
      sessionId: result.sessionId,
      gameTick: result.gameTick
    };
  }

  function GameQuery(client) {
    this.client = client;
    this._sessionId = null;
    this._sessionVersion = 0;
    this._indexCache = new Map();
  }

  GameQuery.prototype._rememberSession = function (sessionId) {
    if (sessionId !== this._sessionId) {
      this._sessionId = sessionId;
      this._sessionVersion += 1;
      this._indexCache.clear();
    }
  };

  GameQuery.prototype._assertSession = function (context) {
    if (context.sessionId !== this._sessionId) throw sessionChanged();
  };

  GameQuery.prototype._read = function (params, context) {
    context = context || {};
    var self = this;
    var version = this._sessionVersion;
    return this.client.read(params).then(function (result) {
      if (version !== self._sessionVersion && result.sessionId !== self._sessionId) throw sessionChanged();
      self._rememberSession(result.sessionId);
      if (context.sessionId && context.sessionId !== result.sessionId) throw sessionChanged();
      context.sessionId = result.sessionId;
      context.gameTick = result.gameTick;
      return result;
    }).catch(function (err) {
      if (err && (err.kind === "SESSION_CHANGED" || err.kind === "GAME_NOT_READY")) {
        self._indexCache.clear();
        if (version === self._sessionVersion) self._rememberSession(null);
      }
      throw err;
    });
  };

  GameQuery.prototype.waitUntilReady = function (options) {
    options = options || {};
    var interval = options.intervalMs == null ? 500 : options.intervalMs;
    var timeout = options.timeoutMs == null ? 30000 : options.timeoutMs;
    var self = this;
    var start = Date.now();
    function loop() {
      var version = self._sessionVersion;
      return self.client.roots().then(function (roots) {
        if (roots && roots.gameReady) {
          if (version !== self._sessionVersion && roots.sessionId !== self._sessionId) throw sessionChanged();
          self._rememberSession(roots.sessionId);
          return roots;
        }
        self._rememberSession(null);
        if (Date.now() - start >= timeout) {
          throw new ApiError({ kind: "GAME_NOT_READY", message: "Timed out waiting for a player game." });
        }
        return new Promise(function (resolve) { setTimeout(resolve, interval); }).then(loop);
      });
    }
    return loop();
  };

  GameQuery.prototype._getTechState = function (techId, context) {
    var self = this;
    return this._read({
      root: "history",
      path: ["techStates", key(techId)],
      select: ["unlocked", "curLevel", "maxLevel", "hashUploaded", "hashNeeded"]
    }, context).then(function (result) {
      self._assertSession(context);
      return decorateTech(techId, result);
    });
  };

  GameQuery.prototype.getTechState = function (techId) {
    return this._getTechState(techId, {});
  };

  GameQuery.prototype.getCurrentResearch = function () {
    var self = this;
    var context = {};
    return this._read({ root: "history", path: ["currentTech"] }, context).then(function (tech) {
      self._assertSession(context);
      var techId = tech.value || 0;
      if (!techId) return { techId: 0, active: false, sessionId: tech.sessionId, gameTick: tech.gameTick };
      return self._getTechState(techId, context).then(function (state) {
        self._assertSession(context);
        state.active = true;
        return state;
      });
    });
  };

  GameQuery.prototype._factoryIds = function (scope, context) {
    if (scope === "localPlanet") {
      return this._read({ root: "localPlanet", path: ["factoryIndex"] }, context).then(function (result) {
        return Number.isInteger(result.value) && result.value >= 0 ? [result.value] : [];
      });
    }
    var factoryIndex = scope && typeof scope === "object" ? scope.factoryIndex : null;
    if (factoryIndex != null) {
      if (!Number.isInteger(factoryIndex) || factoryIndex < 0) throw new RangeError("factoryIndex must be a nonnegative integer.");
    } else if (scope != null && scope !== "cluster") {
      throw new Error("Unknown production scope.");
    }
    return this._read({ root: "game", path: ["factoryCount"] }, context).then(function (result) {
      var ids = [];
      var count = result.value || 0;
      if (factoryIndex != null) return factoryIndex < count ? [factoryIndex] : [];
      for (var index = 0; index < count; index++) ids.push(index);
      return ids;
    });
  };

  GameQuery.prototype._productIndex = function (factoryIndex, itemId, context) {
    this._assertSession(context);
    var cacheKey = factoryIndex + ":" + itemId;
    if (this._indexCache.has(cacheKey)) return Promise.resolve(this._indexCache.get(cacheKey));
    var self = this;
    return this._read({
      root: "statistics",
      path: ["production", "factoryStatPool", factoryIndex, "productIndices", itemId]
    }, context).then(function (result) {
      self._assertSession(context);
      var index = result.value || 0;
      if (index > 0) self._indexCache.set(cacheKey, index);
      return index;
    }).catch(function (err) {
      if (err && (err.kind === "INDEX_OUT_OF_RANGE" || err.kind === "NULL_PATH")) {
        self._assertSession(context);
        return 0;
      }
      throw err;
    });
  };

  GameQuery.prototype._readFactoryItem = function (factoryIndex, itemId, spec, context) {
    var self = this;
    var consumptionOffset = spec.consumptionIndex - spec.productionIndex;
    return this._productIndex(factoryIndex, itemId, context).then(function (poolIndex) {
      if (!poolIndex) {
        return { itemId: itemId, production: 0, consumption: 0, theoreticalProduction: 0, theoreticalConsumption: 0, hit: false };
      }
      return allReads([
        self._read({
          root: "statistics",
          path: ["production", "factoryStatPool", factoryIndex, "productPool", poolIndex, "total"],
          offset: spec.productionIndex,
          limit: consumptionOffset + 1
        }, context),
        self._read({
          root: "statistics",
          path: ["production", "factoryStatPool", factoryIndex, "productPool", poolIndex],
          select: ["refProductSpeed", "refConsumeSpeed"]
        }, context)
      ]).then(function (pair) {
        self._assertSession(context);
        var totals = (pair[0].value && pair[0].value.items) || [];
        var refs = pair[1].value || {};
        var productionTotal = toNumber(totals[0]) || 0;
        var consumptionTotal = toNumber(totals[consumptionOffset]) || 0;
        return {
          itemId: itemId,
          production: spec.perMinute ? productionTotal / spec.divisor : productionTotal,
          consumption: spec.perMinute ? consumptionTotal / spec.divisor : consumptionTotal,
          theoreticalProduction: refs.refProductSpeed || 0,
          theoreticalConsumption: refs.refConsumeSpeed || 0,
          hit: true
        };
      });
    });
  };

  GameQuery.prototype.getItemRates = function (itemId, options) {
    options = options || {};
    var windowName = options.window || "1min";
    var spec = RATE_WINDOWS[windowName];
    if (!Object.prototype.hasOwnProperty.call(RATE_WINDOWS, windowName)) throw new Error("Unknown production window: " + windowName);
    var ids = Array.isArray(itemId) ? itemId : [itemId];
    ids.forEach(function (id) {
      if (!Number.isInteger(id) || id <= 0) throw new RangeError("Item IDs must be positive integers.");
    });
    if (!ids.length) return Promise.resolve([]);
    var uniqueIds = Array.from(new Set(ids));
    var self = this;
    var context = {};
    return this._factoryIds(options.scope, context).then(function (factoryIds) {
      self._assertSession(context);
      var jobs = [];
      factoryIds.forEach(function (factoryIndex) {
        uniqueIds.forEach(function (id) { jobs.push(self._readFactoryItem(factoryIndex, id, spec, context)); });
      });
      return allReads(jobs);
    }).then(function (parts) {
      self._assertSession(context);
      var byItem = Object.create(null);
      uniqueIds.forEach(function (id) {
        byItem[id] = {
          itemId: id, window: windowName, perMinute: spec.perMinute,
          production: 0, consumption: 0, theoreticalProduction: 0, theoreticalConsumption: 0, factories: 0
        };
      });
      parts.forEach(function (part) {
        if (!part) return;
        var row = byItem[part.itemId];
        row.production += part.production;
        row.consumption += part.consumption;
        row.theoreticalProduction += part.theoreticalProduction;
        row.theoreticalConsumption += part.theoreticalConsumption;
        if (part.hit) row.factories++;
      });
      var list = ids.map(function (id) {
        var row = byItem[id];
        row.sessionId = context.sessionId;
        row.gameTick = context.gameTick;
        return row;
      });
      return Array.isArray(itemId) ? list : list[0];
    });
  };

  var SPHERE_ENERGY_FIELDS = ["energyGenCurrentTick", "energyGenOriginalCurrentTick", "energyReqCurrentTick"];

  GameQuery.prototype._readSphere = function (starIndex, energy, context) {
    var self = this;
    return this._read({
      root: "game",
      path: ["dysonSpheres", starIndex, "starData"],
      select: ["index", "id", "name", "overrideName"]
    }, context).then(function (result) {
      self._assertSession(context);
      var star = result.value || {};
      var joules = toBigInt(energy.energyGenCurrentTick);
      var original = toBigInt(energy.energyGenOriginalCurrentTick);
      var watts = joules * BigInt(TICKS_PER_SECOND);
      return {
        starIndex: starIndex,
        starId: star.id,
        name: star.overrideName || star.name || ("Star " + starIndex),
        joulesPerTick: joules.toString(),
        originalJoulesPerTick: original.toString(),
        watts: watts.toString(),
        originalWatts: (original * BigInt(TICKS_PER_SECOND)).toString(),
        wattsNumber: toNumber(watts),
        energyReqCurrentTick: energy.energyReqCurrentTick != null ? String(energy.energyReqCurrentTick) : "0",
        sessionId: context.sessionId,
        gameTick: context.gameTick
      };
    });
  };

  GameQuery.prototype._collectSpheres = function (offset, spheres, context) {
    var self = this;
    var limits = this.client.infoSnapshot && this.client.infoSnapshot.limits;
    var pageSize = limits && limits.maxPageSize || 128;
    return this._read({
      root: "game", path: ["dysonSpheres"], select: SPHERE_ENERGY_FIELDS,
      offset: offset, limit: Math.min(128, pageSize)
    }, context).then(function (page) {
      self._assertSession(context);
      var items = (page.value && page.value.items) || [];
      var jobs = [];
      items.forEach(function (entry, itemIndex) {
        var index = (page.value.offset || 0) + itemIndex;
        if (!entry) return;
        jobs.push(self._readSphere(index, entry, context));
      });
      return allReads(jobs).then(function (batch) {
        Array.prototype.push.apply(spheres, batch);
        if (page.value && page.value.hasMore) {
          var nextOffset = (page.value.offset || 0) + items.length;
          if (nextOffset <= offset) throw new ApiError({ kind: "INVALID_RESPONSE", message: "Collection page made no progress." });
          return self._collectSpheres(nextOffset, spheres, context);
        }
        return spheres;
      });
    });
  };

  GameQuery.prototype.getDysonPower = function (options) {
    options = options || {};
    var self = this;
    var context = {};
    var request;
    if (options.starIndex == null) {
      request = this._collectSpheres(0, [], context);
    } else {
      if (!Number.isInteger(options.starIndex) || options.starIndex < 0) throw new RangeError("starIndex must be a nonnegative integer.");
      request = this._read({
        root: "game", path: ["dysonSpheres", options.starIndex], select: SPHERE_ENERGY_FIELDS
      }, context).then(function (result) {
        if (!result.value) return [];
        return self._readSphere(options.starIndex, result.value, context).then(function (sphere) { return [sphere]; });
      });
    }
    return request.then(function (spheres) {
      self._assertSession(context);
      var total = 0n;
      var original = 0n;
      spheres.forEach(function (sphere) {
        total += toBigInt(sphere.watts);
        original += toBigInt(sphere.originalWatts);
      });
      return {
        sphereCount: spheres.length,
        watts: total.toString(),
        originalWatts: original.toString(),
        wattsNumber: toNumber(total),
        joulesPerTick: (total / BigInt(TICKS_PER_SECOND)).toString(),
        spheres: spheres,
        sessionId: context.sessionId,
        gameTick: context.gameTick
      };
    });
  };

  ns.GameQuery = GameQuery;
  var LCID_ALIASES = {
    en: 1033, "en-us": 1033,
    zh: 2052, "zh-cn": 2052,
    fr: 1036, "fr-fr": 1036,
    de: 1031, "de-de": 1031,
    ja: 1041, "ja-jp": 1041, "ja-ja": 1041
  };

  function normalizeLcid(language, languages) {
    if (language == null || language === "") return 1033;
    if (typeof language === "number") return language;
    var lower = String(language).toLowerCase();
    if (Object.prototype.hasOwnProperty.call(LCID_ALIASES, lower)) return LCID_ALIASES[lower];
    if (/^\d+$/.test(String(language))) return parseInt(language, 10);
    if (languages) {
      var ids = Object.keys(languages);
      for (var i = 0; i < ids.length; i++) {
        var meta = languages[ids[i]];
        if (!meta) continue;
        if (String(meta.abbr).toLowerCase() === lower || String(meta.abbr2).toLowerCase() === lower || String(meta.name).toLowerCase() === lower) {
          return parseInt(ids[i], 10);
        }
      }
    }
    return 1033;
  }

  function GameText(data, options) {
    options = options || {};
    this.data = data || { languages: {}, ids: {}, strings: {} };
    this.lcid = normalizeLcid(options.language, this.data.languages);
  }

  GameText.fromGenerated = function (options) {
    var data = (typeof globalThis !== "undefined" && globalThis.LiveStreamAssistTextData) ||
      (typeof window !== "undefined" && window.LiveStreamAssistTextData) ||
      null;
    if (!data) throw new Error("LiveStreamAssistTextData is not loaded. Include generated/dsp-text.js first.");
    return new GameText(data, options);
  };

  GameText.prototype.setLanguage = function (language) {
    this.lcid = normalizeLcid(language, this.data.languages);
    return this;
  };

  GameText.prototype.translate = function (nameKey) {
    if (nameKey == null || nameKey === "") return "";
    var row = this.data.strings && this.data.strings[nameKey];
    if (!row) return String(nameKey);
    var lcid = String(this.lcid);
    if (row[lcid] != null) return row[lcid];
    return String(nameKey);
  };

  GameText.prototype.nameKey = function (kind, id) {
    var table = this.data.ids && this.data.ids[kind];
    return table ? table[String(id)] || null : null;
  };

  GameText.prototype.name = function (kind, id) {
    var nameKey = this.nameKey(kind, id);
    return nameKey ? this.translate(nameKey) : "";
  };

  GameText.prototype.itemName = function (id) { return this.name("item", id); };
  GameText.prototype.techName = function (id) { return this.name("tech", id); };
  GameText.prototype.recipeName = function (id) { return this.name("recipe", id); };
  GameText.prototype.signalName = function (id) { return this.name("signal", id); };
  GameText.prototype.veinName = function (id) { return this.name("vein", id); };

  GameText.prototype.techDisplayName = function (id, curLevel) {
    var name = this.techName(id);
    if (!name) return "";
    if (!curLevel) return name;
    return name + this.translate("杠等级") + curLevel;
  };

  GameText.prototype.separators = function () {
    var thousand = this.translate("千分分隔符");
    var decimal = this.translate("小数点");
    return {
      thousand: thousand && thousand.length === 1 ? thousand : ",",
      decimal: decimal && decimal.length === 1 ? decimal : "."
    };
  };

  GameText.prototype.formatKmg = function (value, options) {
    return formatKmg(value, Object.assign({ decimal: this.separators().decimal, kind: "count" }, options));
  };
  GameText.prototype.formatPower = function (value, options) {
    return formatKmg(value, Object.assign({ decimal: this.separators().decimal, kind: "power" }, options));
  };
  GameText.prototype.formatEnergy = function (value, options) {
    return formatKmg(value, Object.assign({ decimal: this.separators().decimal, kind: "energy" }, options));
  };
  GameText.prototype.formatRate = function (value) {
    var seps = this.separators();
    if (value >= 10000) return this.formatKmg(Math.round(value));
    if (value >= 1000) return value.toFixed(0);
    if (value >= 100) return value.toFixed(1).replace(".", seps.decimal);
    if (value > 0) return value.toFixed(2).replace(".", seps.decimal);
    return "0";
  };

  ns.GameText = GameText;
  function GameIcons(data) {
    this.data = data || { icons: { item: {}, tech: {} } };
  }

  GameIcons.fromGenerated = function () {
    var data = (typeof globalThis !== "undefined" && globalThis.LiveStreamAssistIconData) ||
      (typeof window !== "undefined" && window.LiveStreamAssistIconData) ||
      null;
    if (!data) throw new Error("LiveStreamAssistIconData is not loaded. Include generated/dsp-icons.js first.");
    return new GameIcons(data);
  };

  GameIcons.prototype.icon = function (kind, id) {
    var table = this.data.icons && this.data.icons[kind];
    return table ? table[String(id)] || null : null;
  };

  GameIcons.prototype.itemIcon = function (id) { return this.icon("item", id); };
  GameIcons.prototype.techIcon = function (id) { return this.icon("tech", id); };

  ns.GameIcons = GameIcons;
  return ns;
});
