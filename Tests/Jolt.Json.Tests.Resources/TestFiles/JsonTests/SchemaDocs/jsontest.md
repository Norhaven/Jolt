# JSON Test Schema Schema

```txt
undefined
```

A schema that defines the structure for a given Jolt JSON Test file.

| Abstract            | Extensible | Status         | Identifiable | Custom Properties | Additional Properties | Access Restrictions | Defined In                                                                 |
| :------------------ | :--------- | :------------- | :----------- | :---------------- | :-------------------- | :------------------ | :------------------------------------------------------------------------- |
| Can be instantiated | No         | Unknown status | No           | Forbidden         | Allowed               | none                | [JsonTest.schema.json](../out/JsonTest.schema.json "open original schema") |

## JSON Test Schema Type

`object` ([JSON Test Schema](jsontest.md))

# JSON Test Schema Properties

| Property                                                        | Type     | Required | Nullable       | Defined by                                                                                                                        |
| :-------------------------------------------------------------- | :------- | :------- | :------------- | :-------------------------------------------------------------------------------------------------------------------------------- |
| [possibleExceptionCodes](#possibleexceptioncodes)               | `object` | Optional | cannot be null | [JSON Test Schema](jsontest-properties-possible-exception-codes.md "undefined#/properties/possibleExceptionCodes")                |
| [possibleExternalMethodSources](#possibleexternalmethodsources) | `object` | Optional | cannot be null | [JSON Test Schema](jsontest-properties-possible-external-method-sources.md "undefined#/properties/possibleExternalMethodSources") |
| [testGroups](#testgroups)                                       | `array`  | Required | cannot be null | [JSON Test Schema](jsontest-properties-jolt-test-groups.md "undefined#/properties/testGroups")                                    |

## possibleExceptionCodes

An alias list of exception code names as keys and their .Net enum names as values. Tests can list an exception code alias as their expectation.

`possibleExceptionCodes`

* is optional

* Type: `object` ([Possible Exception Codes](jsontest-properties-possible-exception-codes.md))

* cannot be null

* defined in: [JSON Test Schema](jsontest-properties-possible-exception-codes.md "undefined#/properties/possibleExceptionCodes")

### possibleExceptionCodes Type

`object` ([Possible Exception Codes](jsontest-properties-possible-exception-codes.md))

## possibleExternalMethodSources

An alias list of external method names as keys and their fully qualified assembly names as values. Tests may be register these for use in their 'transformer' property.

`possibleExternalMethodSources`

* is optional

* Type: `object` ([Possible External Method Sources](jsontest-properties-possible-external-method-sources.md))

* cannot be null

* defined in: [JSON Test Schema](jsontest-properties-possible-external-method-sources.md "undefined#/properties/possibleExternalMethodSources")

### possibleExternalMethodSources Type

`object` ([Possible External Method Sources](jsontest-properties-possible-external-method-sources.md))

## testGroups

An array of test groups, each group containing a specific area that they cover (e.g. math operators)

`testGroups`

* is required

* Type: `object[]` ([Jolt Test Group](jsontest-properties-jolt-test-groups-jolt-test-group.md))

* cannot be null

* defined in: [JSON Test Schema](jsontest-properties-jolt-test-groups.md "undefined#/properties/testGroups")

### testGroups Type

`object[]` ([Jolt Test Group](jsontest-properties-jolt-test-groups-jolt-test-group.md))
