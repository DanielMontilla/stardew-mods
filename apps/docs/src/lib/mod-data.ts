import { readFile } from 'node:fs/promises';
import path from 'node:path';
import { type ConfigField, extractConfigFields, extractConstants } from './csharp';
import { listFiles } from './files';
import { isRow, type Row, tableGroups } from './format';
import { getMods, type Mod, type ModManifest } from './mods';

export interface AssetFile {
	/** Base file name without `.json`, e.g. `minecart-spots`. */
	key: string;
	/** Path relative to the mod folder. */
	path: string;
	data: unknown;
}

/** Everything a mod page can pull values from, for one difficulty mode. */
export interface ModData {
	manifest: ModManifest;
	version: string;
	config: Record<string, unknown>;
	configFields: ConfigField[];
	constants: Record<string, unknown>;
	asset: Record<string, unknown>;
	assetFiles: AssetFile[];
	spot: Record<string, unknown>;
	difficulty: Record<string, unknown>;
	i18n: Record<string, string>;
	/** The difficulty this variant was built for. */
	mode: string;
	/** Difficulty labels defined by this mod, sorted from cheapest to most expensive. */
	modes: string[];
	/** The mode new installs start with, from the `Difficulty` config default. */
	defaultMode: string;
}

/**
 * Build one data context per difficulty preset. Values that the mod scales at
 * runtime (unlock gold, item counts, ride prices, discovery conditions) are
 * precomputed here so the static site can switch modes without shipping the
 * mod's logic to the browser.
 */
export async function buildModVariants(mod: Mod): Promise<ModData[]> {
	const sources = await readCsharpSources(mod.directory);
	const configSource =
		sources.find(({ file }) => path.basename(file) === 'ModConfig.cs')?.code ?? '';
	const configFields = extractConfigFields(configSource);
	const constants = extractConstants(sources.map(({ code }) => code));

	const baseAssetFiles = mod.assets.map((asset) => ({
		key: path.basename(asset.path, path.extname(asset.path)),
		path: asset.path,
		data: asset.data,
	}));
	const baseAsset = Object.fromEntries(baseAssetFiles.map((file) => [file.key, file.data]));
	const difficulties = isRow(baseAsset['difficulties']) ? baseAsset['difficulties'] : {};
	const modes = orderModes(difficulties);

	const configuredDifficulty = configFields.find(
		(field) => field.name.toLowerCase() === 'difficulty',
	)?.default;
	const defaultMode = resolveDefaultMode(configuredDifficulty, modes);

	const shared = {
		manifest: mod.manifest,
		version: mod.version,
		config: Object.fromEntries(configFields.map((field) => [field.name, field.default])),
		configFields,
		constants,
		difficulty: difficulties,
		i18n: mod.i18n,
		modes,
		defaultMode,
	};

	if (modes.length === 0) {
		return [
			makeVariant(shared, 'Default', baseAssetFiles, null),
		];
	}

	return modes.map((mode) =>
		makeVariant(shared, mode, baseAssetFiles, isRow(difficulties[mode]) ? difficulties[mode] : null),
	);
}

/** Union of every mod's difficulty labels, for the site-wide switcher. */
export async function getSiteModes(mods: Mod[]): Promise<{ modes: string[]; defaultMode: string }> {
	const modes: string[] = [];
	let defaultMode = '';

	for (const mod of mods) {
		const variants = await buildModVariants(mod);

		for (const variant of variants) {
			if (variant.modes.length === 0) continue;
			if (!modes.includes(variant.mode)) modes.push(variant.mode);
			if (defaultMode === '') defaultMode = variant.defaultMode;
		}
	}

	return { modes, defaultMode: defaultMode || modes[0] || '' };
}

let repoModes: Promise<{ modes: string[]; defaultMode: string }> | undefined;

/** Cached site modes for components rendered on every page. */
export function getRepoModes(): Promise<{ modes: string[]; defaultMode: string }> {
	repoModes ??= getMods().then(getSiteModes);
	return repoModes;
}

