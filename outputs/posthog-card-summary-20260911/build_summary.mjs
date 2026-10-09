import fs from "node:fs/promises";
import path from "node:path";
import { SpreadsheetFile, Workbook } from "@oai/artifact-tool";

const sourceDir = String.raw`E:\DATA\GODOT\res\posthog`;
const outputDir = String.raw`E:\DATA\GODOT\MyMod\MeiLinMod-sts2\outputs\posthog-card-summary-20260911`;
const outputPath = path.join(outputDir, "posthog_梅铃友纪卡牌汇总.xlsx");

const configs = [
  {
    role: "梅铃",
    prefix: "CARD.MEILINMOD_",
    keyPrefix: "MEILINMOD_",
    pickFile: "meilin_pick.csv",
    winFile: "meilin_win.csv",
    cardsDir: String.raw`E:\DATA\GODOT\MyMod\MeiLinMod-sts2\MeiLinModCode\Cards`,
    localization: String.raw`E:\DATA\GODOT\MyMod\MeiLinMod-sts2\MeiLinMod\localization\zhs\cards.json`,
  },
  {
    role: "友纪",
    prefix: "CARD.YUKIMOD_",
    keyPrefix: "YUKIMOD_",
    pickFile: "yuki_pick.csv",
    winFile: "yuki_win.csv",
    cardsDir: String.raw`E:\DATA\GODOT\MyMod\YukiMod\YukiModCode\Cards`,
    localization: String.raw`E:\DATA\GODOT\MyMod\YukiMod\YukiMod\localization\zhs\cards.json`,
  },
];

function parseCsv(text) {
  const rows = [];
  let row = [];
  let field = "";
  let quoted = false;
  const value = text.replace(/^\uFEFF/, "");
  for (let i = 0; i < value.length; i++) {
    const ch = value[i];
    if (quoted) {
      if (ch === '"' && value[i + 1] === '"') {
        field += '"';
        i++;
      } else if (ch === '"') {
        quoted = false;
      } else {
        field += ch;
      }
    } else if (ch === '"') {
      quoted = true;
    } else if (ch === ",") {
      row.push(field);
      field = "";
    } else if (ch === "\n") {
      row.push(field.replace(/\r$/, ""));
      if (row.some((cell) => cell !== "")) rows.push(row);
      row = [];
      field = "";
    } else {
      field += ch;
    }
  }
  if (field !== "" || row.length) {
    row.push(field.replace(/\r$/, ""));
    rows.push(row);
  }
  const headers = rows.shift();
  return rows.map((values) => Object.fromEntries(headers.map((header, index) => [header, values[index] ?? ""])));
}

function pascalToSnake(name) {
  return name
    .replace(/(.)([A-Z][a-z]+)/g, "$1_$2")
    .replace(/([a-z0-9])([A-Z])/g, "$1_$2")
    .toUpperCase();
}

async function readLocalization(filePath) {
  const text = (await fs.readFile(filePath, "utf8")).replace(/^\uFEFF/, "");
  return JSON.parse(text);
}

async function readMetadata(config) {
  const localization = await readLocalization(config.localization);
  const entries = await fs.readdir(config.cardsDir, { withFileTypes: true });
  const result = new Map();
  for (const entry of entries) {
    if (!entry.isFile() || !entry.name.endsWith(".cs")) continue;
    const text = await fs.readFile(path.join(config.cardsDir, entry.name), "utf8");
    const classMatch = text.match(/\bclass\s+(\w+)/);
    if (!classMatch) continue;
    const suffix = pascalToSnake(classMatch[1]);
    const cardId = `${config.prefix}${suffix}`;
    const rarityMatch = text.match(/CardRarity\.(\w+)/);
    const poolMatch = text.match(/\[Pool\(typeof\((\w+)\)\)\)\]/);
    result.set(cardId, {
      name: localization[`${config.keyPrefix}${suffix}.title`] ?? suffix,
      rarity: rarityMatch?.[1] ?? "未知",
      multiplayerOnly: text.includes("CardMultiplayerConstraint.MultiplayerOnly"),
      pool: poolMatch?.[1] ?? "",
    });
  }
  return { localization, metadata: result };
}

function toInteger(value) {
  if (value === undefined || value === null || value === "") return null;
  const parsed = Number.parseInt(value, 10);
  return Number.isFinite(parsed) ? parsed : null;
}

function toRate(value) {
  if (value === undefined || value === null || value === "") return null;
  const parsed = Number.parseFloat(value);
  return Number.isFinite(parsed) ? parsed / 100 : null;
}

