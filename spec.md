# DataForge Binary Format Specification

This document summarizes the layout of the `DataForge` (.dcb) files used by `unforge`.
It is derived from the implementation under `src/unforge`.

## 1. File Header

A DataForge file begins with a header of 32-bit little‑endian fields:

| Offset | Field | Description |
|--------|-------|-------------|
| 0x00 | `temp00` | Unknown placeholder value. |
| 0x04 | `FileVersion` | Format version number. |
| 0x08 | `Header[4]` | Four 16‑bit fields present only in non‑legacy files; purpose unknown. |
| 0x10 | `StructDefinitionCount` | Number of struct definitions. |
| 0x14 | `PropertyDefinitionCount` | Number of property definitions. |
| 0x18 | `EnumDefinitionCount` | Number of enum definitions. |
| 0x1C | `DataMappingCount` | Number of data mapping entries. |
| 0x20 | `RecordDefinitionCount` | Number of record entries. |
| 0x24 | `BooleanValueCount` | Count of boolean values. |
| 0x28 | `Int8ValueCount` | Count of signed byte values. |
| 0x2C | `Int16ValueCount` | Count of 16‑bit integer values. |
| 0x30 | `Int32ValueCount` | Count of 32‑bit integer values. |
| 0x34 | `Int64ValueCount` | Count of 64‑bit integer values. |
| 0x38 | `UInt8ValueCount` | Count of unsigned byte values. |
| 0x3C | `UInt16ValueCount` | Count of unsigned 16‑bit integer values. |
| 0x40 | `UInt32ValueCount` | Count of unsigned 32‑bit integer values. |
| 0x44 | `UInt64ValueCount` | Count of unsigned 64‑bit integer values. |
| 0x48 | `SingleValueCount` | Count of 32‑bit float values. |
| 0x4C | `DoubleValueCount` | Count of 64‑bit float values. |
| 0x50 | `GuidValueCount` | Count of GUID values. |
| 0x54 | `StringValueCount` | Count of string references. |
| 0x58 | `LocaleValueCount` | Count of locale string references. |
| 0x5C | `EnumValueCount` | Count of enum indices. |
| 0x60 | `StrongValueCount` | Count of strong pointers. |
| 0x64 | `WeakValueCount` | Count of weak pointers. |
| 0x68 | `ReferenceValueCount` | Count of reference values. |
| 0x6C | `EnumOptionCount` | Number of enum option strings. |
| 0x70 | `TextLength` | Length in bytes of the text string table. |
| 0x74 | `BlobLength` | Length in bytes of the blob string table (zero for legacy files). |

These fields are read sequentially as shown in `DataForge.cs`【F:src/unforge/DataForge.cs†L76-L125】.

## 2. Definition Tables

Immediately following the header are several tables, each serialized as an array of the specified entry type.
The counts for these arrays come from the header.

### 2.1 Struct Definition Table
Each `DataForgeStructDefinition` entry has the following fields【F:src/unforge/ComplexTypes/DataForgeStructDefinition.cs†L11-L33】:

| Field | Type | Description |
|-------|------|-------------|
| `NameOffset` | `UInt32` | Offset into the blob string table for the struct name. |
| `ParentTypeIndex` | `UInt32` | Index of the parent struct or `0xFFFFFFFF` if none. |
| `AttributeCount` | `UInt16` | Number of property definitions for this struct. |
| `FirstAttributeIndex` | `UInt16` | Index into the Property Definition table for the first property. |
| `NodeType` | `UInt32` | Node type identifier. |

### 2.2 Property Definition Table
Each `DataForgePropertyDefinition` entry is read as【F:src/unforge/ComplexTypes/DataForgePropertyDefinition.cs†L10-L24】:

| Field | Type | Description |
|-------|------|-------------|
| `NameOffset` | `UInt32` | Offset into the blob string table for the property name. |
| `StructIndex` | `UInt16` | Struct index used for complex types or enums. |
| `DataType` | `UInt16` (`EDataType`) | Underlying data type. |
| `ConversionType` | `UInt16` (`EConversionType`) | How the value is stored (attribute vs. array). |
| `Padding` | `UInt16` | Reserved/unused. |

### 2.3 Enum Definition Table
Entries contain the enum name and the span of options within the enum option table【F:src/unforge/ComplexTypes/DataForgeEnumDefinition.cs†L8-L18】:

| Field | Type | Description |
|-------|------|-------------|
| `NameOffset` | `UInt32` | Offset into the blob string table for the enum name. |
| `ValueCount` | `UInt16` | Number of enum options. |
| `FirstValueIndex` | `UInt16` | Index into the Enum Option Table for the first option string. |

### 2.4 Data Mapping Table
Each `DataForgeDataMapping` entry provides a struct index and how many instances of that struct exist. It is version‑dependent【F:src/unforge/ComplexTypes/DataForgeDataMapping.cs†L15-L22】:

| Field | Type | Description |
|-------|------|-------------|
| `StructCount` | `UInt32` or `UInt16` | Number of serialized instances (32‑bit for FileVersion ≥ 5). |
| `StructIndex` | `UInt32` or `UInt16` | Index into the struct table. |
| `NameOffset` | `UInt32` | Derived from the struct's name offset. |

### 2.5 Record Definition Table
Records describe top‑level objects referenced by name and path【F:src/unforge/ComplexTypes/DataForgeRecord.cs†L25-L39】:

| Field | Type | Description |
|-------|------|-------------|
| `NameOffset` | `UInt32` | Offset for record name. |
| `FileNameOffset` | `UInt32` (absent in legacy files) | Offset into text table for original path. |
| `StructIndex` | `UInt32` | Struct type index. |
| `Hash` | `Guid` | Optional GUID reference. |
| `VariantIndex` | `UInt16` | Index into the struct's instance array. |
| `OtherIndex` | `UInt16` | Additional index value. |

## 3. Value Arrays

For each scalar or complex type, a contiguous array of values follows the definition tables. The arrays are read using the counts in the header and include (in order) signed integers, unsigned integers, floating‑point numbers, GUIDs, strings, locale strings, enums, strong pointers, weak pointers and reference records【F:src/unforge/DataForge.cs†L132-L151】.

Each entry type has its own structure under `src/unforge/SimpleTypes`.

## 4. String Tables

Two variable‑length string tables follow the value arrays:

* **TextMap** – a sequence of C‑strings totaling `TextLength` bytes. Offsets within this block index textual data such as property values or file names【F:src/unforge/DataForge.cs†L153-L163】.
* **BlobMap** – an optional second string block of `BlobLength` bytes used for localized or large data blobs; legacy files omit this block and reuse `TextMap`【F:src/unforge/DataForge.cs†L165-L177】.

Each string's offset within its block is used as a key throughout the format.

## 5. Enumerations

The format uses two enumerations defined in `Enums.cs`:

* `EDataType` enumerates the primitive and compound value kinds (boolean, integers, strings, pointers, etc.)【F:src/unforge/Enums.cs†L3-L23】.
* `EConversionType` specifies how properties are stored: as direct attributes, simple arrays, complex arrays or class arrays【F:src/unforge/Enums.cs†L26-L31】.

## 6. Data Assembly

Data mappings associate struct definitions with arrays of actual data. Each mapping states how many instances of a struct exist, allowing the loader to deserialize them into XML nodes. Record definitions then reference specific struct instances by name and file path to build the final document structure.

This specification captures the structure of `DataForge` files as interpreted by the `unforge` tool.