const ROOTS = new Set([
	'asset',
	'config',
	'const',
	'difficulty',
	'i18n',
	'manifest',
	'spot',
	'version',
]);

/**
 * Resolve a dotted path such as `config.Difficulty` or
 * `spot.Desert.Unlock.Items[0].Count` against a mod's data. Lookups are
 * case-insensitive, and arrays can be indexed by number or by an `Id` field.
 */
export function resolveValue(expression: string, data: ModData): unknown {
	const segments = expression.split('.').filter(Boolean);

	if (segments.length === 0) {
		throw new Error('Empty docs value.');
	}

	if (!ROOTS.has(segments[0].toLowerCase())) {
		throw new Error(
			`Unknown docs value root "${segments[0]}". Valid roots: ${[...ROOTS].join(', ')}.`,
		);
	}

	let current: unknown = {
		manifest: data.manifest,
		version: data.version,
		config: data.config,
		const: data.constants,
		asset: data.asset,
		spot: data.spot,
		difficulty: data.difficulty,
		i18n: data.i18n,
	};
	const walked: string[] = [];

	for (const segment of segments) {
		const match = /^([^[\]]+)(?:\[(\d+)\])?$/.exec(segment);

		if (match === null) {
			throw new Error(`Invalid docs value segment "${segment}".`);
		}

		walked.push(match[1]);
		current = child(current, match[1], walked.join('.'));

		if (match[2] !== undefined) {
			walked.push(`[${match[2]}]`);
			current = child(current, match[2], walked.join('.'));
		}
	}

	return current;
}

interface SharedData {
	manifest: ModManifest;
	version: string;
	config: Record<string, unknown>;
	configFields: ConfigField[];
	constants: Record<string, unknown>;
	difficulty: Record<string, unknown>;
	i18n: Record<string, string>;
	modes: string[];
	defaultMode: string;
}

function makeVariant(
	shared: SharedData,
	mode: string,
	baseAssetFiles: AssetFile[],
	settings: Record<string, unknown> | null,
): ModData {
	const assetFiles = baseAssetFiles.map((file) =>
		file.key === 'minecart-spots' && settings !== null
			? { ...file, data: scaleSpots(file.data, settings) }
			: file,
	);
	const asset = Object.fromEntries(assetFiles.map((file) => [file.key, file.data]));

	return {
		...shared,
		mode,
		asset,
		assetFiles,
		spot: indexRows(findRows(asset['minecart-spots'])),
	};
}

function orderModes(difficulties: Record<string, unknown>): string[] {
	return Object.entries(difficulties)
		.filter(([, settings]) => isRow(settings))
		.sort(([labelA, settingsA], [labelB, settingsB]) => {
			const multiplierA = numberOf(settingsA, 'UnlockGoldMultiplier');
			const multiplierB = numberOf(settingsB, 'UnlockGoldMultiplier');
			return multiplierA - multiplierB || labelA.localeCompare(labelB);
		})
		.map(([label]) => label);
}

function resolveDefaultMode(configured: unknown, modes: string[]): string {
	if (typeof configured === 'string') {
		const match = modes.find((mode) => mode.toLowerCase() === configured.toLowerCase());
		if (match !== undefined) return match;
	}

	return modes[0] ?? 'Default';
}

function scaleSpots(data: unknown, settings: Record<string, unknown>): unknown {
	if (!isRow(data) || !Array.isArray(data.Spots)) return data;

	const goldMultiplier = numberOf(settings, 'UnlockGoldMultiplier', 1);
	const itemMultiplier = numberOf(settings, 'UnlockItemMultiplier', 1);
	const rideMultiplier = numberOf(settings, 'RidePriceMultiplier', 1);
	const requireDiscovery = settings.RequireDiscovery !== false;

	return {
		...data,
		Spots: data.Spots.map((spot) =>
			isRow(spot)
				? scaleSpot(spot, { goldMultiplier, itemMultiplier, rideMultiplier, requireDiscovery })
				: spot,
		),
	};
}

