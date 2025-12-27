# Jolt Test Group Schema

```txt
https://norhaven.net/schemas/jolt/jsontest.schema.json#/properties/testGroups/items
```

An individual test group object

| Abstract            | Extensible | Status         | Identifiable | Custom Properties | Additional Properties | Access Restrictions | Defined In                                                                   |
| :------------------ | :--------- | :------------- | :----------- | :---------------- | :-------------------- | :------------------ | :--------------------------------------------------------------------------- |
| Can be instantiated | No         | Unknown status | No           | Forbidden         | Allowed               | none                | [JsonTest.schema.json\*](../out/JsonTest.schema.json "open original schema") |

## items Type

`object` ([Jolt Test Group](jsontest-properties-jolt-test-groups-jolt-test-group.md))

# items Properties

| Property                                      | Type     | Required | Nullable       | Defined by                                                                                                                                                                                                                          |
| :-------------------------------------------- | :------- | :------- | :------------- | :---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| [name](#name)                                 | `string` | Required | cannot be null | [JSON Test Schema](jsontest-properties-jolt-test-groups-jolt-test-group-properties-test-group-name.md "https://norhaven.net/schemas/jolt/jsontest.schema.json#/properties/testGroups/items/properties/name")                        |
| [source](#source)                             | `object` | Required | cannot be null | [JSON Test Schema](jsontest-properties-jolt-test-groups-jolt-test-group-properties-source-document.md "https://norhaven.net/schemas/jolt/jsontest.schema.json#/properties/testGroups/items/properties/source")                      |
| [externalMethodSource](#externalmethodsource) | `string` | Optional | cannot be null | [JSON Test Schema](jsontest-properties-jolt-test-groups-jolt-test-group-properties-external-method-source.md "https://norhaven.net/schemas/jolt/jsontest.schema.json#/properties/testGroups/items/properties/externalMethodSource") |
| [tests](#tests)                               | `array`  | Required | cannot be null | [JSON Test Schema](jsontest-properties-jolt-test-groups-jolt-test-group-properties-jolt-tests.md "https://norhaven.net/schemas/jolt/jsontest.schema.json#/properties/testGroups/items/properties/tests")                            |

## name

The name of the test group. It should be descriptive of the functionality that it targets (e.g.  math operators)

`name`

* is required

* Type: `string` ([Test Group Name](jsontest-properties-jolt-test-groups-jolt-test-group-properties-test-group-name.md))

* cannot be null

* defined in: [JSON Test Schema](jsontest-properties-jolt-test-groups-jolt-test-group-properties-test-group-name.md "https://norhaven.net/schemas/jolt/jsontest.schema.json#/properties/testGroups/items/properties/name")

### name Type

`string` ([Test Group Name](jsontest-properties-jolt-test-groups-jolt-test-group-properties-test-group-name.md))

## source

The JSON object that acts as the incoming source document that will be transformed in all of the tests in this group.

`source`

* is required

* Type: `object` ([Source Document](jsontest-properties-jolt-test-groups-jolt-test-group-properties-source-document.md))

* cannot be null

* defined in: [JSON Test Schema](jsontest-properties-jolt-test-groups-jolt-test-group-properties-source-document.md "https://norhaven.net/schemas/jolt/jsontest.schema.json#/properties/testGroups/items/properties/source")

### source Type

`object` ([Source Document](jsontest-properties-jolt-test-groups-jolt-test-group-properties-source-document.md))

## externalMethodSource

The alias name of an external method that may be used within a transformer in all of the tests in this group.

`externalMethodSource`

* is optional

* Type: `string` ([External Method Source](jsontest-properties-jolt-test-groups-jolt-test-group-properties-external-method-source.md))

* cannot be null

* defined in: [JSON Test Schema](jsontest-properties-jolt-test-groups-jolt-test-group-properties-external-method-source.md "https://norhaven.net/schemas/jolt/jsontest.schema.json#/properties/testGroups/items/properties/externalMethodSource")

### externalMethodSource Type

`string` ([External Method Source](jsontest-properties-jolt-test-groups-jolt-test-group-properties-external-method-source.md))

## tests

An array of tests that are contained in this group.

`tests`

* is required

* Type: `object[]` ([Jolt Test](jsontest-properties-jolt-test-groups-jolt-test-group-properties-jolt-tests-jolt-test.md))

* cannot be null

* defined in: [JSON Test Schema](jsontest-properties-jolt-test-groups-jolt-test-group-properties-jolt-tests.md "https://norhaven.net/schemas/jolt/jsontest.schema.json#/properties/testGroups/items/properties/tests")

### tests Type

`object[]` ([Jolt Test](jsontest-properties-jolt-test-groups-jolt-test-group-properties-jolt-tests-jolt-test.md))
