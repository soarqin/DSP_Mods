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

  GameQuery.prototype.getClusterString = function () {
    var self = this;
    var context = {};
    return this._read({ root: "game", path: ["gameDesc", "clusterString"] }, context).then(function (result) {
      self._assertSession(context);
      return {
        clusterString: result.value == null ? "" : String(result.value),
        sessionId: result.sessionId,
        gameTick: result.gameTick
      };
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
