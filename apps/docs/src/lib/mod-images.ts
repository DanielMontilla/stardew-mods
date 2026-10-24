import path from 'node:path';
import type { ImageMetadata } from 'astro';
import { MODS_ROOT } from './mods';

const REPO_ROOT = path.dirname(MODS_ROOT);

const modules = import.meta.glob('../../../../mods/*/assets/**/*.{png,jpg,jpeg,webp,gif}', {
	eager: true,
	import: 'default',
}) as Record<string, ImageMetadata>;

// Markdown images can come from `assets/` or anywhere under `docs/`.
const documentImages = import.meta.glob(
	[
		'../../../../mods/*/assets/**/*.{png,jpg,jpeg,webp,gif,svg}',
		'../../../../mods/*/docs/**/*.{png,jpg,jpeg,webp,gif,svg}',
	],
	{ eager: true, query: '?url', import: 'default' },
) as Record<string, string>;

export interface ModImage {
	/** Path relative to the mod folder, using forward slashes. */
	path: string;
	src: ImageMetadata;
}

const imagesByFolder = new Map<string, ModImage[]>();
const urlsByPath = new Map<string, string>();

for (const [key, src] of Object.entries(modules)) {
	const match = /mods\/([^/]+)\/assets\/(.+)$/.exec(key);

	if (!match) continue;

	const [, folder, relativePath] = match;
	const images = imagesByFolder.get(folder) ?? [];

	images.push({ path: relativePath, src });
	imagesByFolder.set(folder, images);
}

for (const [key, url] of Object.entries(documentImages)) {
	const index = key.indexOf('mods/');

	if (index >= 0) urlsByPath.set(key.slice(index), url);
}

/** Images bundled straight out of `mods/<folder>/assets/` at build time. */
export function getModImages(folder: string): ModImage[] {
	return [...(imagesByFolder.get(folder) ?? [])].sort((a, b) => a.path.localeCompare(b.path));
}

/**
 * Resolve a Markdown image path relative to the file that references it.
 * Used for `![alt](../images/route.png)` in `mods/<Mod>/docs/**`.
 */
export function resolveModImage(file: string, source: string): string {
	const cleanSource = source.split(/[?#]/)[0];
	const absolute = path.resolve(path.dirname(file), cleanSource);
	const repoRelative = path.relative(REPO_ROOT, absolute).replace(/\\/g, '/');
	const url = urlsByPath.get(repoRelative);

	if (url === undefined) {
		throw new Error(`Image "${source}" wasn't found in the mod (looked for "${repoRelative}").`);
	}

	return url;
}
