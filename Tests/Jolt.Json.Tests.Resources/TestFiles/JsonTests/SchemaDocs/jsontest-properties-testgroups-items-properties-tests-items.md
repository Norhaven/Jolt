# Untitled object in undefined Schema

```txt
undefined#/properties/testGroups/items/properties/tests/items
```

An individual test.

| Abstract            | Extensible | Status         | Identifiable | Custom Properties | Additional Properties | Access Restrictions | Defined In                                                                   |
| :------------------ | :--------- | :------------- | :----------- | :---------------- | :-------------------- | :------------------ | :--------------------------------------------------------------------------- |
| Can be instantiated | No         | Unknown status | No           | Forbidden         | Allowed               | none                | [JsonTest.schema.json\*](../out/JsonTest.schema.json "open original schema") |

## items Type

`object` ([Details](jsontest-properties-testgroups-items-properties-tests-items.md))

one (and only one) of

* [Untitled schema](jsontest-properties-testgroups-items-properties-tests-items-oneof-0.md "check type definition")

* [Untitled schema](jsontest-properties-testgroups-items-properties-tests-items-oneof-1.md "check type definition")

# items Properties

| Property                                  | Type     | Required | Nullable       | Defined by                                                                                                                                                                                                    |
| :---------------------------------------- | :------- | :------- | :------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| [name](#name)                             | `string` | Required | cannot be null | [Untitled schema](jsontest-properties-testgroups-items-properties-tests-items-properties-name.md "undefined#/properties/testGroups/items/properties/tests/items/properties/name")                             |
| [transformer](#transformer)               | `object` | Required | cannot be null | [Untitled schema](jsontest-properties-testgroups-items-properties-tests-items-properties-transformer.md "undefined#/properties/testGroups/items/properties/tests/items/properties/transformer")               |
| [result](#result)                         | `object` | Optional | cannot be null | [Untitled schema](jsontest-properties-testgroups-items-properties-tests-items-properties-result.md "undefined#/properties/testGroups/items/properties/tests/items/properties/result")                         |
| [exceptionCode](#exceptioncode)           | `string` | Optional | cannot be null | [Untitled schema](jsontest-properties-testgroups-items-properties-tests-items-properties-exceptioncode.md "undefined#/properties/testGroups/items/properties/tests/items/properties/exceptionCode")           |
| [innerExceptionCode](#innerexceptioncode) | `string` | Optional | cannot be null | [Untitled schema](jsontest-properties-testgroups-items-properties-tests-items-properties-innerexceptioncode.md "undefined#/properties/testGroups/items/properties/tests/items/properties/innerExceptionCode") |

## name

The name of the test. It should be descriptive of the functionlity that it targets (e.g. The >= operator should return true when the number on the left is larger)

`name`

* is required

* Type: `string`

* cannot be null

* defined in: [Untitled schema](jsontest-properties-testgroups-items-properties-tests-items-properties-name.md "undefined#/properties/testGroups/items/properties/tests/items/properties/name")

### name Type

`string`

## transformer

The JSON object that acts as the transformer for this test group's incoming source document.

`transformer`

* is required

* Type: `object` ([Details](jsontest-properties-testgroups-items-properties-tests-items-properties-transformer.md))

* cannot be null

* defined in: [Untitled schema](jsontest-properties-testgroups-items-properties-tests-items-properties-transformer.md "undefined#/properties/testGroups/items/properties/tests/items/properties/transformer")

### transformer Type

`object` ([Details](jsontest-properties-testgroups-items-properties-tests-items-properties-transformer.md))

## result

The JSON object that acts as the expected result of this test.

`result`

* is optional

* Type: `object` ([Details](jsontest-properties-testgroups-items-properties-tests-items-properties-result.md))

* cannot be null

* defined in: [Untitled schema](jsontest-properties-testgroups-items-properties-tests-items-properties-result.md "undefined#/properties/testGroups/items/properties/tests/items/properties/result")

### result Type

`object` ([Details](jsontest-properties-testgroups-items-properties-tests-items-properties-result.md))

## exceptionCode

The alias name of an exception code that this test expects to receive as a result of this test.

`exceptionCode`

* is optional

* Type: `string`

* cannot be null

* defined in: [Untitled schema](jsontest-properties-testgroups-items-properties-tests-items-properties-exceptioncode.md "undefined#/properties/testGroups/items/properties/tests/items/properties/exceptionCode")

### exceptionCode Type

`string`

## innerExceptionCode

The alias name of an exception code on the expected exception's inner exception that this test expects to receive as a result of this test.

`innerExceptionCode`

* is optional

* Type: `string`

* cannot be null

* defined in: [Untitled schema](jsontest-properties-testgroups-items-properties-tests-items-properties-innerexceptioncode.md "undefined#/properties/testGroups/items/properties/tests/items/properties/innerExceptionCode")

### innerExceptionCode Type

`string`
