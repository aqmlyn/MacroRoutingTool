[AltSidesHelper](https://gamebanana.com/mods/166210) lets mappers assign maps as sides of chapters outside of the vanilla side system. MRT currently doesn't support this -- below are notes I've taken regarding what I'd need to do to add support.

# What I know
- AltSidesHelper associates `AreaData`s with `AltSidesHelperMeta`s [here](https://github.com/l-Luna/AltSidesHelper/blob/ab3b5339e760f37c6fc614e274647e3080a2c295/AltSidesHelperModule.cs#L27). That class stores an array of `AltSidesHelperMode`s [here](https://github.com/l-Luna/AltSidesHelper/blob/ab3b5339e760f37c6fc614e274647e3080a2c295/AltSidesHelperModule.cs#L888), similar to how `AreaData` stores an array of `ModeProperties`.
- The `AltSidesHelperMode` class is defined [here](https://github.com/l-Luna/AltSidesHelper/blob/ab3b5339e760f37c6fc614e274647e3080a2c295/AltSidesHelperModule.cs#L921), and all its properties are explained [here](https://github.com/l-Luna/AltSidesHelper/wiki/Customisable-Fields).

# What I'd need
- To check if a map uses AltSidesHelper, I think I can just check if the `AreaData` for the map's SID has an associated `AltSidesHelperMeta`.
- `Graph` class: instead of storing an `AreaMode`, I'd probably need to store integer and convert it to an `AreaMode` (index for `AreaData.Mode`) by default. Then if a map uses AltSidesHelper, I could instead leave it as an integer (index for `AltSidesHelperMeta.Sides`).
- When reading the side for an AltSidesHelper map, use the index in the `AltSidesHelperMeta`'s `Sides` instead of the `AreaData`'s `Mode`.
    - `GraphViewer` rendering: for an AltSidesHelper side, the side text's dialog key needs to be gotten from an `AltSidesHelperMode`'s `Label`.