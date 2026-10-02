import { strict as assert } from 'node:assert';
import { test } from 'node:test';
import { getMethodReferenceAt } from '../language/hover';

const line = `"result": "#valueOf($.a)->#toUpperCase()"`;

test('finds the method under the cursor', () => {
	assert.deepEqual(getMethodReferenceAt(line, line.indexOf('valueOf') + 2), { name: 'valueOf', start: 11, end: 19 });
});

test('finds a piped method', () => {
	assert.equal(getMethodReferenceAt(line, line.indexOf('toUpperCase'))?.name, 'toUpperCase');
});

test('the hash and the end of the name are part of the reference', () => {
	assert.equal(getMethodReferenceAt(line, line.indexOf('#valueOf'))?.name, 'valueOf');
	assert.equal(getMethodReferenceAt(line, line.indexOf('('))?.name, 'valueOf');
});

test('no reference outside of a method name', () => {
	assert.equal(getMethodReferenceAt(line, line.indexOf('$.a') + 1), undefined);
	assert.equal(getMethodReferenceAt(line, 2), undefined);
});
