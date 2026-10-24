import { statSync } from 'node:fs';
import { readdir, readFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import matter from 'gray-matter';
import { z } from 'zod';
import { listFiles } from './files';

/** Absolute path to the repository's `mods/` directory. */
export const MODS_ROOT = findModsRoot();

/** Build output and dependency folders that are not mods. */
const SKIPPED_DIRECTORIES = new Set(['bin', 'obj', 'node_modules']);

const manifestSchema = z.object({
	Name: z.string().min(1),
	Author: z.string().min(1),
	Version: z.string().min(1),
	Description: z.string(),
	UniqueID: z.string().min(1),
	EntryDll: z.string().min(1),
	MinimumApiVersion: z.string().optional(),
});

const i18nSchema = z.record(z.string(), z.string());

export type ModManifest = z.infer<typeof manifestSchema>;

export interface ModAsset {
	/** Path relative to the mod folder, using forward slashes, e.g. `assets/minecart-spots.json`. */
	path: string;
	data: unknown;
}

export interface ModPage {
	/** URL segment under the mod, or an empty string for the overview. */
	slug: string;
	title: string;
	description: string;
	/** Sort order in the sidebar; lower comes first. */
	order: number;
	/** Markdown body with frontmatter and the leading title removed. */
	body: string;
	file: string;
	isOverview: boolean;
}

export interface Mod {
	/** URL-safe identifier derived from the folder name. */
	slug: string;
	folder: string;
	directory: string;
	manifest: ModManifest;
	/** Version from the `.csproj`, because the manifest keeps a `%ProjectVersion%` token. */
	version: string;
	/** README body without its leading title. */
	readme: string | null;
	assets: ModAsset[];
	i18n: Record<string, string>;
	/** Overview first, then the mod's `docs` pages sorted by frontmatter order. */
	pages: ModPage[];
}

/**
 * Load every mod in `mods/` that has a `manifest.json`, sorted by display name.
 *
 * This reads from disk rather than importing mod files, so new mods show up
 * without touching this app. The Astro dev server needs a restart to rebuild
 * the sidebar, but page data itself is re-read on every request.
 */
export async function getMods(): Promise<Mod[]> {
	let entries;

	try {
		entries = await readdir(MODS_ROOT, { withFileTypes: true });
	} catch (error) {
		if (isMissingFile(error)) return [];
		throw error;
	}

	const mods = await Promise.all(
		entries
			.filter(
				(entry) =>
					entry.isDirectory() && !entry.name.startsWith('.') && !SKIPPED_DIRECTORIES.has(entry.name),
			)
			.map((entry) => loadMod(entry.name, path.join(MODS_ROOT, entry.name))),
	);

	return mods
		.filter((mod): mod is Mod => mod !== null)
		.sort((a, b) => a.manifest.Name.localeCompare(b.manifest.Name));
}

async function loadMod(folder: string, directory: string): Promise<Mod | null> {
	const manifestFile = path.join(directory, 'manifest.json');
	const rawManifest = await readOptionalJson(manifestFile);

	if (rawManifest === null) return null;

	const parsedManifest = manifestSchema.safeParse(rawManifest);
	if (!parsedManifest.success) {
		const issues = parsedManifest.error.issues
			.map((issue) => `  ${issue.path.join('.') || '(root)'}: ${issue.message}`)
			.join('\n');
		throw new Error(`Invalid ${manifestFile}:\n${issues}`);
	}

	const manifest = parsedManifest.data;
	const files = await readdir(directory);
	const csproj = files.find((file) => file.endsWith('.csproj'));
	const projectVersion = csproj ? await readProjectVersion(path.join(directory, csproj)) : undefined;
	const resolvedVersion = projectVersion ?? manifest.Version;

	const rawI18n = await readOptionalJson(path.join(directory, 'i18n', 'default.json'));
	const readme = await readOptionalText(path.join(directory, 'README.md'));
	const readmeBody = readme === null ? '' : stripTitle(readme);

	return {
		slug: toSlug(folder),
		folder,
		directory,
		manifest,
		version: /^%.*%$/.test(resolvedVersion) ? '0.0.0' : resolvedVersion,
		readme: readme === null ? null : readmeBody,
		assets: await readAssets(directory),
		i18n: rawI18n === null ? {} : i18nSchema.parse(rawI18n),
		pages: await readPages(directory, manifest, readmeBody),
	};
}

async function readPages(
	directory: string,
	manifest: ModManifest,
	readmeBody: string,
): Promise<ModPage[]> {
	const docsDirectory = path.join(directory, 'docs');
	const files = (
		await listFiles(docsDirectory, (fileName) => fileName.endsWith('.md'))
	).filter((file) => !path.basename(file).startsWith('_'));
	const readmeFile = path.join(directory, 'README.md');

	const overview: ModPage = {
		slug: '',
		title: manifest.Name,
		description: manifest.Description,
		order: -1,
		body: readmeBody,
		file: readmeFile,
		isOverview: true,
	};

	const pages = await Promise.all(
		files.map(async (file) => {
			const source = await readFile(file, 'utf8');
			let parsed: ReturnType<typeof matter>;

			try {
				parsed = matter(source);
			} catch (error) {
				throw new Error(`Invalid frontmatter in ${file}: ${(error as Error).message}`, {
					cause: error,
				});
			}

			const relative = path.relative(docsDirectory, file).replace(/\\/g, '/');
			const title = typeof parsed.data.title === 'string' ? parsed.data.title : titleFromFile(file);

			return {
				slug: pageSlug(relative),
				title,
				description: typeof parsed.data.description === 'string' ? parsed.data.description : '',
				order: typeof parsed.data.order === 'number' ? parsed.data.order : 100,
				body: stripTitle(parsed.content),
				file,
				isOverview: false,
			} satisfies ModPage;
		}),
	);

	return [
		overview,
		...pages.sort((a, b) => a.order - b.order || a.title.localeCompare(b.title)),
	];
}

function pageSlug(relativePath: string): string {
	let segments = relativePath.replace(/\.md$/i, '').split('/');

	if (segments.length > 1 && segments[segments.length - 1].toLowerCase() === 'index') {
		segments = segments.slice(0, -1);
	}

	return segments.map(toSlug).filter(Boolean).join('/') || 'index';
}

async function readAssets(directory: string, relativeDirectory = 'assets'): Promise<ModAsset[]> {
	let entries;

	try {
		entries = await readdir(path.join(directory, relativeDirectory), { withFileTypes: true });
	} catch (error) {
		if (isMissingFile(error)) return [];
		throw error;
	}

	const assets = await Promise.all(
		entries.map(async (entry): Promise<ModAsset[]> => {
			const relativePath = `${relativeDirectory}/${entry.name}`;

			if (entry.isDirectory()) return readAssets(directory, relativePath);
			if (!entry.name.endsWith('.json')) return [];

			const file = path.join(directory, relativePath);

			try {
				return [{ path: relativePath, data: JSON.parse(await readFile(file, 'utf8')) }];
			} catch (error) {
				throw new Error(`Cannot read ${file}: ${(error as Error).message}`, { cause: error });
			}
		}),
	);

	return assets.flat().sort((a, b) => a.path.localeCompare(b.path));
}

async function readProjectVersion(file: string): Promise<string | undefined> {
	const source = await readFile(file, 'utf8');
	return /<Version>\s*([^<]+?)\s*<\/Version>/.exec(source)?.[1].trim() || undefined;
}

async function readOptionalText(file: string): Promise<string | null> {
	try {
		return await readFile(file, 'utf8');
	} catch (error) {
		if (isMissingFile(error)) return null;
		throw error;
	}
}

async function readOptionalJson(file: string): Promise<unknown | null> {
	const text = await readOptionalText(file);

	if (text === null) return null;

	try {
		return JSON.parse(text);
	} catch (error) {
		throw new Error(`Invalid JSON in ${file}: ${(error as Error).message}`, { cause: error });
	}
}

function titleFromFile(file: string): string {
	return path
		.basename(file, path.extname(file))
		.replace(/[-_]+/g, ' ')
		.replace(/\b\w/g, (character) => character.toUpperCase());
}

function stripTitle(markdown: string): string {
	return markdown.replace(/^#\s+[^\r\n]*\r?\n+/, '').trim();
}

function toSlug(value: string): string {
	return value
		.replace(/([a-z0-9])([A-Z])/g, '$1-$2')
		.replace(/[^a-zA-Z0-9]+/g, '-')
		.replace(/^-+|-+$/g, '')
		.toLowerCase();
}

/**
 * Walk up from the current working directory (and this module's location) until
 * a `mods/` directory is found. Bundling rewrites `import.meta.url`, so the
 * working directory is the reliable starting point.
 */
function findModsRoot(): string {
	const starts = [process.cwd(), path.dirname(fileURLToPath(import.meta.url))];

	for (const start of starts) {
		let current = start;

		while (true) {
			const candidate = path.join(current, 'mods');

			if (isDirectory(candidate)) return candidate;

			const parent = path.dirname(current);
			if (parent === current) break;
			current = parent;
		}
	}

	throw new Error('Could not find the repo `mods/` directory. Run the docs app from inside the repository.');
}

function isDirectory(candidate: string): boolean {
	try {
		return statSync(candidate).isDirectory();
	} catch {
		return false;
	}
}

function isMissingFile(error: unknown): boolean {
	return (error as { code?: string }).code === 'ENOENT';
}
