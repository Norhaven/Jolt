export interface CallContext {
	/** The name of the method being called, without the leading "#". */
	name: string;
	/** Whether the method is being called with the pipe operator, which supplies its first parameter. */
	isPiped: boolean;
	/** The index of the argument containing the cursor, counting from the first argument written in the call. */
	argumentIndex: number;
}

interface Frame {
	/** The method that the bracket opens the arguments of, or undefined for any other bracket. */
	call?: { name: string; isPiped: boolean };
	commas: number;
}

const methodBeforeParenthesis = /(->\s*)?#([A-Za-z_]\w*)\s*$/;

/**
 * Finds the innermost method call whose arguments contain the end of the expression, or undefined when the end is
 * not within a call's arguments. Commas only separate arguments at the call's own nesting level, so those within
 * nested calls, array and object literals, and string literals are ignored.
 */
export function getCallContext(expression: string): CallContext | undefined {
	const frames: Frame[] = [];
	let isInLiteral = false;

	for (let i = 0; i < expression.length; i++) {
		const current = expression[i];

		if (isInLiteral) {
			if (current === '\\') {
				i++;
			} else if (current === '\'') {
				isInLiteral = false;
			}

			continue;
		}

		switch (current) {
			case '\'':
				isInLiteral = true;
				break;
			case '(': {
				const method = methodBeforeParenthesis.exec(expression.slice(0, i));

				frames.push({ call: method ? { name: method[2], isPiped: method[1] !== undefined } : undefined, commas: 0 });
				break;
			}
			case '[':
			case '{':
				frames.push({ commas: 0 });
				break;
			case ')':
			case ']':
			case '}':
				frames.pop();
				break;
			case ',':
				if (frames.length > 0) {
					frames[frames.length - 1].commas++;
				}
				break;
		}
	}

	for (let i = frames.length - 1; i >= 0; i--) {
		const call = frames[i].call;

		if (call) {
			return { ...call, argumentIndex: frames[i].commas };
		}
	}

	return undefined;
}
