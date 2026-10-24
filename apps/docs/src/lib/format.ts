export type Row = Record<string, unknown>;

export interface TableGroup {
	name: string;
	rows: Row[];
}

export function isRow(value: unknown): value is Row {
	return typeof value === 'object' && value !== null && !Array.isArray(value);
}

/** Turn a JSON property name such as `DisplayNameKey` into `Display Name Key`. */
export function label(key: string): string {
	return key
		.replace(/([a-z0-9])([A-Z])/g, '$1 $2')
		.replace(/[_-]+/g, ' ')
		.replace(/\b\w/g, (character) => character.toUpperCase())
		.trim();
}

/** Render any JSON value as inline text, resolving translation keys when possible. */
export function formatValue(value: unknown, i18n: Record<string, string> = {}): string {
	if (value === null || value === undefined || value === '') return '—';
	if (typeof value === 'string') return i18n[value] ?? value;
	if (typeof value === 'number') return Number.isFinite(value) ? value.toLocaleString('en-US') : String(value);
	if (typeof value === 'boolean') return value ? 'yes' : 'no';
	if (Array.isArray(value)) {
		return value.length === 0 ? '—' : value.map((item) => formatItem(item, i18n)).join(', ');
	}

	if (isRow(value)) {
		const entries = Object.entries(value);
		return entries.length === 0
			? '—'
			: entries.map(([key, item]) => `${label(key)}: ${formatValue(item, i18n)}`).join(' · ');
	}

	return String(value);
}

/** Columns in first-seen order, so tables stay stable as assets change. */
export function collectColumns(rows: Row[]): string[] {
	const columns = new Set<string>();

	for (const row of rows) {
		for (const key of Object.keys(row)) columns.add(key);
	}

	return [...columns];
}

/**
 * Find every table inside an asset file. Supports a top-level array as well as
 * an object whose array properties hold the rows (e.g. `{ "Spots": [...] }`).
 */
export function tableGroups(data: unknown): TableGroup[] {
	if (Array.isArray(data)) {
		const rows = data.filter(isRow);
		return rows.length === 0 ? [] : [{ name: '', rows }];
	}

	if (!isRow(data)) return [];

	return Object.entries(data)
		.filter(([, value]) => Array.isArray(value) && value.length > 0 && value.every(isRow))
		.map(([name, value]) => ({ name, rows: value as Row[] }));
}

/** Render rows as the same table markup used by the docs stylesheet. */
export function renderTableHtml(
	rows: Row[],
	i18n: Record<string, string> = {},
	columns?: string[],
): string {
	const cols = columns ?? collectColumns(rows);
	const head = cols.map((column) => `<th scope="col">${escapeHtml(label(column))}</th>`).join('');
	const body = rows
		.map((row) => {
			const cells = cols
				.map((column) => {
					const value = row[column];
					const key = typeof value === 'string' && value in i18n ? value : undefined;
					const title = key === undefined ? '' : ` title="${escapeHtml(key)}"`;
					return `<td${title}>${escapeHtml(formatValue(value, i18n))}</td>`;
				})
				.join('');
			return `<tr>${cells}</tr>`;
		})
		.join('');

	return `<div class="json-table-wrap"><table class="json-table"><thead><tr>${head}</tr></thead><tbody>${body}</tbody></table></div>`;
}

export function escapeHtml(value: string): string {
	return value
		.replace(/&/g, '&amp;')
		.replace(/</g, '&lt;')
		.replace(/>/g, '&gt;')
		.replace(/"/g, '&quot;');
}

function formatItem(item: unknown, i18n: Record<string, string>): string {
	if (isRow(item) && typeof item.Count === 'number') {
		const { Count, ...rest } = item;
		const value = Object.values(rest)[0];
		return `${Count.toLocaleString('en-US')}× ${formatValue(value, i18n)}`;
	}

	return formatValue(item, i18n);
}
