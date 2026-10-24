// @ts-check
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import starlight from '@astrojs/starlight';
import { defineConfig } from 'astro/config';
import { getSiteModes } from './src/lib/mod-data.ts';
import { getMods } from './src/lib/mods.ts';

const appRoot = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(appRoot, '../..');

const mods = await getMods();
const { modes, defaultMode } = await getSiteModes(mods);

const site = process.env.SITE_URL ?? 'http://localhost:4321';
const base = process.env.BASE_PATH ?? '/';

const basePath = base === '/' ? '' : `/${base.replace(/^\/+|\/+$/g, '')}`;
const withBase = (/** @type {string} */ target) =>
	`${basePath}${target.startsWith('/') ? target : `/${target}`}`;

// Runs in <head> so the stored mode is on <html> before the page paints.
const modeInitScript = `(() => {
	const modes = ${JSON.stringify(modes)};
	const fallback = ${JSON.stringify(defaultMode)};
	let stored = null;
	try { stored = localStorage.getItem('stardew-docs-mode'); } catch {}
	const mode = modes.find((value) => value.toLowerCase() === String(stored).toLowerCase()) ?? fallback;
	const root = document.documentElement;
	root.dataset.modes = JSON.stringify(modes);
	root.dataset.defaultMode = fallback;
	root.dataset.mode = mode.toLowerCase();
})();`;

// Visibility is pure CSS so switching modes never waits for JS to replace text.
const modeStyle =
	modes.length > 1
		? modes
				.map((mode) => {
					const key = mode.toLowerCase();
					return `html[data-mode="${key}"] [data-mode-text="${key}"] { display: inline; }\nhtml[data-mode="${key}"] [data-mode-block="${key}"] { display: block; }`;
				})
				.join('\n')
		: '';

// https://astro.build/config
export default defineConfig({
	site,
	base,
	integrations: [
		starlight({
			title: 'Stardew Mods',
			description: 'Documentation and data reference for these Stardew Valley mods.',
			customCss: ['./src/styles/custom.css'],
			lastUpdated: true,
			// The override renders the mode switch next to the theme picker in the
			// header and the mobile menu.
			components: {
				ThemeSelect: './src/components/ThemeWithModeSelect.astro',
			},
			// We ship our own 404 page so Starlight's route and the docs catch-all
			// don't both try to generate /404.
			disable404Route: true,
			head:
				modes.length > 1
					? [
							{ tag: 'script', content: modeInitScript },
							{ tag: 'style', content: modeStyle },
							{
								tag: 'script',
								attrs: { src: withBase('/mode-switcher.js'), defer: true },
							},
						]
					: [
							{
								tag: 'script',
								attrs: { src: withBase('/mode-switcher.js'), defer: true },
							},
						],
			sidebar: [
				{
					label: 'Mods',
					items: [
						{ label: 'All mods', link: '/mods/' },
						...mods.map((mod) => ({
							label: mod.manifest.Name,
							items: mod.pages.map((page) => ({
								label: page.slug === '' ? 'Overview' : page.title,
								link:
									page.slug === ''
										? `/mods/${mod.slug}/`
										: `/mods/${mod.slug}/${page.slug}/`,
							})),
						})),
					],
				},
				{
					label: 'Guides',
					items: [{ autogenerate: { directory: 'guides' } }],
				},
			],
		}),
	],
	vite: {
		server: {
			fs: {
				// Mod assets live outside this app, one level up in the repo.
				allow: [appRoot, repoRoot],
			},
		},
	},
});
