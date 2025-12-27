# README

## Top-level Schemas

* [JSON Test Schema](./jsontest.md "A schema that defines the structure for a given Jolt JSON Test file") – `https://norhaven.net/schemas/jolt/jsontest.schema.json`

## Other Schemas

### Objects

* [Jolt Test](./jsontest-properties-jolt-test-groups-jolt-test-group-properties-jolt-tests-jolt-test.md "An individual test") – `https://norhaven.net/schemas/jolt/jsontest.schema.json#/properties/testGroups/items/properties/tests/items`

* [Jolt Test Group](./jsontest-properties-jolt-test-groups-jolt-test-group.md "An individual test group object") – `https://norhaven.net/schemas/jolt/jsontest.schema.json#/properties/testGroups/items`

* [Possible Exception Codes](./jsontest-properties-possible-exception-codes.md "An alias list of exception code names as keys and their ") – `https://norhaven.net/schemas/jolt/jsontest.schema.json#/properties/possibleExceptionCodes`

* [Possible External Method Sources](./jsontest-properties-possible-external-method-sources.md "An alias list of external method names as keys and their fully qualified assembly names as values") – `https://norhaven.net/schemas/jolt/jsontest.schema.json#/properties/possibleExternalMethodSources`

* [Result](./jsontest-properties-jolt-test-groups-jolt-test-group-properties-jolt-tests-jolt-test-properties-result.md "The JSON object that acts as the expected result of this test") – `https://norhaven.net/schemas/jolt/jsontest.schema.json#/properties/testGroups/items/properties/tests/items/properties/result`

* [Source Document](./jsontest-properties-jolt-test-groups-jolt-test-group-properties-source-document.md "The JSON object that acts as the incoming source document that will be transformed in all of the tests in this group") – `https://norhaven.net/schemas/jolt/jsontest.schema.json#/properties/testGroups/items/properties/source`

* [Transformer](./jsontest-properties-jolt-test-groups-jolt-test-group-properties-jolt-tests-jolt-test-properties-transformer.md "The JSON object that acts as the transformer for this test group's incoming source document") – `https://norhaven.net/schemas/jolt/jsontest.schema.json#/properties/testGroups/items/properties/tests/items/properties/transformer`

### Arrays

* [Jolt Test Groups](./jsontest-properties-jolt-test-groups.md "An array of test groups, each group containing a specific area that they cover (e") – `https://norhaven.net/schemas/jolt/jsontest.schema.json#/properties/testGroups`

* [Jolt Tests](./jsontest-properties-jolt-test-groups-jolt-test-group-properties-jolt-tests.md "An array of tests that are contained in this group") – `https://norhaven.net/schemas/jolt/jsontest.schema.json#/properties/testGroups/items/properties/tests`

## Version Note

The schemas linked above follow the JSON Schema Spec version: `https://json-schema.org/draft/2020-12/schema`
