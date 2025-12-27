# Untitled object in JSON Test Schema Schema

```txt
undefined#/properties/testGroups/items
```

An individual test group object

| Abstract            | Extensible | Status         | Identifiable | Custom Properties | Additional Properties | Access Restrictions | Defined In                                                                   |
| :------------------ | :--------- | :------------- | :----------- | :---------------- | :-------------------- | :------------------ | :--------------------------------------------------------------------------- |
| Can be instantiated | No         | Unknown status | No           | Forbidden         | Allowed               | none                | [JsonTest.schema.json\*](../out/JsonTest.schema.json "open original schema") |

## items Type

`object` ([Details](jsontest-properties-testgroups-items.md))

# items Properties

| Property                                      | Type     | Required | Nullable       | Defined by                                                                                                                                                           |
| :-------------------------------------------- | :------- | :------- | :------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| [name](#name)                                 | `string` | Required | cannot be null | [JSON Test Schema](jsontest-properties-testgroups-items-properties-name.md "undefined#/properties/testGroups/items/properties/name")                                 |
| [source](#source)                             | `object` | Required | cannot be null | [JSON Test Schema](jsontest-properties-testgroups-items-properties-source.md "undefined#/properties/testGroups/items/properties/source")                             |
| [externalMethodSource](#externalmethodsource) | `string` | Optional | cannot be null | [JSON Test Schema](jsontest-properties-testgroups-items-properties-externalmethodsource.md "undefined#/properties/testGroups/items/properties/externalMethodSource") |
| [tests](#tests)                               | `array`  | Required | cannot be null | [JSON Test Schema](jsontest-properties-testgroups-items-properties-tests.md "undefined#/properties/testGroups/items/properties/tests")                               |

## name

The name of the test group. It should be descriptive of the functionality that it targets (e.g.  math operators)

`name`

* is required

* Type: `string`

* cannot be null

* defined in: [JSON Test Schema](jsontest-properties-testgroups-items-properties-name.md "undefined#/properties/testGroups/items/properties/name")

### name Type

`string`

## source

The JSON object that acts as the incoming source document that will be transformed in all of the tests in this group.

`source`

* is required

* Type: `object` ([Details](jsontest-properties-testgroups-items-properties-source.md))

* cannot be null

* defined in: [JSON Test Schema](jsontest-properties-testgroups-items-properties-source.md "undefined#/properties/testGroups/items/properties/source")

### source Type

`object` ([Details](jsontest-properties-testgroups-items-properties-source.md))

## externalMethodSource

The alias name of an external method that may be used within a transformer in all of the tests in this group.

`externalMethodSource`

* is optional

* Type: `string`

* cannot be null

* defined in: [JSON Test Schema](jsontest-properties-testgroups-items-properties-externalmethodsource.md "undefined#/properties/testGroups/items/properties/externalMethodSource")

### externalMethodSource Type

`string`

## tests

An array of tests that are contained in this group.

`tests`

* is required

* Type: `object[]` ([Details](jsontest-properties-testgroups-items-properties-tests-items.md))

* cannot be null

* defined in: [JSON Test Schema](jsontest-properties-testgroups-items-properties-tests.md "undefined#/properties/testGroups/items/properties/tests")

### tests Type

`object[]` ([Details](jsontest-properties-testgroups-items-properties-tests-items.md))
