import { readdir } from 'node:fs/promises';
import path from 'node:path';

const SKIPPED_DIRECTORIES = new Set(['bin', 'obj', 'node_modules']);

/** Recursively list files under `directory` that match `predicate`. Returns full paths. */
export async function listFiles(
	directory: string,
	predicate: (fileName: string) => boolean,
): Promise<string[]> {
	let entries;

	try {
		entries = await readdir(directory, { withFileTypes: true });
	} catch (error) {
		if ((error as { code?: string }).code === 'ENOENT') return [];
		throw error;
	}

	const files: string[] = [];

	for (const entry of entries) {
		if (entry.name.startsWith('.')) continue;

		if (entry.isDirectory()) {
			if (SKIPPED_DIRECTORIES.has(entry.name)) continue;
			files.push(...(await listFiles(path.join(directory, entry.name), predicate)));
		} else if (entry.isFile() && predicate(entry.name)) {
			files.push(path.join(directory, entry.name));
		}
	}

	return files.sort();
}
