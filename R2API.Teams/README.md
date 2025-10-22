# R2API.Teams - Custom teams addition

## About

An R2API submodule that allows the creation of custom character teams.

## Use Cases / Features

R2API.Teams can be used to add your own teams to the game for more complex combat relationships.

Use `TeamsAPI.RegisterTeam` to add a new team, the `TeamIndex` of the new team is returned. Any Enum parsing/ToString of TeamIndex is redirected to the added TeamIndex for compatibility.

## Related Pages

## Changelog

### '1.0.1'
* Add CanPickup virtual bool to TeamBehaviour.

### '1.0.0'
* Initial Release
