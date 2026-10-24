(() => {
	const root = document.documentElement;
	const MODE_KEY = 'stardew-docs-mode';
	const normalize = (value) => String(value).toLowerCase();

	const buttons = () => [...document.querySelectorAll('[data-mode-option]')];
	const options = buttons()
		.map((button) => button.dataset.modeOption)
		.filter(Boolean);

	if (options.length === 0) return;

	const canonical = (value) => options.find((option) => normalize(option) === normalize(value));

	const read = () => {
		try {
			return localStorage.getItem(MODE_KEY);
		} catch {
			return null;
		}
	};

	let current = canonical(read()) ?? canonical(root.dataset.defaultMode) ?? options[0];

	const supported = () =>
		new Set(
			[...document.querySelectorAll('[data-mode-text], [data-mode-block]')].map((element) =>
				normalize(
					element.getAttribute('data-mode-text') ||
						element.getAttribute('data-mode-block') ||
						'',
				),
			),
		);

	const pageDefault = () =>
		normalize(
			document.querySelector('[data-page-mode-default]')?.getAttribute('data-page-mode-default') ||
				'',
		);

	/** Prefer the saved mode; fall back to the page's default when the mod lacks it. */
	function apply() {
		if (!current) return;

		const wanted = normalize(current);
		const available = supported();
		const fallback = pageDefault();

		const effective =
			available.size === 0 || available.has(wanted)
				? wanted
				: available.has(fallback)
					? fallback
					: [...available][0];

		root.dataset.mode = effective;

		for (const button of buttons()) {
			button.setAttribute(
				'aria-pressed',
				String(normalize(button.dataset.modeOption) === wanted),
			);
		}
	}

	document.addEventListener('click', (event) => {
		const target = event.target;
		const button = target instanceof Element ? target.closest('[data-mode-option]') : null;
		const value = button instanceof HTMLElement ? button.dataset.modeOption : undefined;

		if (!value) return;

		current = value;

		try {
			localStorage.setItem(MODE_KEY, value);
		} catch {
			// Storage can be unavailable; the choice still applies to this page.
		}

		apply();
	});

	window.addEventListener('storage', (event) => {
		if (event.key !== MODE_KEY || event.newValue === null) return;

		const next = canonical(event.newValue);

		if (next) {
			current = next;
			apply();
		}
	});

	if (document.readyState === 'loading') {
		document.addEventListener('DOMContentLoaded', apply, { once: true });
	} else {
		apply();
	}
})();
