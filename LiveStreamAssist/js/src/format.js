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
