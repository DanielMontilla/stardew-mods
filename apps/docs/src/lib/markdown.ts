import { marked } from 'marked';
import { resolveModImage } from './mod-images';
import type { AssetFile, ModData } from './mod-data';
import { resolveValue } from './mod-data';
import {
	escapeHtml,
	formatValue,
	label,
	type Row,
	renderTableHtml,
	tableGroups,
} from './format';

const TOKEN = /\{\{\s*([^{}]+?)\s*\}\}/g;

/**
 * Render a mod markdown page, resolving `{{...}}` tokens against its data.
 *
 * Tokens with a dot (`{{config.Difficulty}}`) or a directive prefix
 * (`{{table:...}}`, `{{i18n:...}}`) are docs tokens. Bare tokens such as
 * `{{spot}}` are left untouched so SMAPI translation strings survive.
 *
 * Pass one data variant per difficulty to make values switch with the
 * site-wide mode control. Values that are identical in every mode render as
 * plain text.
 */
export function renderMarkdown(markdown: string, variants: ModData[], file = 'markdown'): string {
	const data = variants[0];

	if (data === undefined) throw new Error(`${file}: no mod data.`);

	let replaced: string;

	try {
		replaced = replaceOutsideCode(markdown, (text) =>
			replaceImages(replaceTokens(text, variants), file),
		);
	} catch (error) {
		throw new Error(`${file}: ${(error as Error).message}`, { cause: error });
	}

	const html = marked.parse(replaced, { async: false, gfm: true }) as string;

	return applyBase(html);
}

/** Render every asset file as a table section, used on the overview page. */
export function renderAllAssetTables(variants: ModData[]): string {
	const data = variants[0];
	if (data === undefined) return '';

	return renderModeBlocks(
		variants.map((variant) => ({ mode: variant.mode, html: renderAssetTables(variant) })),
		data.defaultMode,
	);
}

function replaceTokens(text: string, variants: ModData[]): string {
	const data = variants[0];

	return text.replace(TOKEN, (match, rawExpression: string) => {
		const expression = rawExpression.trim();

		if (expression.startsWith('table:')) {
			return renderTableDirective(expression.slice('table:'.length).trim(), variants);
		}

		if (expression.startsWith('requirements:')) {
			const spotId = expression.slice('requirements:'.length).trim();
			const results = variants.map((variant) => ({
				mode: variant.mode,
				html: renderSpotRequirements(spotId, variant),
			}));

			return renderModeBlocks(results, data.defaultMode);
		}

		if (expression.startsWith('i18n:')) {
			const key = expression.slice('i18n:'.length).trim();
			const value = data.i18n[key];

			if (value === undefined) {
				throw new Error(`No translation for "{{i18n:${key}}}".`);
			}

			return escapeHtml(value);
		}

		// `spot`, `gold`, `days`, ... are SMAPI translation tokens, not docs values.
		if (!/^version$/i.test(expression) && !expression.includes('.')) {
			return match;
		}

		const results = variants.map((variant) => {
			const value = resolveValue(expression, variant);

			if (value === undefined) {
				throw new Error(`Docs value "{{${expression}}}" resolved to undefined.`);
			}

			return { mode: variant.mode, text: formatValue(value, variant.i18n) };
		});

		return renderModeValues(results, data.defaultMode);
	});
}

function renderTableDirective(name: string, variants: ModData[]): string {
	const data = variants[0];
	const results = variants.map((variant) => ({
		mode: variant.mode,
		html: renderTable(name, variant),
	}));

	return renderModeBlocks(results, data.defaultMode);
}

function renderTable(name: string, data: ModData): string {
	if (name === 'config') {
		return renderConfigTable(data);
	}

	if (name === 'difficulties') {
		const rows = Object.entries(data.difficulty).map(([preset, settings]) => ({
			Label: preset,
			...(isPlainObject(settings) ? settings : {}),
		}));

		if (rows.length === 0) return '';

		const columns = ['Label', ...Object.keys(rows[0]).filter((key) => key !== 'Label')];
		return renderTableHtml(rows, data.i18n, columns);
	}

	if (name === 'spots') {
		return renderSpotsTable(data);
	}

	const assetKey = name;
	const asset = data.asset[assetKey];

	if (asset === undefined) {
		throw new Error(
			`Unknown table "{{table:${name}}}". Available: config, difficulties, spots, ${Object.keys(data.asset).join(', ')}.`,
		);
	}

	const groups = tableGroups(asset);

	if (groups.length === 0) {
		throw new Error(`"{{table:${name}}}" doesn't contain tabular data.`);
	}

	return groups
		.map(
			(group) =>
				`${group.name === '' ? '' : `<h3>${escapeHtml(label(group.name))}</h3>`}${renderTableHtml(group.rows, data.i18n)}`,
		)
		.join('\n');
}

