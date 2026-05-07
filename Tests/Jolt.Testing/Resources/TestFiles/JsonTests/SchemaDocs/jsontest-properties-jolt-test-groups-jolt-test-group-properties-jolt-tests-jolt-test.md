# Jolt Test Schema

```txt
https://github.com/norhaven/schemas/jolt/jsontest.schema.json#/properties/testGroups/items/properties/tests/items
```

An individual test.

| Abstract            | Extensible | Status         | Identifiable | Custom Properties | Additional Properties | Access Restrictions | Defined In                                                                   |
| :------------------ | :--------- | :------------- | :----------- | :---------------- | :-------------------- | :------------------ | :--------------------------------------------------------------------------- |
| Can be instantiated | No         | Unknown status | No           | Forbidden         | Allowed               | none                | [JsonTest.schema.json\*](../out/JsonTest.schema.json "open original schema") |

## items Type

`object` ([Jolt Test](jsontest-properties-jolt-test-groups-jolt-test-group-properties-jolt-tests-jolt-test.md))

one (and only one) of

* [Untitled undefined type in JSON Test Schema](jsontest-properties-jolt-test-groups-jolt-test-group-properties-jolt-tests-jolt-test-oneof-0.md "check type definition")

* [Untitled undefined type in JSON Test Schema](jsontest-properties-jolt-test-groups-jolt-test-group-properties-jolt-tests-jolt-test-oneof-1.md "check type definition")

# items Properties

| Property                                  | Type     | Required | Nullable       | Defined by                                                                                                                                                                                                                                                                                    |
| :---------------------------------------- | :------- | :------- | :------------- | :-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| [name](#name)                             | `string` | Required | cannot be null | [JSON Test Schema](jsontest-properties-jolt-test-groups-jolt-test-group-properties-jolt-tests-jolt-test-properties-name.md "https://github.com/norhaven/schemas/jolt/jsontest.schema.json#/properties/testGroups/items/properties/tests/items/properties/name")                               |
| [transformer](#transformer)               | `object` | Required | cannot be null | [JSON Test Schema](jsontest-properties-jolt-test-groups-jolt-test-group-properties-jolt-tests-jolt-test-properties-transformer.md "https://github.com/norhaven/schemas/jolt/jsontest.schema.json#/properties/testGroups/items/properties/tests/items/properties/transformer")                 |
| [result](#result)                         | `object` | Optional | cannot be null | [JSON Test Schema](jsontest-properties-jolt-test-groups-jolt-test-group-properties-jolt-tests-jolt-test-properties-result.md "https://github.com/norhaven/schemas/jolt/jsontest.schema.json#/properties/testGroups/items/properties/tests/items/properties/result")                           |
| [exceptionCode](#exceptioncode)           | `string` | Optional | cannot be null | [JSON Test Schema](jsontest-properties-jolt-test-groups-jolt-test-group-properties-jolt-tests-jolt-test-properties-exception-code.md "https://github.com/norhaven/schemas/jolt/jsontest.schema.json#/properties/testGroups/items/properties/tests/items/properties/exceptionCode")            |
| [innerExceptionCode](#innerexceptioncode) | `string` | Optional | cannot be null | [JSON Test Schema](jsontest-properties-jolt-test-groups-jolt-test-group-properties-jolt-tests-jolt-test-properties-inner-exception-code.md "https://github.com/norhaven/schemas/jolt/jsontest.schema.json#/properties/testGroups/items/properties/tests/items/properties/innerExceptionCode") |

## name

The name of the test. It should be descriptive of the functionlity that it targets (e.g. The >= operator should return true when the number on the left is larger)

`name`

* is required

* Type: `string` ([Name](jsontest-properties-jolt-test-groups-jolt-test-group-properties-jolt-tests-jolt-test-properties-name.md))

* cannot be null

* defined in: [JSON Test Schema](jsontest-properties-jolt-test-groups-jolt-test-group-properties-jolt-tests-jolt-test-properties-name.md "https://github.com/norhaven/schemas/jolt/jsontest.schema.json#/properties/testGroups/items/properties/tests/items/properties/name")

### name Type

`string` ([Name](jsontest-properties-jolt-test-groups-jolt-test-group-properties-jolt-tests-jolt-test-properties-name.md))

## transformer

The JSON object that acts as the transformer for this test group's incoming source document.

`transformer`

* is required

* Type: `object` ([Transformer](jsontest-properties-jolt-test-groups-jolt-test-group-properties-jolt-tests-jolt-test-properties-transformer.md))

* cannot be null

* defined in: [JSON Test Schema](jsontest-properties-jolt-test-groups-jolt-test-group-properties-jolt-tests-jolt-test-properties-transformer.md "https://github.com/norhaven/schemas/jolt/jsontest.schema.json#/properties/testGroups/items/properties/tests/items/properties/transformer")

### transformer Type

`object` ([Transformer](jsontest-properties-jolt-test-groups-jolt-test-group-properties-jolt-tests-jolt-test-properties-transformer.md))

## result

The JSON object that acts as the expected result of this test.

`result`

* is optional

* Type: `object` ([Result](jsontest-properties-jolt-test-groups-jolt-test-group-properties-jolt-tests-jolt-test-properties-result.md))

* cannot be null

* defined in: [JSON Test Schema](jsontest-properties-jolt-test-groups-jolt-test-group-properties-jolt-tests-jolt-test-properties-result.md "https://github.com/norhaven/schemas/jolt/jsontest.schema.json#/properties/testGroups/items/properties/tests/items/properties/result")

### result Type

`object` ([Result](jsontest-properties-jolt-test-groups-jolt-test-group-properties-jolt-tests-jolt-test-properties-result.md))

## exceptionCode

The alias name of an exception code that this test expects to receive as a result of this test.

`exceptionCode`

* is optional

* Type: `string` ([Exception Code](jsontest-properties-jolt-test-groups-jolt-test-group-properties-jolt-tests-jolt-test-properties-exception-code.md))

* cannot be null

* defined in: [JSON Test Schema](jsontest-properties-jolt-test-groups-jolt-test-group-properties-jolt-tests-jolt-test-properties-exception-code.md "https://github.com/norhaven/schemas/jolt/jsontest.schema.json#/properties/testGroups/items/properties/tests/items/properties/exceptionCode")

### exceptionCode Type

`string` ([Exception Code](jsontest-properties-jolt-test-groups-jolt-test-group-properties-jolt-tests-jolt-test-properties-exception-code.md))

## innerExceptionCode

The alias name of an exception code on the expected exception's inner exception that this test expects to receive as a result of this test.

`innerExceptionCode`

* is optional

* Type: `string` ([Inner Exception Code](jsontest-properties-jolt-test-groups-jolt-test-group-properties-jolt-tests-jolt-test-properties-inner-exception-code.md))

* cannot be null

* defined in: [JSON Test Schema](jsontest-properties-jolt-test-groups-jolt-test-group-properties-jolt-tests-jolt-test-properties-inner-exception-code.md "https://github.com/norhaven/schemas/jolt/jsontest.schema.json#/properties/testGroups/items/properties/tests/items/properties/innerExceptionCode")

### innerExceptionCode Type

`string` ([Inner Exception Code](jsontest-properties-jolt-test-groups-jolt-test-group-properties-jolt-tests-jolt-test-properties-inner-exception-code.md))
