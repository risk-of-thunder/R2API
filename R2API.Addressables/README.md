# R2API.Addressables - Addressable related utility for modding

## About

R2API.Addressables is a submodule for R2API that implements systems for working with the AddressablesAPI from Unity.

Currently it adds the AddressReferencedAsset system, which allows you on the Editor or on Code to easily refer to either an explicit Asset, or an Address to said asset.

## Changelog

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