interface Multipliers {
	goldMultiplier: number;
	itemMultiplier: number;
	rideMultiplier: number;
	requireDiscovery: boolean;
}

function scaleSpot(spot: Row, multipliers: Multipliers): Row {
	const unlock = isRow(spot.Unlock) ? { ...spot.Unlock } : {};

	// Spots with steps ignore the top-level gold/items, so only scale values that exist.
	if (unlock.Gold !== undefined) unlock.Gold = scaleGold(unlock.Gold, multipliers.goldMultiplier);
	if (unlock.Items !== undefined) unlock.Items = scaleItems(unlock.Items, multipliers.itemMultiplier);

	if (!multipliers.requireDiscovery) unlock.Condition = null;

	if (Array.isArray(unlock.Steps)) {
		unlock.Steps = unlock.Steps.map((step) => {
			if (!isRow(step)) return step;

			const scaled = { ...step };
			if (step.Gold !== undefined) scaled.Gold = scaleGold(step.Gold, multipliers.goldMultiplier);
			if (step.Items !== undefined) scaled.Items = scaleItems(step.Items, multipliers.itemMultiplier);

			return scaled;
		});
	}

	return {
		...spot,
		RidePrice: scaleGold(spot.RidePrice, multipliers.rideMultiplier),
		Unlock: unlock,
	};
}

/** Mirrors `MinecartSpotRepository.ScaleGold`: round away from zero, never negative. */
function scaleGold(value: unknown, multiplier: number): number {
	return Math.max(0, Math.round(Number(value ?? 0) * multiplier));
}

/** Mirrors `MinecartSpotRepository.ScaleCount`: round up, never below one. */
function scaleItems(items: unknown, multiplier: number): unknown {
	if (!Array.isArray(items)) return items;

	return items.map((item) =>
		isRow(item) && typeof item.Count === 'number'
			? { ...item, Count: Math.max(1, Math.ceil(item.Count * multiplier)) }
			: item,
	);
}

function numberOf(settings: unknown, key: string, fallback = Number.POSITIVE_INFINITY): number {
	const value = isRow(settings) ? settings[key] : undefined;
	return typeof value === 'number' ? value : fallback;
}

function child(value: unknown, key: string, path: string): unknown {
	if (Array.isArray(value)) {
		if (/^\d+$/.test(key)) {
			const item = value[Number(key)];
			if (item !== undefined) return item;
		} else {
			const match = value.find(
				(item) =>
					isRow(item) &&
					['Id', 'Label', 'Name'].some(
						(property) =>
							typeof item[property] === 'string' &&
							item[property].toLowerCase() === key.toLowerCase(),
					),
			);

			if (match !== undefined) return match;
		}

		throw new Error(`Docs value "${path}" has no "${key}" entry.`);
	}

	if (isRow(value)) {
		const entry = Object.entries(value).find(([name]) => name.toLowerCase() === key.toLowerCase());
		if (entry !== undefined) return entry[1];

		throw new Error(`Docs value "${path}" has no "${key}" property.`);
	}

	throw new Error(`Docs value "${path}" can't read "${key}" from ${typeof value}.`);
}

function findRows(data: unknown): Row[] {
	for (const group of tableGroups(data)) {
		if (group.rows.length > 0) return group.rows;
	}

	return [];
}

function indexRows(rows: Row[]): Record<string, unknown> {
	const index: Record<string, unknown> = {};

	for (const row of rows) {
		const key = row.Id ?? row.Label ?? row.Name;

		if (typeof key === 'string') index[key] = row;
	}

	return index;
}

async function readCsharpSources(directory: string): Promise<{ file: string; code: string }[]> {
	const files = await listFiles(directory, (fileName) => fileName.endsWith('.cs'));

	return Promise.all(files.map(async (file) => ({ file, code: await readFile(file, 'utf8') })));
}
