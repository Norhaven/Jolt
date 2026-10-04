import manifest from '../data/library-methods.json';

/** A position within a transformer where a library method may be used (mirrors LibraryMethodTarget in Jolt). */
export type MethodTarget = 'propertyName' | 'propertyValue' | 'statementBlock' | 'matchBlock';

/** How a parameter is written in a transformer, derived from the parameter's type in the Jolt library. */
export type ParameterKind =
	| 'value'
	| 'string'
	| 'object'
	| 'expression'
	| 'lambda'
	| 'range'
	| 'rangeVariable'
	| 'variablePath'
	| 'enumeration'
	| 'variableAlias';

export interface LibraryParameter {
	name: string;
	kind: ParameterKind;
	isOptional: boolean;
	isVariadic: boolean;
	isLazy: boolean;
}

export interface LibraryMethod {
	name: string;
	description?: string;
	example?: string;
	validOn: MethodTarget[];
	isValueGenerator: boolean;
	isUnsafe: boolean;
	parameters: LibraryParameter[];
	/** Where a custom method was defined (e.g. a .jolt/methods.json path), or undefined for the standard library. */
	source?: string;
}

/**
 * The Jolt standard library, generated from the Jolt assembly by IDE/Tools/Jolt.LanguageMetadata.
 */
export const libraryMethods: readonly LibraryMethod[] = manifest.methods as LibraryMethod[];