function renderConfigTable(data: ModData): string {
	if (data.configFields.length === 0) return '';

	const rows: Row[] = data.configFields.map((field) => ({
		Option: field.name,
		Type: field.type,
		Default: defaultLabel(field.default, field.defaultCode),
		Description: field.description,
	}));

	return renderTableHtml(rows, data.i18n, ['Option', 'Type', 'Default', 'Description']);
}

function renderAssetTables(data: ModData): string {
	return data.assetFiles.map((file) => renderAssetSection(file, data)).join('\n');
}

function renderAssetSection(file: AssetFile, data: ModData): string {
	const tables =
		file.key === 'minecart-spots'
			? renderSpotsTable(data)
			: tableGroups(file.data)
					.map(
						(group) =>
							`${group.name === '' ? '' : `<h3>${escapeHtml(label(group.name))}</h3>`}${renderTableHtml(group.rows, data.i18n)}`,
					)
					.join('\n');

	return [
		`<section>`,
		`<h2>Data: <code>${escapeHtml(file.path)}</code></h2>`,
		tables,
		`<details class="mod-raw"><summary>Raw JSON</summary><pre><code>${escapeHtml(JSON.stringify(file.data, null, 2))}</code></pre></details>`,
		`</section>`,
	].join('\n');
}

/** A compact route table: unlock stages collapse into a details element. */
function renderSpotsTable(data: ModData): string {
	const rows = findSpotRows(data);

	if (rows.length === 0) return '';

	const body = rows
		.map((spot) => {
			const nameKey = typeof spot.DisplayNameKey === 'string' ? spot.DisplayNameKey : '';
			const name = data.i18n[nameKey] ?? (nameKey || String(spot.Id ?? ''));
			const location = typeof spot.TargetLocation === 'string' ? spot.TargetLocation : '';
			const tile = isPlainObject(spot.TargetTile)
				? `(${spot.TargetTile.X}, ${spot.TargetTile.Y})`
				: '';
			const unlock = isPlainObject(spot.Unlock) ? spot.Unlock : {};
			const steps = Array.isArray(unlock.Steps) ? unlock.Steps.filter(isPlainObject) : [];
			const ride =
				typeof spot.RidePrice === 'number'
					? `${spot.RidePrice.toLocaleString('en-US')}g`
					: '—';
			const discovery =
				typeof unlock.Condition === 'string' && unlock.Condition !== ''
					? `<code>${escapeHtml(unlock.Condition)}</code>`
					: '—';
			const unlockCell =
				steps.length > 0
					? renderSteps(steps, data.i18n)
					: renderSingleUnlock(unlock, data.i18n);

			return [
				`<tr>`,
				`<td>${escapeHtml(name)}</td>`,
				`<td>${escapeHtml(location)} ${escapeHtml(tile)}</td>`,
				`<td>${unlockCell}</td>`,
				`<td>${escapeHtml(ride)}</td>`,
				`<td>${discovery}</td>`,
				`</tr>`,
			].join('');
		})
		.join('');

	return `<div class="json-table-wrap"><table class="json-table"><thead><tr><th scope="col">Route</th><th scope="col">Destination</th><th scope="col">Unlock</th><th scope="col">Ride</th><th scope="col">Discovery</th></tr></thead><tbody>${body}</tbody></table></div>`;
}

function renderSteps(steps: Record<string, unknown>[], i18n: Record<string, string>): string {
	const totalGold = steps.reduce(
		(total, step) => total + (typeof step.Gold === 'number' ? step.Gold : 0),
		0,
	);

	const items = steps
		.map((step) => {
			const nameKey = typeof step.NameKey === 'string' ? step.NameKey : '';
			const name = i18n[nameKey] ?? (nameKey || String(step.Id ?? ''));
			const parts = [
				typeof step.Gold === 'number' ? `${step.Gold.toLocaleString('en-US')}g` : '',
				Array.isArray(step.Items) && step.Items.length > 0
					? formatValue(step.Items, i18n)
					: '',
				typeof step.WaitDays === 'number' && step.WaitDays > 0
					? `wait ${step.WaitDays}d`
					: '',
			].filter(Boolean);

			return `<li><strong>${escapeHtml(name)}</strong>${parts.length > 0 ? ` — ${escapeHtml(parts.join(' · '))}` : ''}</li>`;
		})
		.join('');

	return `<details><summary>${steps.length} steps · ${totalGold.toLocaleString('en-US')}g</summary><ol class="spot-steps">${items}</ol></details>`;
}

