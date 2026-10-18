// Tokenizes Jolt expression text with the rules from a TextMate grammar (src/grammars/jolt-expr.tmLanguage.json).
// Kept free of editor dependencies so it can be tested on its own.

/**
 * @param {{ repository: Record<string, { match?: string, patterns?: { include: string }[] }> }} grammar
 * @returns {(text: string) => { from: number, to: number, rule: string }[]}
 */
export function createTokenizer(grammar) {
	// The grammar's top-level rules, in order, compiled as sticky regexes so each is tried at an exact position.
	const rules = grammar.repository.expression.patterns
		.map(({ include }) => include.replace(/^#/, ''))
		.map((name) => ({ name, regex: new RegExp(grammar.repository[name].match, 'y') }));

	return function tokenize(text) {
		const tokens = [];
		let position = 0;

		while (position < text.length) {
			let matched = false;

			for (const rule of rules) {
				rule.regex.lastIndex = position;
				const match = rule.regex.exec(text);

				if (match && match[0].length > 0) {
					tokens.push({ from: position, to: position + match[0].length, rule: rule.name });
					position += match[0].length;
					matched = true;
					break;
				}
			}

			// Whitespace, brackets and anything else the grammar doesn't name.
			if (!matched) position++;
		}

		return tokens;
	};
}

// What the engine treats as an expression (JoltTransformer: method call, parenthesis, variable, array or object
// literal, logical NOT), plus paths and string literals, which appear as expressions inside match cases.
export const EXPRESSION_START = /^\s*[#@$!(\[{']/;
