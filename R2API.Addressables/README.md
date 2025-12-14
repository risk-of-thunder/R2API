# R2API.Addressables - Addressable related utility for modding

## About

R2API.Addressables is a submodule for R2API that implements systems for working with the AddressablesAPI from Unity.

Currently it adds the AddressReferencedAsset system, which allows you on the Editor or on Code to easily refer to either an explicit Asset, or an Address to said asset.

## Changelog

### '1.1.3'

* Added the ``NotCatalogLoadable`` attribute, which when implemented on a field and in conjunction with RoR2EditorKit, forces an AddressReferencedAsset to never be loaded from the catalog.

### '1.1.2'

* Reimplemented version '1.1.0' with fixes, changes and improvements.

### '1.1.1'

* Revert 1.1.0

### '1.1.0'

* Implemented the ability to restrict a load to not use the ingame catalogs, forcing an address to be used instead.
* implemented IDisposeable an AddressReferencedAsset, which releases an internal handle for the addressable asset.
* Added the ability to bypass the secure loading by calling ResolveAsset().
* Replaced most of the Task returning methods with IEnumerator coroutines.

### '1.0.4'

* Added Safeguards for the loading process to avoid loading assets with addresses that are invalid or malformed.

### '1.0.3'

* Fixed a typo on ``AddressReferencedAsset(UnityEngine.Object)`` constructor which would incorrectly set ``UseDirectReference`` to ``false``
* Fixed issue where ``AddressReferencedEquipmentDef`` wasnt marked as "Serializable"

### '1.0.2'

* Added utility property to check wether an ``AddressReferencedAsset`` is "Invalid". Invalid in this instance means that no direct reference to an asset exists, and the string address is null, empty or whitespace.
* Addedd a utility property to check if an ``AddressReferencedAsset`` has a direct reference to an Asset.

### '1.0.1'

* Added missing casting operators and constructors for the bundled in, derived classes of ``AddressReferencedAsset<T>``

### '1.0.0'

* Initial Release
