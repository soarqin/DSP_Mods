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