const rarityNames = {
  Basic: "基础",
  Common: "普通",
  Uncommon: "罕见",
  Rare: "稀有",
  Ancient: "远古",
  Token: "衍生",
  Curse: "诅咒",
  Status: "状态",
  未知: "未知",
};
const rarityOrder = new Map([["基础", 0], ["普通", 1], ["罕见", 2], ["稀有", 3], ["远古", 4], ["衍生", 5], ["状态", 6], ["诅咒", 7], ["未知", 8]]);

const allSummaries = [];
const sourceSheets = [];

for (const config of configs) {
  const pickRows = parseCsv(await fs.readFile(path.join(sourceDir, config.pickFile), "utf8"))
    .filter((row) => row.card_id.startsWith(config.prefix));
  const winRows = parseCsv(await fs.readFile(path.join(sourceDir, config.winFile), "utf8"))
    .filter((row) => row.card_id.startsWith(config.prefix));
  const { localization, metadata } = await readMetadata(config);
  const combined = new Map();

  for (const row of pickRows) {
    combined.set(row.card_id, {
      ...(combined.get(row.card_id) ?? {}),
      chosen: toInteger(row.times_chosen),
      offered: toInteger(row.times_offered),
      pickRate: toRate(row.pick_rate_pct),
    });
  }
  for (const row of winRows) {
    combined.set(row.card_id, {
      ...(combined.get(row.card_id) ?? {}),
      wins: toInteger(row.wins),
      runsWithCard: toInteger(row.total_runs_with_card),
      winRate: toRate(row.win_rate_pct),
    });
  }

  for (const [cardId, stats] of combined.entries()) {
    const suffix = cardId.slice(config.prefix.length);
    const meta = metadata.get(cardId);
    const fallbackName = localization[`${config.keyPrefix}${suffix}.title`]
      ?? (cardId === "CARD.YUKIMOD_XIAN_JIAN_ZHI_MING" ? "先见之明" : suffix);
    const notes = [];
    if (!meta) notes.push("当前源码未找到卡牌定义");
    if (stats.pickRate === undefined) notes.push("选取表无记录");
    if (stats.winRate === undefined) notes.push("胜率表无记录");
    allSummaries.push({
      role: config.role,
      name: meta?.name ?? fallbackName,
      cardId,
      rarity: rarityNames[meta?.rarity ?? "未知"] ?? (meta?.rarity ?? "未知"),
      multiplayer: meta?.multiplayerOnly ? "是" : "否",
      chosen: stats.chosen ?? null,
      offered: stats.offered ?? null,
      pickRate: stats.pickRate ?? null,
      wins: stats.wins ?? null,
      runsWithCard: stats.runsWithCard ?? null,
      winRate: stats.winRate ?? null,
      notes: notes.join("；"),
    });
  }

  sourceSheets.push({ name: `${config.role}选取`, file: config.pickFile, kind: "pick", rows: pickRows });
  sourceSheets.push({ name: `${config.role}胜率`, file: config.winFile, kind: "win", rows: winRows });
}

allSummaries.sort((a, b) =>
  a.role.localeCompare(b.role, "zh-CN")
  || (rarityOrder.get(a.rarity) ?? 99) - (rarityOrder.get(b.rarity) ?? 99)
  || Number(b.multiplayer === "是") - Number(a.multiplayer === "是")
  || a.name.localeCompare(b.name, "zh-CN")
);

const workbook = Workbook.create();
const summary = workbook.worksheets.add("汇总");
summary.showGridLines = false;
summary.tabColor = "#1F4E78";

summary.getRange("A2").values = [["梅铃与友纪卡牌统计汇总"]];
summary.getRange("A2").format.font = { name: "Arial", size: 16, bold: true, color: "#1F2937" };
summary.getRange("A3").values = [["按 card_id 合并选取与胜率数据；仅保留 MEILINMOD 和 YUKIMOD 卡牌。稀有度与多人限定标记来自当前源码。"]];
summary.getRange("A3:L3").format.font = { name: "Arial", size: 10, italic: true, color: "#64748B" };
summary.getRange("A4:L4").format.borders = { bottom: { style: "thin", color: "#94A3B8" } };