function renderSingleUnlock(unlock: Record<string, unknown>, i18n: Record<string, string>): string {
	const parts = [
		typeof unlock.Gold === 'number' ? `${unlock.Gold.toLocaleString('en-US')}g` : '',
		Array.isArray(unlock.Items) && unlock.Items.length > 0
			? formatValue(unlock.Items, i18n)
			: '',
	].filter(Boolean);

	return parts.length === 0 ? '—' : escapeHtml(parts.join(' + '));
}

/** A route fact card plus one card per unlock stage, for a spot detail page. */
function renderSpotRequirements(spotId: string, data: ModData): string {
	const spot = Object.entries(data.spot).find(
		([id]) => id.toLowerCase() === spotId.toLowerCase(),
	)?.[1];

	if (!isPlainObject(spot)) {
		throw new Error(`Unknown spot "{{requirements:${spotId}}}".`);
	}

	const nameKey = typeof spot.DisplayNameKey === 'string' ? spot.DisplayNameKey : '';
	const name = data.i18n[nameKey] ?? (nameKey || String(spot.Id ?? spotId));
	const unlock = isPlainObject(spot.Unlock) ? spot.Unlock : {};
	const steps = Array.isArray(unlock.Steps) ? unlock.Steps.filter(isPlainObject) : [];
	const tile = isPlainObject(spot.TargetTile)
		? `${spot.TargetTile.X}, ${spot.TargetTile.Y}`
		: '';
	const destination = [String(spot.TargetLocation ?? ''), tile === '' ? '' : `(${tile})`]
		.filter(Boolean)
		.join(' ');
	const ride =
		typeof spot.RidePrice === 'number' ? `${spot.RidePrice.toLocaleString('en-US')}g` : '—';
	const discovery =
		typeof unlock.Condition === 'string' && unlock.Condition !== ''
			? `<code>${escapeHtml(unlock.Condition)}</code>`
			: 'Not required';

	const stages =
		steps.length > 0
			? `<h3>Unlock stages</h3><ol class="spot-stages">${steps
					.map((step, index) => renderStage(step, index, data.i18n))
					.join('')}</ol>`
			: `<h3>Unlock cost</h3><p class="spot-stage__cost">${escapeHtml(singleUnlockCost(unlock, data.i18n))}</p>`;

	return [
		`<section class="spot-card">`,
		`<dl class="spot-card__facts">`,
		fact('Route', escapeHtml(name)),
		fact('Destination', escapeHtml(destination)),
		fact('Ride price', escapeHtml(ride)),
		fact('Discovery', discovery),
		`</dl>`,
		stages,
		`</section>`,
	].join('');
}

function fact(term: string, valueHtml: string): string {
	return `<div><dt>${escapeHtml(term)}</dt><dd>${valueHtml}</dd></div>`;
}

function renderStage(
	step: Record<string, unknown>,
	index: number,
	i18n: Record<string, string>,
): string {
	const nameKey = typeof step.NameKey === 'string' ? step.NameKey : '';
	const descriptionKey = typeof step.DescriptionKey === 'string' ? step.DescriptionKey : '';
	const name = i18n[nameKey] ?? (nameKey || `Stage ${index + 1}`);
	const description = descriptionKey === '' ? '' : (i18n[descriptionKey] ?? '');
	const cost = [
		typeof step.Gold === 'number' && step.Gold > 0
			? `${step.Gold.toLocaleString('en-US')}g`
			: '',
		Array.isArray(step.Items) && step.Items.length > 0 ? formatValue(step.Items, i18n) : '',
	]
		.filter(Boolean)
		.join(' + ');
	const condition =
		typeof step.Condition === 'string' && step.Condition !== ''
			? `<p class="spot-stage__condition">Requires <code>${escapeHtml(step.Condition)}</code>.</p>`
			: '';
	const npcs = Array.isArray(step.NpcFlow)
		? step.NpcFlow
				.filter(isPlainObject)
				.map((entry) => String(entry.Npc ?? ''))
				.filter(Boolean)
		: [];
	const npcFlow =
		npcs.length > 0
			? `<p class="spot-stage__npc">Talk to ${escapeHtml(npcs.join(', then '))}.</p>`
			: '';
	const wait =
		typeof step.WaitDays === 'number' && step.WaitDays > 0
			? `Next stage after ${step.WaitDays} day${step.WaitDays === 1 ? '' : 's'}.`
			: '';

	return [
		`<li class="spot-stage">`,
		`<div class="spot-stage__index" aria-hidden="true">${index + 1}</div>`,
		`<div class="spot-stage__body">`,
		`<h4>${escapeHtml(name)}</h4>`,
		description === '' ? '' : `<p>${escapeHtml(description)}</p>`,
		npcFlow,
		`<p class="spot-stage__cost">${escapeHtml(cost || 'Free')}</p>`,
		condition,
		wait === '' ? '' : `<p class="spot-stage__wait">${escapeHtml(wait)}</p>`,
		`</div>`,
		`</li>`,
	].join('');
}

