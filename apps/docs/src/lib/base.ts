/** Prefix a root-relative path with the configured Astro base path. */
export function withBase(path: string): string {
	const base = import.meta.env.BASE_URL.replace(/\/+$/, '');
	return `${base}${path.startsWith('/') ? path : `/${path}`}`;
}
