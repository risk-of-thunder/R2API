# Changelog

## 2.0.0
- Fixed SurvivorAPI, you can now have more than one custom survivor
  - Note to DEVS: You must set SurvivorIndex to a value > 8 (SurvivorIndex.Count) in order for your Survivor to be added, otherwise it will replace the survivor at that index.
- Added Custom EntityStates support
- Added Fast Cached Reflection API, see: [Reflection.cs](https://github.com/risk-of-thunder/R2API/blob/master/R2API/Utils/Reflection.cs)
- Added In-Game Rule UI API, see: [LobbyConfigAPI.cs](https://github.com/risk-of-thunder/R2API/blob/master/R2API/LobbyConfigAPI.cs)