function singleUnlockCost(
	unlock: Record<string, unknown>,
	i18n: Record<string, string>,
): string {
	const parts = [
		typeof unlock.Gold === 'number' && unlock.Gold > 0
			? `${unlock.Gold.toLocaleString('en-US')}g`
			: '',
		Array.isArray(unlock.Items) && unlock.Items.length > 0 ? formatValue(unlock.Items, i18n) : '',
	].filter(Boolean);

	return parts.length > 0 ? parts.join(' + ') : 'Free';
}

function findSpotRows(data: ModData): Row[] {
	for (const group of tableGroups(data.asset['minecart-spots'])) {
		if (group.rows.length > 0) return group.rows;
	}

	return [];
}

function renderModeValues(
	results: { mode: string; text: string }[],
	defaultMode: string,
): string {
	const texts = results.map((result) => result.text);

	if (new Set(texts).size === 1) return escapeHtml(texts[0]);

	const defaultKey = modeKey(
		results.some((result) => modeKey(result.mode) === modeKey(defaultMode))
			? defaultMode
			: results[0].mode,
	);
	const parts = results.map((result) => {
		const key = modeKey(result.mode);
		const marker = key === defaultKey ? ' data-mode-default' : '';
		return `<span data-mode-text="${escapeHtml(key)}"${marker}>${escapeHtml(result.text)}</span>`;
	});

	return `<span class="mode-value">${parts.join('')}</span>`;
}

function renderModeBlocks(
	results: { mode: string; html: string }[],
	defaultMode: string,
): string {
	const htmls = results.map((result) => result.html);

	if (new Set(htmls).size === 1) return htmls[0];

	const defaultKey = modeKey(
		results.some((result) => modeKey(result.mode) === modeKey(defaultMode))
			? defaultMode
			: results[0].mode,
	);

	return results
		.map((result) => {
			const key = modeKey(result.mode);
			const marker = key === defaultKey ? ' data-mode-default' : '';
			return `<div class="mode-block" data-mode-block="${escapeHtml(key)}"${marker}>${result.html}</div>`;
		})
		.join('\n');
}

function modeKey(mode: string): string {
	return mode.trim().toLowerCase();
}

function defaultLabel(value: unknown, code: string | null): string {
	if (code !== null) return code;
	if (value === null || value === undefined) return 'null';
	if (Array.isArray(value)) return value.length === 0 ? '[]' : JSON.stringify(value);
	if (typeof value === 'string') return JSON.stringify(value);

	return String(value);
}

/**
 * Resolve tokens outside fenced code blocks and inline code spans, so examples
 * can demonstrate the syntax without being evaluated.
 */
function replaceOutsideCode(markdown: string, transform: (text: string) => string): string {
	let inFence = false;

	return markdown
		.split(/\r?\n/)
		.map((line) => {
			if (/^\s*(```|~~~)/.test(line)) {
				inFence = !inFence;
				return line;
			}

			if (inFence) return line;

			return line
				.split(/(`+[^`]*`+)/g)
				.map((part) => (part.startsWith('`') ? part : transform(part)))
				.join('');
		})
		.join('\n');
}

const IMAGE = /!\[([^\]]*)\]\(([^)\s]+)(\s+"[^"]*")?\)/g;
const basePath = import.meta.env.BASE_URL.replace(/\/+$/, '');

/** Resolve mod-relative Markdown images to their bundled URLs. */
function replaceImages(markdown: string, file: string): string {
	return markdown.replace(IMAGE, (match, alt: string, source: string, title = '') => {
		if (/^(?:[a-z][a-z0-9+.-]*:|\/\/|#|\/)/i.test(source)) return match;

		return `![${alt}](${resolveModImage(file, source)}${title})`;
	});
}

/** Prefix root-relative links and images with the deployment base path. */
function applyBase(html: string): string {
	if (basePath === '') return html;

	const prefix = `${basePath}/`;
	return html.replace(
		/\b(href|src)="\/(?!\/)/g,
		(_, attribute: string) => `${attribute}="${prefix}`,
	);
}

function isPlainObject(value: unknown): value is Record<string, unknown> {
	return typeof value === 'object' && value !== null && !Array.isArray(value);
}