const summaryHeaders = ["角色", "卡牌名称", "卡牌ID", "稀有度", "多人限定", "选取次数", "被提供次数", "选取率", "胜利数", "持卡对局数", "胜率", "备注"];
const summaryValues = allSummaries.map((row) => [
  row.role, row.name, row.cardId, row.rarity, row.multiplayer,
  row.chosen, row.offered, row.pickRate, row.wins, row.runsWithCard, row.winRate, row.notes,
]);
summary.getRange("A5:L5").values = [summaryHeaders];
summary.getRangeByIndexes(5, 0, summaryValues.length, summaryHeaders.length).values = summaryValues;
const summaryEnd = 5 + summaryValues.length;
summary.tables.add(`A5:L${summaryEnd}`, true, "CardSummaryTable").style = "TableStyleMedium2";
summary.getRange(`A5:L${summaryEnd}`).format.font = { name: "Arial", size: 10, color: "#1F2937" };
summary.getRange("A5:L5").format = {
  fill: "#1F4E78",
  font: { name: "Arial", size: 10, bold: true, color: "#FFFFFF" },
  horizontalAlignment: "center",
  verticalAlignment: "center",
};
summary.getRange(`F6:G${summaryEnd}`).format.numberFormat = "#,##0";
summary.getRange(`H6:H${summaryEnd}`).format.numberFormat = "0.00%";
summary.getRange(`I6:J${summaryEnd}`).format.numberFormat = "#,##0";
summary.getRange(`K6:K${summaryEnd}`).format.numberFormat = "0.00%";
summary.getRange(`A6:E${summaryEnd}`).format.verticalAlignment = "center";
summary.getRange(`F6:K${summaryEnd}`).format.horizontalAlignment = "right";
summary.getRange(`A5:L${summaryEnd}`).format.autofitColumns();
summary.getRange("A:A").format.columnWidth = 9;
summary.getRange("B:B").format.columnWidth = 20;
summary.getRange("C:C").format.columnWidth = 46;
summary.getRange("D:E").format.columnWidth = 10;
summary.getRange("F:K").format.columnWidth = 13;
summary.getRange("L:L").format.columnWidth = 24;
summary.getRange(`L6:L${summaryEnd}`).format.wrapText = false;
summary.freezePanes.freezeRows(5);
summary.freezePanes.freezeColumns(3);

for (let index = 0; index < sourceSheets.length; index++) {
  const source = sourceSheets[index];
  const sheet = workbook.worksheets.add(source.name);
  sheet.showGridLines = false;
  sheet.getRange("A1").values = [[`来源：${path.join(sourceDir, source.file)}`]];
  sheet.getRange("A1:D1").format.font = { name: "Arial", size: 10, italic: true, color: "#64748B" };
  let headers;
  let values;
  if (source.kind === "pick") {
    headers = ["card_id", "times_chosen", "times_offered", "pick_rate"];
    values = source.rows.map((row) => [row.card_id, toInteger(row.times_chosen), toInteger(row.times_offered), toRate(row.pick_rate_pct)]);
  } else {
    headers = ["card_id", "wins", "total_runs_with_card", "win_rate"];
    values = source.rows.map((row) => [row.card_id, toInteger(row.wins), toInteger(row.total_runs_with_card), toRate(row.win_rate_pct)]);
  }
  sheet.getRange("A2:D2").values = [headers];
  sheet.getRangeByIndexes(2, 0, values.length, headers.length).values = values;
  const endRow = 2 + values.length;
  const table = sheet.tables.add(`A2:D${endRow}`, true, `SourceTable${index + 1}`);
  table.style = "TableStyleMedium2";
  sheet.getRange(`A2:D${endRow}`).format.font = { name: "Arial", size: 10, color: "#1F2937" };
  sheet.getRange("A2:D2").format = {
    fill: "#4472C4",
    font: { name: "Arial", size: 10, bold: true, color: "#FFFFFF" },
    horizontalAlignment: "center",
    verticalAlignment: "center",
  };
  sheet.getRange(`B3:C${endRow}`).format.numberFormat = "#,##0";
  sheet.getRange(`D3:D${endRow}`).format.numberFormat = "0.00%";
  sheet.getRange("A:A").format.columnWidth = 50;
  sheet.getRange("B:D").format.columnWidth = 20;
  sheet.freezePanes.freezeRows(2);
}

workbook.recalculate();

const inspection = await workbook.inspect({
  kind: "table",
  range: "汇总!A2:L18",
  include: "values,formulas",
  tableMaxRows: 18,
  tableMaxCols: 12,
  maxChars: 12000,
});
console.log(inspection.ndjson);

const errors = await workbook.inspect({
  kind: "match",
  searchTerm: "#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A|#NUM!|#NULL!|#SPILL!|#CALC!",
  options: { useRegex: true, maxResults: 100 },
  summary: "final formula error scan",
});
console.log(errors.ndjson);

const preview = await workbook.render({ sheetName: "汇总", range: "A1:L30", scale: 1.3, format: "png" });
await fs.writeFile(path.join(outputDir, "preview.png"), new Uint8Array(await preview.arrayBuffer()));

await fs.mkdir(outputDir, { recursive: true });
const output = await SpreadsheetFile.exportXlsx(workbook);
await output.save(outputPath);
console.log(JSON.stringify({ outputPath, summaryRows: allSummaries.length, sourceRows: sourceSheets.map((sheet) => [sheet.name, sheet.rows.length]) }));
