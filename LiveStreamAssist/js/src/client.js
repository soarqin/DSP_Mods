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
