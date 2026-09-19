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
