# HouseModel_ScannedPlan

`HouseModelGenerator.cs` builds the proportional model of the private Thousand
Oaks home. Unity units are treated as feet. Exact address, parcel, appraisal,
survey, and raw capture source files are intentionally excluded from this
repository.

Private GLB source captures may be placed in a sibling `HouseSources/` directory
for local regeneration. `FirstFloorScanAssetBuilder` converts them into the
native assets under `Assets/Models/HomeScans/`. If sources or generated assets
are unavailable, the procedural interior remains the fallback.

Use **Tools → House Model → Create Scanned Plan Scene and Prefab** to regenerate
the prefab, scene, and house materials. `HouseModelController` retains the roof,
floor, wall, cutaway, exploded view, scan, and camera controls used by WebGL.
