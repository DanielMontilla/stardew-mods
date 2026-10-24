export interface ConfigField {
	name: string;
	type: string;
	/** Parsed default, or `undefined` when the initializer isn't a simple literal. */
	default: unknown;
	/** Original initializer source when it can't be evaluated. */
	defaultCode: string | null;
	description: string;
}

type Literal = { ok: true; value: unknown } | { ok: false };

/**
 * Read the `ModConfig` properties, their default initializers, and their XML
 * doc summaries. Only literal initializers are evaluated; anything else is
 * kept as source text so the docs can still show it.
 */
export function extractConfigFields(source: string): ConfigField[] {
	const body = findClassBody(source, 'ModConfig');
	if (body === null) return [];

	const fields: ConfigField[] = [];
	const propertyPattern =
		/((?:[ \t]*\/\/\/[^\r\n]*(?:\r?\n|$))*)[ \t]*(?:public|internal|protected)[ \t]+([\w.]+(?:<[^;{}]*>)?\?*(?:\[\])?)[ \t]+(\w+)[ \t]*\{[ \t]*get;[ \t]*set;[ \t]*\}[ \t]*(?:=[ \t]*([^;]+))?;/g;

	for (const match of body.matchAll(propertyPattern)) {
		const [, docBlock, type, name, initializer] = match;
		const parsed =
			initializer === undefined
				? ({ ok: true, value: defaultValueFor(type) } satisfies Literal)
				: parseLiteral(initializer);

		fields.push({
			name,
			type: type.replace(/\s+/g, ' ').trim(),
			default: parsed.ok ? parsed.value : undefined,
			defaultCode: parsed.ok ? null : initializer!.trim(),
			description: summarizeDoc(docBlock),
		});
	}

	return fields;
}

/**
 * Collect `const` fields with literal string/number/boolean values, keyed by
 * bare name and by class name. Bare names are only useful when they're unique;
 * use `Class.Name` for ambiguous ones.
 */
export function extractConstants(sources: string[]): Record<string, unknown> {
	const found: { className: string; name: string; value: string | number | boolean }[] = [];
	const constPattern =
		/^[ \t]*(?:(?:private|public|internal|protected)\s+)?const\s+[\w.]+(?:<[^;{}]*>)?\s+(\w+)\s*=\s*([^;]+);/gm;

	for (const source of sources) {
		const classes = [...source.matchAll(/\bclass\s+(\w+)/g)].map((match) => ({
			name: match[1],
			index: match.index ?? 0,
		}));

		for (const match of source.matchAll(constPattern)) {
			const parsed = parseLiteral(match[2]);
			if (!parsed.ok) continue;
			if (!['string', 'number', 'boolean'].includes(typeof parsed.value)) continue;

			const index = match.index ?? 0;
			const className = [...classes].reverse().find((entry) => entry.index < index)?.name ?? '';

			found.push({
				className,
				name: match[1],
				value: parsed.value as string | number | boolean,
			});
		}
	}

	const constants: Record<string, unknown> = {};
	const byClass: Record<string, Record<string, unknown>> = {};

	for (const constant of found) {
		(byClass[constant.className] ??= {})[constant.name] = constant.value;

		if (!(constant.name in constants)) constants[constant.name] = constant.value;
	}

	return { ...constants, ...byClass };
}

function findClassBody(source: string, className: string): string | null {
	const match = new RegExp(`\\bclass\\s+${className}\\b[^{]*\\{`).exec(source);
	if (match === null) return null;

	const open = match.index + match[0].length - 1;
	return source.slice(open + 1, matchBrace(source, open));
}

function matchBrace(source: string, open: number): number {
	let depth = 0;

	for (let index = open; index < source.length; index++) {
		const character = source[index];
		const next = source[index + 1];

		if (character === '/' && next === '/') {
			index = source.indexOf('\n', index);
			if (index === -1) return source.length;
			continue;
		}

		if (character === '/' && next === '*') {
			const end = source.indexOf('*/', index + 2);
			index = end === -1 ? source.length : end + 1;
			continue;
		}

		if (character === '@' && next === '"') {
			index = skipVerbatimString(source, index + 1);
			continue;
		}

		if (character === '"') {
			index = skipString(source, index);
			continue;
		}

		if (character === "'") {
			index = skipChar(source, index);
			continue;
		}

		if (character === '{') depth++;
		else if (character === '}') {
			depth--;
			if (depth === 0) return index;
		}
	}

	return -1;
}

function skipString(source: string, quote: number): number {
	for (let index = quote + 1; index < source.length; index++) {
		if (source[index] === '\\') index++;
		else if (source[index] === '"') return index;
	}

	return source.length;
}

function skipVerbatimString(source: string, quote: number): number {
	for (let index = quote + 1; index < source.length; index++) {
		if (source[index] === '"') {
			if (source[index + 1] === '"') index++;
			else return index;
		}
	}

	return source.length;
}

function skipChar(source: string, quote: number): number {
	for (let index = quote + 1; index < source.length; index++) {
		if (source[index] === '\\') index++;
		else if (source[index] === "'") return index;
	}

	return source.length;
}

function parseLiteral(code: string): Literal {
	const value = code.trim();

	if (value === 'null') return { ok: true, value: null };
	if (value === 'true') return { ok: true, value: true };
	if (value === 'false') return { ok: true, value: false };

	if (/^-?\d+(?:\.\d+)?(?:[dDfFmM])?$/.test(value)) {
		return { ok: true, value: Number(value.replace(/[dDfFmM]$/, '')) };
	}

	if (/^@?"(?:[^"\\]|\\.|"")*"$/.test(value)) {
		return { ok: true, value: parseStringLiteral(value) };
	}

	if (/^(?:\[\s*\]|new\s*\(\s*\)|new\s+[\w.]+(?:<[^;{}]*>)?\s*\(\s*\))$/.test(value)) {
		return { ok: true, value: [] };
	}

	return { ok: false };
}

function parseStringLiteral(value: string): string {
	const verbatim = value.startsWith('@');
	const inner = value.slice(verbatim ? 2 : 1, -1);

	if (verbatim) return inner.replace(/""/g, '"');

	return inner.replace(/\\(["\\nrt0])/g, (_, character: string) => {
		const escapes: Record<string, string> = {
			'"': '"',
			'\\': '\\',
			n: '\n',
			r: '\r',
			t: '\t',
			'0': '\0',
		};
		return escapes[character] ?? character;
	});
}

function defaultValueFor(type: string): unknown {
	const normalized = type.replace(/\?$/, '');

	if (normalized === 'string' || normalized === 'object') return null;
	if (normalized === 'bool') return false;
	if (/^(?:byte|sbyte|short|ushort|int|uint|long|ulong|float|double|decimal)$/.test(normalized)) {
		return 0;
	}
	if (normalized.startsWith('List<') || normalized.startsWith('Dictionary<') || normalized.endsWith('[]')) {
		return [];
	}

	return null;
}

function summarizeDoc(block: string): string {
	if (block === '') return '';

	return block
		.split(/\r?\n/)
		.map((line) => line.replace(/^\s*\/\/\//, '').trim())
		.join(' ')
		.replace(/<\/?summary>/g, '')
		.replace(/<c>([\s\S]*?)<\/c>/g, '$1')
		.replace(/<see\s+cref="([^"]+)"\s*\/>/g, (_, cref: string) => cref.split('.').pop() ?? '')
		.replace(/<see\s+langword="([^"]+)"\s*\/>/g, '$1')
		.replace(/<paramref\s+name="([^"]+)"\s*\/>/g, '$1')
		.replace(/\s+/g, ' ')
		.trim();
}
