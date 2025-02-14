# R2API.CostType - Custom Cost Type Handling

## About

R2API.CostType is a submodule assembly for R2API that simplifies the process of adding custom CostTypeDefs to avoid mod conflicts.

## Use Cases / Features

R2API.CostType provides two handlers for registering custom CostTypeDefs, both located in the CostAPI class.

`ReserveCostType` can be used to reserve a CostTypeIndex before the catalog has initialized, via a `CostTypeHolder` which can store a callback to run on reservation.

`RegisterCostType` will register a CostTypeDef immediately. This method may only be used after the catalog has initialized.

## Related Pages

## Changelog

### '1.0.0'
* Released.
