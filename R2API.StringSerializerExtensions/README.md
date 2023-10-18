# R2API.StringSerializerExtensions - Increased String Serializer support

## About

R2API.StringSerializerExtensions is a submodule assembly for R2API that adds new serialization handlers to the HG.GeneralSerializer.StringSerializer class

## Use Cases / Features

R2API.StringSerializerExtensions adds new serialization handlers to the HG.GeneralSerializer.StringSerializer class, particularly useful for Editor centered mods who use EntityStateConfigurations

It adds handlers for:
* LayerMask
* Vector4
* Rect
* RectInt
* Char
* Bounds
* BoundsInt
* Quaternion
* Vector2Int
* Vector3Int
* Any Enum Type, including Flags.

For Thunderkit Users, it is recommended that you install RoR2EditorKit alongside this submodule as it enables the EntityStateConfiguration inspector to serialize the new types.

## Changelog

### '1.0.0'

* Initial Release
