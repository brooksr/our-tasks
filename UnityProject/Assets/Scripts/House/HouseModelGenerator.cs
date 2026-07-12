using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CommerceDemo
{
    /// <summary>
    /// Procedurally builds HouseModel_ScannedPlan from built-in meshes.
    /// Unity units are treated as feet. Models the private Thousand Oaks home
    /// (Greenwich Village Tract, Block 27, Lot 20) — Plan "B" of the 1976
    /// A. Bacchetta &amp; Associates set (76-6422.pdf): entry wing front-LEFT,
    /// 2-car garage front-RIGHT, 4:12 roofs, 29'-10" wide. The current interior
    /// follows the June 2021 appraisal sketch and owner corrections: an open
    /// living/dining/kitchen level with a powder bath, a turning stair beside
    /// the front entry, and three bedrooms plus one full bath upstairs. Colors
    /// and the elevated rear deck follow the appraisal photos (charcoal stucco,
    /// white trim/garage door, paver driveway, turf lawn, and an open wood deck
    /// on posts over the rear downslope).
    /// </summary>
    [ExecuteAlways]
    public class HouseModelGenerator : MonoBehaviour
    {
        enum WallSide
        {
            Front,
            Rear,
            Left,
            Right
        }

        enum RoofOrientation
        {
            RidgeAlongDepth,
            RidgeAlongWidth
        }

        [Header("Generation")]
        public bool generateOnAwake = true;
        public bool generateInEditMode = true;
        public bool clearExisting = true;
        public bool includeInteriorReference = true;
        public bool useScannedFirstFloorInterior = true;
        public bool includeSite = true;
        public bool includeCamerasAndLighting = true;

        [Header("As-built first-floor scan")]
        [SerializeField] Mesh scannedFirstFloorMeshOverride;
        [SerializeField] Material scannedFirstFloorMaterialOverride;
        // Yaw +90 turns the raw Scaniverse capture a quarter-turn clockwise
        // (top-down) so the kitchen sits on the garage/right side and the stair
        // + front door land at the front. Position keeps the footprint centered
        // at (0, 7.66) after the rotation about the mesh origin.
        [SerializeField] Vector3 scannedFirstFloorPosition = new Vector3(9.208f, 11.322f, 17.257f);
        [SerializeField] Vector3 scannedFirstFloorRotation = new Vector3(0f, 110f, 0f);
        [SerializeField] float scannedFirstFloorScale = 3.0f;

        // Garage scan: centered in the garage volume, floor on the slab (Y=0),
        // scaled to the ~21x19 ft garage footprint. Yaw still to be dialed in.
        [Header("As-built garage scan")]
        public bool useScannedGarage = true;
        [SerializeField] Mesh scannedGarageMeshOverride;
        [SerializeField] Material scannedGarageMaterialOverride;
        [SerializeField] Vector3 scannedGaragePosition = new Vector3(6.190f, 6.410f, -6.453f);
        [SerializeField] Vector3 scannedGarageRotation = new Vector3(0f, 180f, 0f);
        [SerializeField] float scannedGarageScale = 3.74f;

        // Upstairs scan: centered on the second floor, floor on the plate
        // (Y = firstFloorHeight). Yaw still to be dialed in.
        [Header("As-built upstairs scan")]
        public bool useScannedUpstairs = true;
        [SerializeField] Mesh scannedUpstairsMeshOverride;
        [SerializeField] Material scannedUpstairsMaterialOverride;
        [SerializeField] Vector3 scannedUpstairsPosition = new Vector3(0.786f, 12.080f, 9.391f);
        [SerializeField] Vector3 scannedUpstairsRotation = new Vector3(0f, 200f, 0f);
        [SerializeField] float scannedUpstairsScale = 2.8f;

        [Header("Approximate feet scale")]
        [SerializeField] float mainWidth = 29.83f; // Plans show 29'-10" overall.
        [SerializeField] float mainDepth = 24.5f; // Living block depth; rear wall aligned to the as-built scan's back wall.
        [SerializeField] float garageWidth = 20.83f;
        [SerializeField] float garageDepth = 20.67f;
        [SerializeField] float entryWingDepth = 14.67f; // Front-left bay projects to a front wall ~6 ft behind the garage face (owner, 2026-07-12).
        [SerializeField] float secondFloorFrontWingWidth = 20.83f;
        [SerializeField] float secondFloorFrontWingDepth = 9.2f;
        [SerializeField] float firstFloorHeight = 8f;
        [SerializeField] float secondFloorHeight = 8f;
        [SerializeField] float wallThickness = 0.45f;
        [SerializeField] float roofPitchRisePerFoot = 4f / 12f; // Plan "B" cross-section notes 4:12 (Plan "A" is 5:12).
        [SerializeField] float eaveOverhang = 1.2f;
        [SerializeField] float rearDeckDepth = 10.5f; // Open elevated deck across the rear (appraisal photos).
        [SerializeField] float rearGradeDrop = 8.3f; // Lot falls away behind the house (~2:1 slope per 1976 soils report).

        [Header("Optional material assets")]
        [SerializeField] Material stuccoMaterialOverride;
        [SerializeField] Material stuccoAccentMaterialOverride;
        [SerializeField] Material roofMaterialOverride;
        [SerializeField] Material fasciaMaterialOverride;
        [SerializeField] Material trimMaterialOverride;
        [SerializeField] Material garageDoorMaterialOverride;
        [SerializeField] Material glassMaterialOverride;
        [SerializeField] Material doorMaterialOverride;
        [SerializeField] Material concreteMaterialOverride;
        [SerializeField] Material groundMaterialOverride;
        [SerializeField] Material drivewayMaterialOverride;
        [SerializeField] Material interiorWallMaterialOverride;
        [SerializeField] Material railingMaterialOverride;
        [SerializeField] Material darkTrimMaterialOverride;
        [SerializeField] Material deckMaterialOverride;

        const float MainFrontZ = 0f;
        const float SlabThickness = 0.32f;
        const float TrimDepth = 0.14f;

        static Mesh _cubeMesh;

        Material _stucco;
        Material _stuccoAccent;
        Material _roof;
        Material _fascia;
        Material _trim;
        Material _garageDoor;
        Material _glass;
        Material _door;
        Material _concrete;
        Material _ground;
        Material _driveway;
        Material _interiorWall;
        Material _railing;
        Material _darkTrim;
        Material _deck;
        Material _lawn;
        Material _hardwood;
        Material _epoxy;
        Material _granite;
        Material _cabinet;
        Material _appliance;
        Material _wallPaint;
        Material _water;

        GameObject _foundation;
        GameObject _firstFloor;
        GameObject _firstExterior;
        GameObject _firstInterior;
        GameObject _garage;
        GameObject _frontPorch;
        GameObject _rearPorch;
        GameObject _doorsFirst;
        GameObject _windowsFirst;
        GameObject _secondFloor;
        GameObject _secondExterior;
        GameObject _secondInterior;
        GameObject _windowsSecond;
        GameObject _roofs;
        GameObject _mainRoof;
        GameObject _garageRoof;
        GameObject _entryWingRoof;
        GameObject _rearRoofSections;
        GameObject _eavesAndFascia;
        GameObject _details;
        GameObject _railings;
        GameObject _trimGroup;
        GameObject _gutters;
        GameObject _stairs;
        GameObject _finishes;
        GameObject _site;
        GameObject _cameras;
        GameObject _lighting;

        readonly List<GameObject> _cutawayWalls = new List<GameObject>();
        bool _isBuilding;

        // Plan "B" orientation: garage hugs the RIGHT side of
        // the street facade, entry/stair wing fills the LEFT side.
        float MainRearZ => MainFrontZ + mainDepth;
        float MainCenterZ => MainFrontZ + mainDepth * 0.5f;
        float MainLeftX => -mainWidth * 0.5f;
        float MainRightX => mainWidth * 0.5f;
        float GarageCenterX => MainRightX - garageWidth * 0.5f;
        float GarageFrontZ => MainFrontZ - garageDepth;
        float GarageCenterZ => MainFrontZ - garageDepth * 0.5f;
        float GarageLeftX => GarageCenterX - garageWidth * 0.5f;
        float GarageRightX => GarageCenterX + garageWidth * 0.5f;
        float EntryLeftX => MainLeftX;
        float EntryRightX => GarageLeftX;
        float EntryCenterX => (EntryLeftX + EntryRightX) * 0.5f;
        float EntryWingWidth => EntryRightX - EntryLeftX;
        float EntryFrontZ => MainFrontZ - entryWingDepth;
        float EntryCenterZ => (EntryFrontZ + MainFrontZ) * 0.5f;
        float SecondFloorFrontWingLeftX => MainRightX - secondFloorFrontWingWidth;
        float SecondFloorRearFrontZ => MainFrontZ + secondFloorFrontWingDepth;
        float SecondFloorRearDepth => mainDepth - secondFloorFrontWingDepth;
        float SecondFloorRearCenterZ => (SecondFloorRearFrontZ + MainRearZ) * 0.5f;
        float SecondFloorFrontWingCenterX => (SecondFloorFrontWingLeftX + MainRightX) * 0.5f;
        float SecondFloorFrontWingCenterZ => (MainFrontZ + SecondFloorRearFrontZ) * 0.5f;
        float RearLowerGroundY => -rearGradeDrop;
        float SecondBaseY => firstFloorHeight;
        float MainRoofBaseY => firstFloorHeight + secondFloorHeight;
        float GarageRoofBaseY => firstFloorHeight;
        bool HasScannedFirstFloor => useScannedFirstFloorInterior && scannedFirstFloorMeshOverride != null && scannedFirstFloorMaterialOverride != null;
        bool HasScannedGarage => useScannedGarage && scannedGarageMeshOverride != null && scannedGarageMaterialOverride != null;
        bool HasScannedUpstairs => useScannedUpstairs && scannedUpstairsMeshOverride != null && scannedUpstairsMaterialOverride != null;

        void Reset()
        {
            gameObject.name = "HouseModel_ScannedPlan";
        }

        void OnEnable()
        {
            if (Application.isPlaying) return;
            if (!generateOnAwake || !generateInEditMode || transform.childCount > 0) return;
            BuildHouse();
        }

        void Awake()
        {
            if (Application.isPlaying && generateOnAwake)
            {
                BuildHouse();
            }
        }

        [ContextMenu("Regenerate House Model")]
        public void BuildHouse()
        {
            if (_isBuilding) return;
            _isBuilding = true;

            if (string.IsNullOrEmpty(gameObject.name) || gameObject.name == "GameObject")
            {
                gameObject.name = "HouseModel_ScannedPlan";
            }

            if (clearExisting)
            {
                ClearChildren();
            }

            _cutawayWalls.Clear();
            CreateMaterials();
            CreateHierarchy();
            BuildFoundation();
            BuildFirstFloor();
            BuildSecondFloor();
            BuildRoofs();
            BuildDetails();
            BuildProceduralFinishes();
            if (includeSite) BuildSite();
            if (includeCamerasAndLighting) BuildCamerasAndLighting();
            BindController();

            _isBuilding = false;
        }

        public void SetMaterialOverrides(
            Material stucco,
            Material stuccoAccent,
            Material roof,
            Material fascia,
            Material trim,
            Material garageDoor,
            Material glass,
            Material door,
            Material concrete,
            Material ground,
            Material driveway,
            Material interiorWall,
            Material railing,
            Material darkTrim,
            Material deck)
        {
            stuccoMaterialOverride = stucco;
            stuccoAccentMaterialOverride = stuccoAccent;
            roofMaterialOverride = roof;
            fasciaMaterialOverride = fascia;
            trimMaterialOverride = trim;
            garageDoorMaterialOverride = garageDoor;
            glassMaterialOverride = glass;
            doorMaterialOverride = door;
            concreteMaterialOverride = concrete;
            groundMaterialOverride = ground;
            drivewayMaterialOverride = driveway;
            interiorWallMaterialOverride = interiorWall;
            railingMaterialOverride = railing;
            darkTrimMaterialOverride = darkTrim;
            deckMaterialOverride = deck;
        }

        public void SetScannedFirstFloorOverride(Mesh scanMesh, Material scanMaterial)
        {
            scannedFirstFloorMeshOverride = scanMesh;
            scannedFirstFloorMaterialOverride = scanMaterial;
        }

        public void SetScannedGarageOverride(Mesh scanMesh, Material scanMaterial)
        {
            scannedGarageMeshOverride = scanMesh;
            scannedGarageMaterialOverride = scanMaterial;
        }

        public void SetScannedUpstairsOverride(Mesh scanMesh, Material scanMaterial)
        {
            scannedUpstairsMeshOverride = scanMesh;
            scannedUpstairsMaterialOverride = scanMaterial;
        }

        void ClearChildren()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i).gameObject;
                child.transform.SetParent(null);
                if (Application.isPlaying)
                {
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
            }
        }

        void CreateHierarchy()
        {
            _foundation = Group("Foundation", gameObject);

            _firstFloor = Group("FirstFloor", gameObject);
            _firstExterior = Group("ExteriorWalls", _firstFloor);
            _firstInterior = Group("InteriorReferenceWalls", _firstFloor);
            _garage = Group("Garage", _firstFloor);
            _frontPorch = Group("FrontPorch", _firstFloor);
            _rearPorch = Group("RearPorch", _firstFloor);
            _doorsFirst = Group("Doors", _firstFloor);
            _windowsFirst = Group("Windows", _firstFloor);

            _secondFloor = Group("SecondFloor", gameObject);
            _secondExterior = Group("ExteriorWalls", _secondFloor);
            _secondInterior = Group("InteriorReferenceWalls", _secondFloor);
            _windowsSecond = Group("Windows", _secondFloor);

            _roofs = Group("Roofs", gameObject);
            _mainRoof = Group("MainGableRoof", _roofs);
            _garageRoof = Group("GarageGableRoof", _roofs);
            _entryWingRoof = Group("EntryWingGableRoof", _roofs);
            _rearRoofSections = Group("RearRoofSections", _roofs);
            _eavesAndFascia = Group("EavesAndFascia", _roofs);

            _details = Group("Details", gameObject);
            _railings = Group("Railings", _details);
            _trimGroup = Group("Trim", _details);
            _gutters = Group("Gutters", _details);
            _stairs = Group("StairsApproximation", _details);

            _finishes = Group("ProceduralFinishes", gameObject);
            _site = Group("Site", gameObject);
            _cameras = Group("Cameras", gameObject);
            _lighting = Group("Lighting", gameObject);
        }

        void BuildFoundation()
        {
            CreateBox(_foundation, "MainRearBlockConcreteSlab",
                new Vector3(0f, -SlabThickness * 0.5f, MainCenterZ),
                new Vector3(mainWidth + 0.8f, SlabThickness, mainDepth + 0.8f), _concrete);

            CreateBox(_foundation, "GarageConcreteSlab",
                new Vector3(GarageCenterX, -SlabThickness * 0.5f, GarageCenterZ),
                new Vector3(garageWidth + 0.7f, SlabThickness, garageDepth + 0.7f), _concrete);

            CreateBox(_foundation, "EntryWingConcreteSlab",
                new Vector3(EntryCenterX, -SlabThickness * 0.5f, EntryCenterZ),
                new Vector3(EntryWingWidth + 0.6f, SlabThickness, entryWingDepth + 0.6f), _concrete);

            CreateBox(_foundation, "PerimeterFooting_MainRear",
                new Vector3(0f, -0.62f, MainRearZ + 0.25f),
                new Vector3(mainWidth + 1.2f, 0.36f, 0.55f), _concrete);
            CreateBox(_foundation, "PerimeterFooting_MainLeft",
                new Vector3(MainLeftX - 0.25f, -0.62f, MainCenterZ),
                new Vector3(0.55f, 0.36f, mainDepth + 1.2f), _concrete);
            CreateBox(_foundation, "PerimeterFooting_MainRight",
                new Vector3(MainRightX + 0.25f, -0.62f, MainCenterZ),
                new Vector3(0.55f, 0.36f, mainDepth + 1.2f), _concrete);
            CreateBox(_foundation, "PerimeterFooting_GarageFront",
                new Vector3(GarageCenterX, -0.62f, GarageFrontZ - 0.25f),
                new Vector3(garageWidth + 1f, 0.36f, 0.55f), _concrete);
            CreateBox(_foundation, "PerimeterFooting_EntryWingFront",
                new Vector3(EntryCenterX, -0.62f, EntryFrontZ - 0.25f),
                new Vector3(EntryWingWidth + 1f, 0.36f, 0.55f), _concrete);
            CreateBox(_foundation, "PerimeterFooting_EntryWingLeft",
                new Vector3(MainLeftX - 0.25f, -0.62f, EntryCenterZ),
                new Vector3(0.55f, 0.36f, entryWingDepth), _concrete);
        }

        void BuildFirstFloor()
        {
            float y = firstFloorHeight * 0.5f;

            CreateBox(_firstExterior, "FirstFloor_RearWall",
                new Vector3(0f, y, MainRearZ + wallThickness * 0.5f),
                new Vector3(mainWidth, firstFloorHeight, wallThickness), _stucco);

            CreateBox(_firstExterior, "FirstFloor_RightExteriorWall",
                new Vector3(MainRightX + wallThickness * 0.5f, y, MainCenterZ),
                new Vector3(wallThickness, firstFloorHeight, mainDepth), _stuccoAccent);

            var cutLeft = CreateBox(_firstExterior, "FirstFloor_LeftExterior_CutawayWall",
                new Vector3(MainLeftX - wallThickness * 0.5f, y, MainCenterZ),
                new Vector3(wallThickness, firstFloorHeight, mainDepth), _stucco);
            _cutawayWalls.Add(cutLeft);

            BuildFrontEntry();

            BuildGarage();
            BuildFirstFloorPorches();
            BuildFirstFloorOpenings();
            if (includeInteriorReference)
            {
                if (HasScannedFirstFloor) BuildScannedFirstFloorInterior();
                else BuildFirstFloorInteriorReference();
            }
        }

        void BuildScannedFirstFloorInterior()
        {
            PlaceScan(_firstInterior, "AsBuiltFirstFloorScan", "FirstFloorScanMesh",
                scannedFirstFloorMeshOverride, scannedFirstFloorMaterialOverride,
                scannedFirstFloorPosition, scannedFirstFloorRotation, scannedFirstFloorScale);
        }

        // Instantiates a Scaniverse scan mesh under the given parent with the
        // supplied local transform. Shared across the first-floor, garage, and
        // upstairs captures.
        void PlaceScan(GameObject parent, string groupName, string meshName,
            Mesh mesh, Material material, Vector3 position, Vector3 rotation, float scale)
        {
            var root = Group(groupName, parent);
            var scan = new GameObject(meshName);
            scan.transform.SetParent(root.transform, false);
            scan.transform.localPosition = position;
            scan.transform.localRotation = Quaternion.Euler(rotation);
            scan.transform.localScale = Vector3.one * scale;

            var filter = scan.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var renderer = scan.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }

        // The front-left living/entry bay projects toward the street as a
        // two-story wing (owner correction 2026-07-12): its street-facing front
        // wall — carrying the front door — lands about 6 ft behind the garage
        // face, roughly 10 ft off the curb, and it carries its own lower gable
        // roof. The bay is open at the back to the main living room; the
        // garage's left wall doubles as its right side on the first floor.
        float FrontDoorCenterX => MainLeftX + 5.6f;

        void BuildFrontEntry()
        {
            float y = firstFloorHeight * 0.5f;
            float doorWidth = 3.2f;
            float doorHeight = 6.8f;
            float doorLeftX = FrontDoorCenterX - doorWidth * 0.5f;
            float doorRightX = FrontDoorCenterX + doorWidth * 0.5f;

            // Left exterior wall of the wing, coplanar with the main left wall
            // and treated as a cutaway wall so the interior stays visible.
            var wingLeft = CreateBox(_firstExterior, "FirstFloor_EntryWing_LeftWall_CutawayWall",
                new Vector3(MainLeftX - wallThickness * 0.5f, y, EntryCenterZ),
                new Vector3(wallThickness, firstFloorHeight, entryWingDepth), _stucco);
            _cutawayWalls.Add(wingLeft);

            // Street-facing front wall with the door opening punched at the door
            // center (exterior stucco on both jambs plus a header above).
            float frontZ = EntryFrontZ - wallThickness * 0.5f;
            float leftJambWidth = doorLeftX - EntryLeftX;
            CreateBox(_firstExterior, "FirstFloor_EntryWing_FrontWall_LeftJamb",
                new Vector3(EntryLeftX + leftJambWidth * 0.5f, y, frontZ),
                new Vector3(leftJambWidth, firstFloorHeight, wallThickness), _stucco);
            float rightJambWidth = EntryRightX - doorRightX;
            CreateBox(_firstExterior, "FirstFloor_EntryWing_FrontWall_RightJamb",
                new Vector3(doorRightX + rightJambWidth * 0.5f, y, frontZ),
                new Vector3(rightJambWidth, firstFloorHeight, wallThickness), _stucco);
            float headerHeight = firstFloorHeight - doorHeight;
            CreateBox(_firstExterior, "FirstFloor_EntryWing_FrontWall_DoorHeader",
                new Vector3(FrontDoorCenterX, doorHeight + headerHeight * 0.5f, frontZ),
                new Vector3(doorWidth, headerHeight, wallThickness), _stucco);

            AddSwingDoor(_doorsFirst, "FrontEntryDoor", WallSide.Front,
                new Vector3(FrontDoorCenterX, doorHeight * 0.5f + 0.05f, EntryFrontZ),
                doorWidth - 0.4f, doorHeight - 0.2f, _door);

            // Tall stair sidelight beside the door (the vertical window strip on
            // the street elevation).
            AddWindow(_windowsFirst, "EntryWingStairSidelight", WallSide.Front,
                new Vector3(doorRightX + 1.0f, 4.6f, EntryFrontZ - wallThickness - 0.03f), 1.2f, 6.2f, true);
        }

        void BuildGarage()
        {
            float y = firstFloorHeight * 0.5f;

            CreateBox(_garage, "Garage_FrontWall",
                new Vector3(GarageCenterX, y, GarageFrontZ - wallThickness * 0.5f),
                new Vector3(garageWidth, firstFloorHeight, wallThickness), _stucco);
            CreateBox(_garage, "Garage_RightWall",
                new Vector3(GarageRightX + wallThickness * 0.5f, y, GarageCenterZ),
                new Vector3(wallThickness, firstFloorHeight, garageDepth), _stuccoAccent);
            CreateBox(_garage, "Garage_LeftWall",
                new Vector3(GarageLeftX - wallThickness * 0.5f, y, GarageCenterZ),
                new Vector3(wallThickness, firstFloorHeight, garageDepth), _stucco);
            CreateBox(_garage, "Garage_BackConnectionWall",
                new Vector3(GarageCenterX, y, MainFrontZ + wallThickness * 0.5f),
                new Vector3(garageWidth, firstFloorHeight, wallThickness), _stucco);

            if (HasScannedGarage)
            {
                PlaceScan(_garage, "AsBuiltGarageScan", "GarageScanMesh",
                    scannedGarageMeshOverride, scannedGarageMaterialOverride,
                    scannedGaragePosition, scannedGarageRotation, scannedGarageScale);
            }
            else
            {
                CreateBox(_garage, "Garage_InteriorSideWallReference",
                    new Vector3(GarageLeftX + 1.2f, 2f, GarageCenterZ + 1.8f),
                    new Vector3(0.18f, 4f, garageDepth - 4f), _interiorWall);
            }
        }

        void BuildFirstFloorPorches()
        {
            CreateBox(_frontPorch, "FrontEntryStoop",
                new Vector3(FrontDoorCenterX, 0.08f, EntryFrontZ - 1.5f),
                new Vector3(4.4f, 0.18f, 3.0f), _concrete);
            CreateBox(_frontPorch, "FrontEntryStep",
                new Vector3(FrontDoorCenterX, -0.04f, EntryFrontZ - 3.1f),
                new Vector3(4.8f, 0.14f, 1.1f), _concrete);

            BuildRearElevatedDeck();
        }

        // Open elevated wood deck across the rear at first-floor level, carried
        // on posts down to the lower rear grade (2021 appraisal rear photo).
        void BuildRearElevatedDeck()
        {
            float deckWidth = mainWidth - 1.0f;
            float deckFrontZ = MainRearZ + wallThickness;
            float deckRearZ = deckFrontZ + rearDeckDepth;
            float deckCenterZ = (deckFrontZ + deckRearZ) * 0.5f;
            float deckTopY = -0.05f;

            CreateBox(_rearPorch, "RearDeckPlatform",
                new Vector3(0f, deckTopY - 0.14f, deckCenterZ),
                new Vector3(deckWidth, 0.28f, rearDeckDepth), _deck);
            CreateBox(_rearPorch, "RearDeckRimJoist",
                new Vector3(0f, deckTopY - 0.55f, deckRearZ - 0.1f),
                new Vector3(deckWidth, 0.75f, 0.2f), _deck);

            // 4-person hot tub on the deck, tucked toward the right end.
            AddHotTub(_rearPorch, new Vector3(deckWidth * 0.25f, deckTopY, deckCenterZ + 0.5f));

            float postTopY = deckTopY - 0.28f;
            float postHeight = postTopY - RearLowerGroundY;
            float[] postXs = { -deckWidth * 0.5f + 0.6f, -deckWidth * 0.17f, deckWidth * 0.17f, deckWidth * 0.5f - 0.6f };
            foreach (float px in postXs)
            {
                CreateBox(_rearPorch, $"DeckPost_Rear_{px:0}",
                    new Vector3(px, RearLowerGroundY + postHeight * 0.5f, deckRearZ - 0.7f),
                    new Vector3(0.55f, postHeight, 0.55f), _deck);
                CreateBox(_rearPorch, $"DeckPost_Mid_{px:0}",
                    new Vector3(px, RearLowerGroundY + postHeight * 0.5f, deckFrontZ + rearDeckDepth * 0.45f),
                    new Vector3(0.55f, postHeight, 0.55f), _deck);
            }

            // White-post railing with light rail infill around the open sides.
            float railBaseY = deckTopY;
            AddRailing(_railings, "RearDeckBackRail",
                new Vector3(-deckWidth * 0.5f + 0.2f, railBaseY, deckRearZ - 0.25f),
                new Vector3(deckWidth * 0.5f - 0.2f, railBaseY, deckRearZ - 0.25f), 3.0f);
            AddRailing(_railings, "RearDeckRightRail",
                new Vector3(deckWidth * 0.5f - 0.2f, railBaseY, deckFrontZ + 0.3f),
                new Vector3(deckWidth * 0.5f - 0.2f, railBaseY, deckRearZ - 0.4f), 3.0f);
            AddRailing(_railings, "RearDeckLeftRail",
                new Vector3(-deckWidth * 0.5f + 0.2f, railBaseY, deckFrontZ + 0.3f),
                new Vector3(-deckWidth * 0.5f + 0.2f, railBaseY, deckRearZ - 3.6f), 3.0f);

            // Straight wood stair run from the deck's left end down to the lower
            // rear grade: treads + risers carried on two diagonal stringers with
            // support posts to grade, so it reads as a solid structure.
            int steps = 13;
            float rise = (deckTopY - RearLowerGroundY - 0.1f) / steps;
            float run = 0.9f;
            float stairX = -deckWidth * 0.5f + 2.0f;
            float stairStartZ = deckRearZ;
            for (int i = 1; i <= steps; i++)
            {
                float tY = deckTopY - i * rise;
                float tZ = stairStartZ + (i - 0.5f) * run;
                CreateBox(_stairs, $"DeckStairTread_{i:00}",
                    new Vector3(stairX, tY, tZ), new Vector3(3.6f, 0.16f, run + 0.1f), _deck);
                CreateBox(_stairs, $"DeckStairRiser_{i:00}",
                    new Vector3(stairX, tY - rise * 0.5f, tZ - run * 0.5f), new Vector3(3.6f, rise, 0.08f), _deck);
            }

            Vector3 strTop = new Vector3(stairX, deckTopY - 0.35f, stairStartZ);
            Vector3 strBot = new Vector3(stairX, deckTopY - steps * rise - 0.35f, stairStartZ + steps * run);
            CreateBeamBetween(_stairs, "DeckStairStringerL", strTop + new Vector3(-1.7f, 0f, 0f), strBot + new Vector3(-1.7f, 0f, 0f), 0.22f, 0.7f, _deck);
            CreateBeamBetween(_stairs, "DeckStairStringerR", strTop + new Vector3(1.7f, 0f, 0f), strBot + new Vector3(1.7f, 0f, 0f), 0.22f, 0.7f, _deck);

            foreach (float t in new[] { 0.5f, 0.92f })
            {
                Vector3 p = Vector3.Lerp(strTop, strBot, t);
                float ph = p.y - RearLowerGroundY;
                if (ph > 0.5f)
                {
                    CreateBox(_stairs, $"DeckStairSupport_{t:0.00}",
                        new Vector3(p.x, RearLowerGroundY + ph * 0.5f, p.z), new Vector3(0.42f, ph, 0.42f), _deck);
                }
            }

            CreateBox(_stairs, "DeckStairLandingPad",
                new Vector3(stairX, RearLowerGroundY - 0.1f, stairStartZ + steps * run + 1.6f),
                new Vector3(5.2f, 0.2f, 5.5f), _concrete);

            Vector3 railTop = new Vector3(0f, deckTopY, stairStartZ);
            Vector3 railBot = new Vector3(0f, RearLowerGroundY + 0.15f, stairStartZ + steps * run);
            AddStairRail(_railings, "RearDeckStairRailLeft",
                railTop + new Vector3(stairX - 1.8f, 0f, 0f),
                railBot + new Vector3(stairX - 1.8f, 0f, 0f), 3.0f);
            AddStairRail(_railings, "RearDeckStairRailRight",
                railTop + new Vector3(stairX + 1.8f, 0f, 0f),
                railBot + new Vector3(stairX + 1.8f, 0f, 0f), 3.0f);
        }

        // Railing that follows a sloped stair run: vertical posts along the
        // slope with top/mid rails as inclined beams.
        void AddStairRail(GameObject parent, string name, Vector3 start, Vector3 end, float height)
        {
            var root = Group(name, parent);
            Vector3 delta = end - start;
            float horizontalLength = new Vector2(delta.x, delta.z).magnitude;
            int intervals = Mathf.Max(1, Mathf.CeilToInt(horizontalLength / 3f));

            for (int i = 0; i <= intervals; i++)
            {
                float t = i / (float)intervals;
                Vector3 p = Vector3.Lerp(start, end, t);
                CreateBox(root, $"Post_{i + 1:00}", new Vector3(p.x, p.y + height * 0.5f, p.z), new Vector3(0.18f, height, 0.18f), _railing);
            }

            CreateBeamBetween(root, "TopRail", start + Vector3.up * height, end + Vector3.up * height, 0.16f, 0.16f, _railing);
            CreateBeamBetween(root, "MidRail", start + Vector3.up * (height * 0.55f), end + Vector3.up * (height * 0.55f), 0.12f, 0.12f, _railing);
        }

        void BuildFirstFloorOpenings()
        {
            float mainRearFace = MainRearZ + wallThickness + 0.03f;
            float garageFrontFace = GarageFrontZ - wallThickness - 0.03f;
            float rightFace = MainRightX + wallThickness + 0.03f;

            AddGarageDoor("DoubleGarageDoor", new Vector3(GarageCenterX, 3.45f, garageFrontFace), 16.2f, 6.9f);
            // Front entry door is built with the recessed entry tower.

            // Three large sliding glass doors across the rear, opening onto the deck.
            AddSlidingDoor(_doorsFirst, "RearSlidingGlassDoorLeft", WallSide.Rear,
                new Vector3(-9.6f, 3.35f, mainRearFace), 7.0f, 6.7f);
            AddSlidingDoor(_doorsFirst, "RearSlidingGlassDoorCenter", WallSide.Rear,
                new Vector3(0f, 3.35f, mainRearFace), 7.0f, 6.7f);
            AddSlidingDoor(_doorsFirst, "RearSlidingGlassDoorRight", WallSide.Rear,
                new Vector3(9.6f, 3.35f, mainRearFace), 7.0f, 6.7f);

            // The only first-floor side window is the kitchen window (right side).
            AddWindow(_windowsFirst, "RightKitchenSideWindow", WallSide.Right,
                new Vector3(rightFace, 4.6f, 5.8f), 3.4f, 3.2f, true);
        }

        void BuildFirstFloorInteriorReference()
        {
            const float h = 3.2f;
            float powderLeftX = EntryRightX + 0.7f;
            float powderRightX = powderLeftX + 4.5f;
            float powderFrontZ = MainFrontZ + 0.8f;
            float powderBackZ = MainFrontZ + 6.0f;

            // The garage remains separated from the conditioned lower floor.
            AddInteriorWall(_firstInterior, "GarageKitchenPartition",
                new Vector3(GarageLeftX + 0.1f, 0f, MainFrontZ + 0.25f),
                new Vector3(MainRightX - 0.2f, 0f, MainFrontZ + 0.25f), h);

            // The appraisal sketch records a single open living/dining/kitchen
            // area. Only the compact half bath beside the stair is enclosed.
            AddInteriorWall(_firstInterior, "PowderBathRearWall",
                new Vector3(powderLeftX, 0f, powderBackZ),
                new Vector3(powderRightX, 0f, powderBackZ), h);
            AddInteriorWall(_firstInterior, "PowderBathWestWall",
                new Vector3(powderLeftX, 0f, powderFrontZ),
                new Vector3(powderLeftX, 0f, powderBackZ), h);
            AddInteriorWall(_firstInterior, "PowderBathEastWall",
                new Vector3(powderRightX, 0f, powderFrontZ),
                new Vector3(powderRightX, 0f, powderBackZ), h);
            AddInteriorWall(_firstInterior, "PowderBathFrontWall_LeftJamb",
                new Vector3(powderLeftX, 0f, powderFrontZ),
                new Vector3(powderLeftX + 0.65f, 0f, powderFrontZ), h);
            AddInteriorWall(_firstInterior, "PowderBathFrontWall_RightJamb",
                new Vector3(powderLeftX + 3.05f, 0f, powderFrontZ),
                new Vector3(powderRightX, 0f, powderFrontZ), h);

            // Low-detail proxies make the open kitchen and half bath readable
            // in cutaway and plan views without closing either sightline.
            CreateBox(_firstInterior, "KitchenGarageWallCounterReference",
                new Vector3(8.0f, 1.15f, 1.35f), new Vector3(10.8f, 2.3f, 1.6f), _interiorWall);
            CreateBox(_firstInterior, "KitchenRightWallCounterReference",
                new Vector3(13.1f, 1.15f, 5.1f), new Vector3(1.55f, 2.3f, 5.9f), _interiorWall);
            CreateBox(_firstInterior, "OpenKitchenIslandReference",
                new Vector3(7.4f, 1.05f, 8.3f), new Vector3(6.4f, 2.1f, 2.6f), _interiorWall);
            CreateBox(_firstInterior, "PowderBathVanityReference",
                new Vector3(powderRightX - 0.7f, 1.05f, powderBackZ - 0.8f),
                new Vector3(1.4f, 2.1f, 1.45f), _interiorWall);
            CreateBox(_firstInterior, "PowderBathToiletReference",
                new Vector3(powderLeftX + 1.0f, 0.7f, powderBackZ - 1.0f),
                new Vector3(1.25f, 1.4f, 1.8f), _interiorWall);
        }

        void BuildSecondFloor()
        {
            CreateBox(_secondFloor, "SecondFloorPlate_RearBedroomBlock",
                new Vector3(0f, SecondBaseY + 0.08f, SecondFloorRearCenterZ),
                new Vector3(mainWidth, 0.16f, SecondFloorRearDepth), _concrete);
            CreateBox(_secondFloor, "SecondFloorPlate_FrontBedroomStairWing",
                new Vector3(SecondFloorFrontWingCenterX, SecondBaseY + 0.08f, SecondFloorFrontWingCenterZ),
                new Vector3(secondFloorFrontWingWidth, 0.16f, secondFloorFrontWingDepth), _concrete);

            float y = firstFloorHeight + secondFloorHeight * 0.5f;
            // The left bay's front wall sits forward on the projecting wing, so
            // the main-block front wall only spans the garage side here.
            float frontWallWidth = MainRightX - EntryRightX;
            CreateBox(_secondExterior, "SecondFloor_FrontWall_OverGarage",
                new Vector3((EntryRightX + MainRightX) * 0.5f, y, MainFrontZ - wallThickness * 0.5f),
                new Vector3(frontWallWidth, secondFloorHeight, wallThickness), _stucco);
            CreateBox(_secondExterior, "SecondFloor_RearWall",
                new Vector3(0f, y, MainRearZ + wallThickness * 0.5f),
                new Vector3(mainWidth, secondFloorHeight, wallThickness), _stucco);
            CreateBox(_secondExterior, "SecondFloor_RightExteriorWall",
                new Vector3(MainRightX + wallThickness * 0.5f, y, MainCenterZ),
                new Vector3(wallThickness, secondFloorHeight, mainDepth), _stuccoAccent);

            var cutLeft = CreateBox(_secondExterior, "SecondFloor_LeftExterior_CutawayWall",
                new Vector3(MainLeftX - wallThickness * 0.5f, y, MainCenterZ),
                new Vector3(wallThickness, secondFloorHeight, mainDepth), _stucco);
            _cutawayWalls.Add(cutLeft);

            BuildEntryWingSecondFloor();
            BuildSecondFloorOpenings();
            if (includeInteriorReference)
            {
                if (HasScannedUpstairs)
                {
                    PlaceScan(_secondInterior, "AsBuiltUpstairsScan", "UpstairsScanMesh",
                        scannedUpstairsMeshOverride, scannedUpstairsMaterialOverride,
                        scannedUpstairsPosition, scannedUpstairsRotation, scannedUpstairsScale);
                }
                else
                {
                    BuildSecondFloorInteriorReference();
                }
            }
        }

        // Upper story of the projecting front-left wing: a floor plate over the
        // bay, a cutaway left wall coplanar with the main left wall, a right
        // wall that rises above the garage roof, and a street-facing front wall
        // with a bedroom window. The back is open to the main second floor.
        void BuildEntryWingSecondFloor()
        {
            float y = firstFloorHeight + secondFloorHeight * 0.5f;

            CreateBox(_secondFloor, "SecondFloorPlate_EntryWing",
                new Vector3(EntryCenterX, SecondBaseY + 0.08f, EntryCenterZ),
                new Vector3(EntryWingWidth, 0.16f, entryWingDepth), _concrete);

            var wingLeft = CreateBox(_secondExterior, "SecondFloor_EntryWing_LeftWall_CutawayWall",
                new Vector3(MainLeftX - wallThickness * 0.5f, y, EntryCenterZ),
                new Vector3(wallThickness, secondFloorHeight, entryWingDepth), _stucco);
            _cutawayWalls.Add(wingLeft);

            CreateBox(_secondExterior, "SecondFloor_EntryWing_RightWall",
                new Vector3(EntryRightX + wallThickness * 0.5f, y, EntryCenterZ),
                new Vector3(wallThickness, secondFloorHeight, entryWingDepth), _stuccoAccent);

            CreateBox(_secondExterior, "SecondFloor_EntryWing_FrontWall",
                new Vector3(EntryCenterX, y, EntryFrontZ - wallThickness * 0.5f),
                new Vector3(EntryWingWidth, secondFloorHeight, wallThickness), _stucco);

            AddWindow(_windowsSecond, "EntryWingBedroomWindow", WallSide.Front,
                new Vector3(EntryCenterX, 12.7f, EntryFrontZ - wallThickness - 0.03f), 3.6f, 3.2f, true);
        }

        void BuildSecondFloorOpenings()
        {
            float frontFace = MainFrontZ - wallThickness - 0.03f;
            float rearFace = MainRearZ + wallThickness + 0.03f;

            // Street side: a single window on the right, above the garage.
            AddWindow(_windowsSecond, "FrontBedroomWindowAboveGarage", WallSide.Front,
                new Vector3(10.6f, 12.7f, frontFace), 3.6f, 3.2f, true);

            // Rear: three windows, aligned above the three sliding doors.
            AddWindow(_windowsSecond, "RearWindowLeft", WallSide.Rear,
                new Vector3(-9.6f, 12.7f, rearFace), 3.4f, 3.2f, true);
            AddWindow(_windowsSecond, "RearWindowCenter", WallSide.Rear,
                new Vector3(0f, 12.7f, rearFace), 3.4f, 3.2f, true);
            AddWindow(_windowsSecond, "RearWindowRight", WallSide.Rear,
                new Vector3(9.6f, 12.7f, rearFace), 3.4f, 3.2f, true);

            // No second-floor windows on either side elevation.
        }

        void BuildSecondFloorInteriorReference()
        {
            const float h = 3.1f;
            float baseY = SecondBaseY;
            float bedOneRightX = MainLeftX + 14.67f; // Plan B second-floor rear grid: 14'-8".
            float bathRightX = bedOneRightX + 5.5f; // 5'-6" bath bay.
            float rearBedroomFrontZ = SecondFloorRearFrontZ + 0.15f;
            float frontHallBackZ = SecondFloorRearFrontZ - 0.15f;
            float bedroomThreeLeftX = bathRightX;
            float stairRightX = SecondFloorFrontWingLeftX + 6.8f;

            // The appraisal sketch shows two bedrooms across the rear with the
            // full bath between them, plus a third bedroom at front-right.
            AddInteriorWall(_secondInterior, "BedroomOneBathDivider",
                new Vector3(bedOneRightX, baseY, rearBedroomFrontZ + 0.4f),
                new Vector3(bedOneRightX, baseY, MainRearZ - 0.5f), h);
            AddInteriorWall(_secondInterior, "BathBedroomTwoDivider",
                new Vector3(bathRightX, baseY, rearBedroomFrontZ + 0.4f),
                new Vector3(bathRightX, baseY, MainRearZ - 0.5f), h);
            AddInteriorWall(_secondInterior, "BedroomOneHallWall_LeftSegment",
                new Vector3(MainLeftX + 0.4f, baseY, rearBedroomFrontZ),
                new Vector3(bedOneRightX - 2.0f, baseY, rearBedroomFrontZ), h);
            AddInteriorWall(_secondInterior, "BedroomOneHallWall_DoorJamb",
                new Vector3(bedOneRightX - 0.6f, baseY, rearBedroomFrontZ),
                new Vector3(bedOneRightX + 0.35f, baseY, rearBedroomFrontZ), h);
            AddInteriorWall(_secondInterior, "BathHallWall",
                new Vector3(bedOneRightX + 0.45f, baseY, rearBedroomFrontZ),
                new Vector3(bathRightX - 0.45f, baseY, rearBedroomFrontZ), h);
            AddInteriorWall(_secondInterior, "BedroomTwoHallWall",
                new Vector3(bathRightX + 0.45f, baseY, rearBedroomFrontZ),
                new Vector3(MainRightX - 0.4f, baseY, rearBedroomFrontZ), h);

            // Stair landing, hall, linen/attic zone, and front-right bedroom 3.
            AddInteriorWall(_secondInterior, "StairwellRightWall",
                new Vector3(stairRightX, baseY, MainFrontZ + 0.4f),
                new Vector3(stairRightX, baseY, frontHallBackZ), h);
            AddInteriorWall(_secondInterior, "HallBedroomThreeDivider",
                new Vector3(bedroomThreeLeftX, baseY, MainFrontZ + 0.4f),
                new Vector3(bedroomThreeLeftX, baseY, frontHallBackZ), h);
            AddInteriorWall(_secondInterior, "BedroomThreeHallWall",
                new Vector3(bedroomThreeLeftX + 0.4f, baseY, MainFrontZ + 3.6f),
                new Vector3(MainRightX - 0.4f, baseY, MainFrontZ + 3.6f), h);
            AddInteriorWall(_secondInterior, "LinenClosetBackWall",
                new Vector3(stairRightX + 0.25f, baseY, MainFrontZ + 5.5f),
                new Vector3(bedroomThreeLeftX - 0.25f, baseY, MainFrontZ + 5.5f), h);
            AddInteriorWall(_secondInterior, "LinenClosetEastWall",
                new Vector3(bedroomThreeLeftX - 0.25f, baseY, MainFrontZ + 3.4f),
                new Vector3(bedroomThreeLeftX - 0.25f, baseY, MainFrontZ + 5.5f), h);

            CreateBox(_secondInterior, "UpstairsBathBlockReference",
                new Vector3((bedOneRightX + bathRightX) * 0.5f, baseY + 1f, MainRearZ - 4.8f),
                new Vector3(4.4f, 2f, 5.1f), _interiorWall);
            CreateBox(_secondInterior, "LinenClosetReference",
                new Vector3((stairRightX + bedroomThreeLeftX) * 0.5f, baseY + 1f, MainFrontZ + 4.45f),
                new Vector3(bedroomThreeLeftX - stairRightX - 0.5f, 2f, 1.7f), _interiorWall);
        }

        void BuildRoofs()
        {
            float mainRoofRise = (mainDepth * 0.5f + eaveOverhang) * roofPitchRisePerFoot;
            CreateGableRoof(_mainRoof, "MainTwoStoryGableRoofMesh",
                new Vector3(0f, 0f, MainCenterZ), mainWidth, mainDepth, MainRoofBaseY + 0.15f,
                mainRoofRise, eaveOverhang, RoofOrientation.RidgeAlongWidth, _roof);

            AddGableFascia("MainRoof", new Vector3(0f, 0f, MainCenterZ), mainWidth, mainDepth,
                MainRoofBaseY + 0.15f, mainRoofRise, eaveOverhang, RoofOrientation.RidgeAlongWidth);

            float garageRise = (garageWidth * 0.5f + eaveOverhang) * roofPitchRisePerFoot;
            CreateGableRoof(_garageRoof, "GarageFrontGableRoofMesh",
                new Vector3(GarageCenterX, 0f, GarageCenterZ), garageWidth, garageDepth, GarageRoofBaseY + 0.15f,
                garageRise, eaveOverhang, RoofOrientation.RidgeAlongDepth, _roof);

            AddGableFascia("GarageRoof", new Vector3(GarageCenterX, 0f, GarageCenterZ), garageWidth, garageDepth,
                GarageRoofBaseY + 0.15f, garageRise, eaveOverhang, RoofOrientation.RidgeAlongDepth);

            // The projecting front-left wing carries its own street-facing gable.
            // Its ridge runs front-to-back over the narrow 9 ft bay, so it peaks
            // well below the main ridge — the separate "shorter roof" the owner
            // pointed out (2026-07-12).
            float entryWingRise = (EntryWingWidth * 0.5f + eaveOverhang) * roofPitchRisePerFoot;
            CreateGableRoof(_entryWingRoof, "EntryWingFrontGableRoofMesh",
                new Vector3(EntryCenterX, 0f, EntryCenterZ), EntryWingWidth, entryWingDepth, MainRoofBaseY + 0.15f,
                entryWingRise, eaveOverhang, RoofOrientation.RidgeAlongDepth, _roof);

            AddGableFascia("EntryWingRoof", new Vector3(EntryCenterX, 0f, EntryCenterZ), EntryWingWidth, entryWingDepth,
                MainRoofBaseY + 0.15f, entryWingRise, eaveOverhang, RoofOrientation.RidgeAlongDepth);

            // The rear deck is fully open to the sky per the 2021 photos.
        }

        void BuildDetails()
        {
            BuildRailings();
            BuildTrimAndGutters();
            // The Scaniverse capture contains the real carpeted switchback
            // stair and railing. Keep the procedural version as the fallback.
            if (!HasScannedFirstFloor) BuildStairs();
        }

        void BuildRailings()
        {
            // White picket fence enclosing the front-left entry yard, with a
            // gate gap on the walkway axis (Google Street View).
            AddPicketFence(_railings, "FrontYardPicketFence",
                MainLeftX - 0.3f, GarageLeftX, GarageFrontZ + 1.0f, 3.0f,
                FrontDoorCenterX, 3.2f);
            // Rear deck railings are built with the deck in BuildRearElevatedDeck().
        }

        // A low white picket fence: evenly spaced pickets on a top rail between
        // two X extents at a fixed Z, with a gate gap and corner/gate posts.
        void AddPicketFence(GameObject parent, string name, float x0, float x1, float z, float height, float gateCenterX, float gateWidth)
        {
            var root = Group(name, parent);
            float gateL = gateCenterX - gateWidth * 0.5f;
            float gateR = gateCenterX + gateWidth * 0.5f;

            int i = 0;
            for (float x = x0 + 0.2f; x <= x1 - 0.15f; x += 0.5f)
            {
                if (x > gateL - 0.15f && x < gateR + 0.15f) continue;
                CreateBox(root, $"Picket_{i:00}",
                    new Vector3(x, height * 0.48f, z), new Vector3(0.12f, height * 0.96f, 0.06f), _trim);
                i++;
            }

            AddFenceRail(root, "RailLeftTop", x0, gateL, z, height * 0.82f);
            AddFenceRail(root, "RailLeftMid", x0, gateL, z, height * 0.34f);
            AddFenceRail(root, "RailRightTop", gateR, x1, z, height * 0.82f);
            AddFenceRail(root, "RailRightMid", gateR, x1, z, height * 0.34f);

            foreach (float px in new[] { x0, gateL, gateR, x1 })
                CreateBox(root, $"Post_{px:0.0}",
                    new Vector3(px, height * 0.5f + 0.2f, z), new Vector3(0.22f, height + 0.4f, 0.22f), _trim);
        }

        void AddFenceRail(GameObject root, string name, float xa, float xb, float z, float y)
        {
            if (xb - xa < 0.2f) return;
            CreateBox(root, name, new Vector3((xa + xb) * 0.5f, y, z),
                new Vector3(xb - xa, 0.12f, 0.06f), _trim);
        }

        void BuildTrimAndGutters()
        {
            float frontFace = MainFrontZ - wallThickness - 0.08f;
            float rearFace = MainRearZ + wallThickness + 0.08f;
            float garageFrontFace = GarageFrontZ - wallThickness - 0.08f;

            CreateBox(_trimGroup, "FrontSecondFloorBeltTrim",
                new Vector3(SecondFloorFrontWingCenterX, SecondBaseY + 0.08f, frontFace),
                new Vector3(secondFloorFrontWingWidth + 0.6f, 0.22f, 0.16f), _trim);
            CreateBox(_trimGroup, "RearSecondFloorBeltTrim",
                new Vector3(0f, SecondBaseY + 0.08f, rearFace),
                new Vector3(mainWidth + 0.6f, 0.22f, 0.16f), _trim);
            CreateBox(_trimGroup, "GarageDoorHeaderTrim",
                new Vector3(GarageCenterX, 7.05f, garageFrontFace),
                new Vector3(17.0f, 0.28f, 0.16f), _trim);

            // Subtle diagonal gable trim echoes guide-like lines visible in the scan.
            float rise = (garageWidth * 0.5f + eaveOverhang) * roofPitchRisePerFoot;
            Vector3 garageEaveL = new Vector3(GarageCenterX - garageWidth * 0.5f - eaveOverhang * 0.65f, GarageRoofBaseY + 0.28f, GarageFrontZ - eaveOverhang - 0.12f);
            Vector3 garageEaveR = new Vector3(GarageCenterX + garageWidth * 0.5f + eaveOverhang * 0.65f, GarageRoofBaseY + 0.28f, GarageFrontZ - eaveOverhang - 0.12f);
            Vector3 garageRidge = new Vector3(GarageCenterX, GarageRoofBaseY + 0.15f + rise - 0.18f, GarageFrontZ - eaveOverhang - 0.12f);
            CreateBeamBetween(_trimGroup, "GarageFrontGableLeftRakeTrim", garageEaveL, garageRidge, 0.16f, 0.18f, _trim);
            CreateBeamBetween(_trimGroup, "GarageFrontGableRightRakeTrim", garageRidge, garageEaveR, 0.16f, 0.18f, _trim);

            CreateBox(_gutters, "MainRearGutter",
                new Vector3(0f, MainRoofBaseY + 0.02f, MainRearZ + eaveOverhang + 0.15f),
                new Vector3(mainWidth + 2f * eaveOverhang, 0.18f, 0.18f), _darkTrim);
            CreateBox(_gutters, "MainFrontGutter",
                new Vector3(0f, MainRoofBaseY + 0.02f, MainFrontZ - eaveOverhang - 0.15f),
                new Vector3(mainWidth + 2f * eaveOverhang, 0.18f, 0.18f), _darkTrim);
            CreateBox(_gutters, "GarageFrontGutter",
                new Vector3(GarageCenterX, GarageRoofBaseY + 0.02f, GarageFrontZ - eaveOverhang - 0.15f),
                new Vector3(garageWidth + 2f * eaveOverhang, 0.18f, 0.18f), _darkTrim);
        }

        void BuildStairs()
        {
            const int risersPerFlight = 7;
            const float treadDepth = 0.68f;
            const float treadThickness = 0.18f;
            const float flightWidth = 3.0f;
            float rise = firstFloorHeight / (risersPerFlight * 2f);
            float lowerFlightX = EntryRightX - 2.35f;
            float upperFlightX = lowerFlightX - 3.45f;
            float lowerStartZ = MainFrontZ - 0.95f;
            float turnLandingZ = lowerStartZ - risersPerFlight * treadDepth - 0.25f;

            // The lower flight begins just inside the open room and climbs
            // toward the front entry. It then turns 180 degrees at mid-height.
            for (int i = 0; i < risersPerFlight; i++)
            {
                float stepZ = lowerStartZ - i * treadDepth;
                float stepTopY = (i + 1) * rise;
                CreateBox(_stairs, $"LowerFlightTread_{i + 1:00}",
                    new Vector3(lowerFlightX, stepTopY - treadThickness * 0.5f, stepZ),
                    new Vector3(flightWidth, treadThickness, treadDepth + 0.06f), _interiorWall);
                CreateBox(_stairs, $"LowerFlightRiser_{i + 1:00}",
                    new Vector3(lowerFlightX, stepTopY - rise * 0.5f, stepZ + treadDepth * 0.48f),
                    new Vector3(flightWidth, rise, 0.08f), _interiorWall);
            }

            CreateBox(_stairs, "TurningLandingReference",
                new Vector3((lowerFlightX + upperFlightX) * 0.5f, firstFloorHeight * 0.5f - treadThickness * 0.5f, turnLandingZ),
                new Vector3(Mathf.Abs(lowerFlightX - upperFlightX) + flightWidth, treadThickness, 1.45f), _interiorWall);

            // The upper flight returns toward the house and meets a landing at
            // the second-floor hall, producing the corrected switchback stair.
            float upperStartZ = turnLandingZ + 0.45f;
            for (int i = 0; i < risersPerFlight; i++)
            {
                float stepZ = upperStartZ + i * treadDepth;
                float stepTopY = firstFloorHeight * 0.5f + (i + 1) * rise;
                CreateBox(_stairs, $"UpperFlightTread_{i + 1:00}",
                    new Vector3(upperFlightX, stepTopY - treadThickness * 0.5f, stepZ),
                    new Vector3(flightWidth, treadThickness, treadDepth + 0.06f), _interiorWall);
                CreateBox(_stairs, $"UpperFlightRiser_{i + 1:00}",
                    new Vector3(upperFlightX, stepTopY - rise * 0.5f, stepZ - treadDepth * 0.48f),
                    new Vector3(flightWidth, rise, 0.08f), _interiorWall);
            }

            CreateBox(_stairs, "SecondFloorLandingReference",
                new Vector3(upperFlightX, firstFloorHeight - treadThickness * 0.5f, MainFrontZ - 0.25f),
                new Vector3(flightWidth + 0.2f, treadThickness, 1.9f), _interiorWall);
            float hallBridgeLeftX = upperFlightX + flightWidth * 0.45f;
            float hallBridgeRightX = SecondFloorFrontWingLeftX + 0.8f;
            CreateBox(_stairs, "SecondFloorHallBridgeReference",
                new Vector3((hallBridgeLeftX + hallBridgeRightX) * 0.5f, firstFloorHeight - treadThickness * 0.5f, MainFrontZ + 0.25f),
                new Vector3(hallBridgeRightX - hallBridgeLeftX, treadThickness, 1.15f), _interiorWall);

            AddStairRail(_stairs, "LowerFlightOpenRail",
                new Vector3(lowerFlightX - flightWidth * 0.5f, 0f, lowerStartZ + 0.25f),
                new Vector3(lowerFlightX - flightWidth * 0.5f, firstFloorHeight * 0.5f, turnLandingZ + 0.3f), 2.55f);
            AddStairRail(_stairs, "UpperFlightOpenRail",
                new Vector3(upperFlightX + flightWidth * 0.5f, firstFloorHeight * 0.5f, upperStartZ - 0.2f),
                new Vector3(upperFlightX + flightWidth * 0.5f, firstFloorHeight, MainFrontZ - 0.7f), 2.55f);
        }

        void BuildSite()
        {
            float overallCenterZ = (GarageFrontZ + MainRearZ) * 0.5f;
            float overallDepth = MainRearZ - GarageFrontZ;

            // Upslope pad: street/front yard through just past the rear wall.
            float frontPadRearZ = MainRearZ + 2.0f;
            float frontPadFrontZ = GarageFrontZ - 34f;
            CreateBox(_site, "FrontGroundPad",
                new Vector3(0f, -0.26f, (frontPadFrontZ + frontPadRearZ) * 0.5f),
                new Vector3(86f, 0.08f, frontPadRearZ - frontPadFrontZ), _ground);

            // The lot falls away behind the house (soils report: ~2:1 slope
            // descending toward the rear). Slope wedge + lower rear pad.
            float slopeRunZ = rearGradeDrop * 2.2f;
            float slopeStartZ = frontPadRearZ;
            float slopeEndZ = slopeStartZ + slopeRunZ;
            float slopeLength = Mathf.Sqrt(slopeRunZ * slopeRunZ + rearGradeDrop * rearGradeDrop);
            var slope = CreateBox(_site, "RearSlopeBank",
                new Vector3(0f, -0.26f - rearGradeDrop * 0.5f, (slopeStartZ + slopeEndZ) * 0.5f),
                new Vector3(86f, 0.08f, slopeLength), _ground);
            slope.transform.localRotation = Quaternion.Euler(Mathf.Atan2(rearGradeDrop, slopeRunZ) * Mathf.Rad2Deg, 0f, 0f);

            CreateBox(_site, "RearLowerGroundPad",
                new Vector3(0f, RearLowerGroundY - 0.3f, slopeEndZ + 16f),
                new Vector3(86f, 0.08f, 34f), _ground);

            // Interlocking-paver driveway in front of the garage (right side).
            CreateBox(_site, "PaverDriveway",
                new Vector3(GarageCenterX, -0.19f, GarageFrontZ - 14.0f),
                new Vector3(18.8f, 0.08f, 28f), _driveway);
            // Walkway from the projecting entry stoop out to the picket-fence gate.
            float walkGateZ = GarageFrontZ + 1.0f;
            float walkStoopZ = EntryFrontZ - 2.6f;
            CreateBox(_site, "Walkway",
                new Vector3(FrontDoorCenterX, -0.17f, (walkGateZ + walkStoopZ) * 0.5f),
                new Vector3(3.2f, 0.08f, walkStoopZ - walkGateZ), _concrete);

            // Bright artificial-turf lawn left of the driveway (2021 photos).
            float lawnRightX = GarageCenterX - 9.4f - 0.2f;
            float lawnLeftX = -20f;
            CreateBox(_site, "FrontLawnTurf",
                new Vector3((lawnLeftX + lawnRightX) * 0.5f, -0.2f, GarageFrontZ - 15.5f),
                new Vector3(lawnRightX - lawnLeftX, 0.09f, 25f), _lawn);

            // Curbside mailbox on the lawn side.
            CreateBox(_site, "MailboxPost",
                new Vector3(-16.5f, 1.15f, GarageFrontZ - 26.5f), new Vector3(0.35f, 2.9f, 0.35f), _trim);
            CreateBox(_site, "MailboxBox",
                new Vector3(-16.5f, 2.75f, GarageFrontZ - 26.4f), new Vector3(0.7f, 0.75f, 1.5f), _darkTrim);

            // Lot 20 is 40 ft wide at the street (title plot map).
            CreateBox(_site, "LotBoundary_Front",
                new Vector3(0f, -0.1f, GarageFrontZ - 29f), new Vector3(40f, 0.08f, 0.18f), _darkTrim);
            CreateBox(_site, "LotBoundary_Left",
                new Vector3(-20f, -0.1f, overallCenterZ), new Vector3(0.18f, 0.08f, overallDepth + 42f), _darkTrim);
            CreateBox(_site, "LotBoundary_Right",
                new Vector3(20f, -0.1f, overallCenterZ), new Vector3(0.18f, 0.08f, overallDepth + 42f), _darkTrim);
        }

        // Photo-matched interior finishes, shown when the as-built scan is
        // hidden: warm hardwood floors, painted walls, a granite/wood kitchen,
        // and a speckled epoxy garage floor. Hidden by default while any scan is
        // present; the controller's scan toggle swaps between the two.
        void BuildProceduralFinishes()
        {
            CreateBox(_finishes, "HardwoodFloor",
                new Vector3(0f, 0.06f, MainCenterZ),
                new Vector3(mainWidth - 0.9f, 0.12f, mainDepth - 0.9f), _hardwood);
            CreateBox(_finishes, "GarageEpoxyFloor",
                new Vector3(GarageCenterX, 0.06f, GarageCenterZ),
                new Vector3(garageWidth - 0.6f, 0.12f, garageDepth - 0.6f), _epoxy);

            BuildInteriorWallPaint();
            BuildKitchen();

            if (HasScannedFirstFloor || HasScannedGarage || HasScannedUpstairs)
            {
                _finishes.SetActive(false);
            }
        }

        // Thin white liners on the inside faces of the first-floor perimeter,
        // so interior walls read as painted drywall like the scan.
        void BuildInteriorWallPaint()
        {
            float y = firstFloorHeight * 0.5f;
            float t = 0.06f;
            float w = mainWidth - 0.6f;
            float d = mainDepth - 0.6f;
            float h = firstFloorHeight - 0.3f;
            CreateBox(_finishes, "Paint_Rear", new Vector3(0f, y, MainRearZ - t * 0.5f), new Vector3(w, h, t), _wallPaint);
            CreateBox(_finishes, "Paint_Front", new Vector3(0f, y, MainFrontZ + t * 0.5f), new Vector3(w, h, t), _wallPaint);
            CreateBox(_finishes, "Paint_Left", new Vector3(MainLeftX + t * 0.5f, y, MainCenterZ), new Vector3(t, h, d), _wallPaint);
            CreateBox(_finishes, "Paint_Right", new Vector3(MainRightX - t * 0.5f, y, MainCenterZ), new Vector3(t, h, d), _wallPaint);
        }

        // A granite-and-wood kitchen against the right wall with an island,
        // matching the scan photos (island, perimeter counters, appliances).
        void BuildKitchen()
        {
            var k = Group("Kitchen", _finishes);
            const float baseH = 2.9f;   // cabinet box height
            const float topT = 0.18f;   // granite thickness
            const float counterD = 2.1f;
            float rightX = MainRightX - 0.15f - counterD * 0.5f;

            // Perimeter counter run along the right wall.
            float runFrontZ = 3.0f, runRearZ = 13.5f;
            AddCounter(k, "RightWallCounter",
                new Vector3(rightX, 0f, (runFrontZ + runRearZ) * 0.5f),
                new Vector3(counterD, baseH, runRearZ - runFrontZ), baseH, topT);
            // Return counter along the rear-right, running in X.
            float backCounterRightX = rightX - counterD * 0.5f;
            float backCounterLeftX = 4.5f;
            AddCounter(k, "BackCounter",
                new Vector3((backCounterLeftX + backCounterRightX) * 0.5f, 0f, runRearZ - counterD * 0.5f),
                new Vector3(backCounterRightX - backCounterLeftX, baseH, counterD), baseH, topT);

            // Wall (upper) cabinets above the right-wall counter.
            CreateBox(k, "UpperCabinets_Right",
                new Vector3(MainRightX - 0.15f - 0.65f, 5.4f, (runFrontZ + runRearZ) * 0.5f),
                new Vector3(1.3f, 2.4f, runRearZ - runFrontZ - 1.0f), _cabinet);

            // Island: wood base + granite top.
            Vector3 islandCenter = new Vector3(7.0f, 0f, 7.5f);
            Vector3 islandSize = new Vector3(6.4f, baseH, 3.0f);
            AddCounter(k, "Island", islandCenter, islandSize, baseH, topT);

            // Appliances (brushed stainless boxes).
            // Range against the right wall.
            AddAppliance(k, "Range", new Vector3(rightX, baseH * 0.5f, 6.0f), new Vector3(counterD - 0.2f, baseH, 2.6f));
            CreateBox(k, "RangeHood", new Vector3(rightX, 6.6f, 6.0f), new Vector3(counterD - 0.4f, 0.9f, 2.2f), _appliance);
            // Tall refrigerator at the back-right corner.
            AddAppliance(k, "Refrigerator", new Vector3(backCounterLeftX + 1.4f, 3.4f, runRearZ - 1.5f), new Vector3(2.8f, 6.8f, 2.6f));
            // Dishwasher in the right run.
            AddAppliance(k, "Dishwasher", new Vector3(rightX, baseH * 0.5f, 10.5f), new Vector3(counterD - 0.3f, baseH - 0.2f, 2.0f));
            // Sink cut into the island (dark inset).
            CreateBox(k, "IslandSink", new Vector3(6.0f, baseH + topT - 0.05f, 7.5f), new Vector3(1.6f, 0.14f, 1.1f), _darkTrim);
        }

        // A base cabinet (wood) capped with a granite top slab.
        void AddCounter(GameObject parent, string name, Vector3 baseCenterFloor, Vector3 size, float baseH, float topT)
        {
            var g = Group(name, parent);
            CreateBox(g, "Base", new Vector3(baseCenterFloor.x, baseH * 0.5f, baseCenterFloor.z),
                new Vector3(size.x, baseH - topT, size.z), _cabinet);
            CreateBox(g, "GraniteTop", new Vector3(baseCenterFloor.x, baseH + topT * 0.5f - 0.02f, baseCenterFloor.z),
                new Vector3(size.x + 0.12f, topT, size.z + 0.12f), _granite);
        }

        // A stainless appliance box with a slightly recessed darker front.
        void AddAppliance(GameObject parent, string name, Vector3 center, Vector3 size)
        {
            var g = Group(name, parent);
            CreateBox(g, "Body", center, size, _appliance);
            CreateBox(g, "Front", center - new Vector3(0f, 0f, size.z * 0.5f + 0.02f),
                new Vector3(size.x * 0.82f, size.y * 0.8f, 0.06f), _darkTrim);
        }

        // A 4-person hot tub (redwood surround, acrylic shell, water surface)
        // sitting on the rear deck.
        void AddHotTub(GameObject parent, Vector3 deckTopCenter)
        {
            var g = Group("HotTub_4Person", parent);
            const float tubW = 7.0f, tubD = 7.0f, tubH = 2.9f;
            float baseY = deckTopCenter.y;
            // Redwood surround skirt.
            CreateBox(g, "Surround", new Vector3(deckTopCenter.x, baseY + tubH * 0.5f, deckTopCenter.z),
                new Vector3(tubW, tubH, tubD), _deck);
            // Acrylic inner shell (recessed).
            CreateBox(g, "Shell", new Vector3(deckTopCenter.x, baseY + tubH * 0.5f + 0.2f, deckTopCenter.z),
                new Vector3(tubW - 1.0f, tubH - 0.3f, tubD - 1.0f), _appliance);
            // Water surface just below the rim.
            CreateBox(g, "Water", new Vector3(deckTopCenter.x, baseY + tubH - 0.35f, deckTopCenter.z),
                new Vector3(tubW - 1.3f, 0.12f, tubD - 1.3f), _water);
            // Bench step.
            CreateBox(g, "Step", new Vector3(deckTopCenter.x - tubW * 0.5f - 0.7f, baseY + 0.6f, deckTopCenter.z),
                new Vector3(1.6f, 1.2f, 2.6f), _deck);
        }

        void BuildCamerasAndLighting()
        {
            var cameraGo = new GameObject("MainPerspectiveCamera");
            cameraGo.transform.SetParent(_cameras.transform, false);
            var cam = cameraGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.74f, 0.79f, 0.82f);
            cam.fieldOfView = 42f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 180f;
            if (Camera.main == null)
            {
                cameraGo.tag = "MainCamera";
            }
            var orbit = cameraGo.AddComponent<CameraOrbitController>();
            orbit.target = new Vector3(0f, 8.2f, (GarageFrontZ + MainRearZ) * 0.5f);
            orbit.distance = 62f;
            orbit.minDistance = 18f;
            orbit.maxDistance = 105f;
            orbit.yaw = 0f; // Start on the street side; yaw is unclamped for full orbit.
            orbit.pitch = 16f;
            orbit.minYaw = -100000f;
            orbit.maxYaw = 100000f;
            orbit.minPitch = 8f;
            orbit.maxPitch = 78f;
            orbit.walkSpeed = 10.5f;
            orbit.minWalkY = 1.2f;
            orbit.maxWalkY = MainRoofBaseY + 6f;
            orbit.SetWalkStart(new Vector3(EntryCenterX, 5.2f, EntryFrontZ + 5.2f), 0f, 3f);

            var topGo = new GameObject("TopDownPlanCamera");
            topGo.transform.SetParent(_cameras.transform, false);
            topGo.transform.localPosition = new Vector3(0f, 72f, 0f);
            topGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var topCam = topGo.AddComponent<Camera>();
            topCam.orthographic = true;
            topCam.orthographicSize = 35f;
            topCam.nearClipPlane = 0.1f;
            topCam.farClipPlane = 150f;
            topCam.enabled = false;

            var sunGo = new GameObject("DirectionalSun");
            sunGo.transform.SetParent(_lighting.transform, false);
            sunGo.transform.localRotation = Quaternion.Euler(48f, -32f, 0f);
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.96f, 0.86f);
            sun.intensity = 1.05f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.45f;

            var fillGo = new GameObject("SoftPorchFillLight");
            fillGo.transform.SetParent(_lighting.transform, false);
            fillGo.transform.localPosition = new Vector3(7.5f, 5.5f, MainFrontZ - 5f);
            var fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Point;
            fill.color = new Color(1f, 0.88f, 0.7f);
            fill.intensity = 0.45f;
            fill.range = 18f;
            fill.shadows = LightShadows.None;

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.58f, 0.58f, 0.55f);
        }

        void BindController()
        {
            var controller = GetComponent<HouseModelController>();
            if (controller == null)
            {
                controller = gameObject.AddComponent<HouseModelController>();
            }

            var interiorGroups = new[] { _firstInterior, _secondInterior, _stairs };
            var exteriorGroups = new[]
            {
                _firstExterior, _garage, _doorsFirst, _windowsFirst,
                _secondExterior, _windowsSecond, _roofs, _trimGroup, _gutters, _railings
            };

            var scanGroupList = new List<GameObject>();
            foreach (var path in new[]
            {
                "FirstFloor/InteriorReferenceWalls/AsBuiltFirstFloorScan",
                "FirstFloor/Garage/AsBuiltGarageScan",
                "SecondFloor/InteriorReferenceWalls/AsBuiltUpstairsScan"
            })
            {
                var t = transform.Find(path);
                if (t != null) scanGroupList.Add(t.gameObject);
            }

            Camera perspective = null;
            Camera topDown = null;
            if (_cameras != null)
            {
                var perspectiveTransform = _cameras.transform.Find("MainPerspectiveCamera");
                var topTransform = _cameras.transform.Find("TopDownPlanCamera");
                if (perspectiveTransform != null) perspective = perspectiveTransform.GetComponent<Camera>();
                if (topTransform != null) topDown = topTransform.GetComponent<Camera>();
            }

            controller.Bind(_roofs, _secondFloor, _firstFloor, interiorGroups, exteriorGroups, _cutawayWalls.ToArray(), scanGroupList.ToArray(), _finishes, perspective, topDown);
        }

        void AddGarageDoor(string name, Vector3 center, float width, float height)
        {
            var root = Group(name, _doorsFirst);
            CreateBox(root, "DoorPanel",
                center, new Vector3(width, height, 0.16f), _garageDoor);

            int verticalPanels = 4;
            for (int i = 1; i < verticalPanels; i++)
            {
                float x = center.x - width * 0.5f + width * i / verticalPanels;
                CreateBox(root, $"VerticalGroove_{i}",
                    new Vector3(x, center.y, center.z - 0.09f), new Vector3(0.08f, height - 0.45f, 0.08f), _darkTrim);
            }

            for (int row = 1; row < 4; row++)
            {
                float y = center.y - height * 0.5f + height * row / 4f;
                CreateBox(root, $"HorizontalPanelGroove_{row}",
                    new Vector3(center.x, y, center.z - 0.09f), new Vector3(width - 0.45f, 0.08f, 0.08f), _darkTrim);
            }

            for (int col = 0; col < verticalPanels; col++)
            {
                for (int row = 0; row < 4; row++)
                {
                    float x = center.x - width * 0.375f + width * col / verticalPanels;
                    float y = center.y - height * 0.31f + height * row / 4f;
                    bool topLiteRow = row == 3; // The real door has a window row across the top.
                    CreateBox(root, topLiteRow ? $"TopWindowLite_{col + 1}" : $"RecessedPanel_{col + 1}_{row + 1}",
                        new Vector3(x, y, center.z - 0.105f),
                        new Vector3(width / verticalPanels - 0.75f, topLiteRow ? 0.9f : 0.65f, 0.055f),
                        topLiteRow ? _glass : _trim);
                }
            }
        }

        void AddSwingDoor(GameObject parent, string name, WallSide side, Vector3 center, float width, float height, Material material)
        {
            var root = Group(name, parent);
            Vector3 panelScale = IsFrontRear(side)
                ? new Vector3(width, height, 0.14f)
                : new Vector3(0.14f, height, width);
            CreateBox(root, "DoorPanel", center, panelScale, material);
            AddRectTrim(root, "DoorTrim", side, center, width + 0.35f, height + 0.35f, 0.16f, _trim);

            Vector3 handlePos = center;
            if (side == WallSide.Front) handlePos += new Vector3(width * 0.32f, 0.25f, -0.12f);
            else if (side == WallSide.Rear) handlePos += new Vector3(-width * 0.32f, 0.25f, 0.12f);
            else if (side == WallSide.Left) handlePos += new Vector3(-0.12f, 0.25f, width * 0.32f);
            else handlePos += new Vector3(0.12f, 0.25f, -width * 0.32f);

            Vector3 handleScale = IsFrontRear(side) ? new Vector3(0.14f, 0.14f, 0.08f) : new Vector3(0.08f, 0.14f, 0.14f);
            CreateBox(root, "DoorHandle", handlePos, handleScale, _darkTrim);
        }

        void AddSlidingDoor(GameObject parent, string name, WallSide side, Vector3 center, float width, float height)
        {
            var root = Group(name, parent);
            float paneWidth = width * 0.48f;
            if (IsFrontRear(side))
            {
                CreateBox(root, "SlidingGlassPaneLeft", center + new Vector3(-paneWidth * 0.5f, 0f, 0f),
                    new Vector3(paneWidth, height, 0.1f), _glass);
                CreateBox(root, "SlidingGlassPaneRight", center + new Vector3(paneWidth * 0.5f, 0f, 0f),
                    new Vector3(paneWidth, height, 0.1f), _glass);
            }
            else
            {
                CreateBox(root, "SlidingGlassPaneLeft", center + new Vector3(0f, 0f, -paneWidth * 0.5f),
                    new Vector3(0.1f, height, paneWidth), _glass);
                CreateBox(root, "SlidingGlassPaneRight", center + new Vector3(0f, 0f, paneWidth * 0.5f),
                    new Vector3(0.1f, height, paneWidth), _glass);
            }
            AddRectTrim(root, "SlidingDoorTrim", side, center, width + 0.45f, height + 0.35f, 0.18f, _trim);
            AddMullion(root, "SlidingDoorCenterRail", side, center, 0.18f, height, _trim);
        }

        void AddWindow(GameObject parent, string name, WallSide side, Vector3 center, float width, float height, bool mullions)
        {
            var root = Group(name, parent);
            Vector3 paneScale = IsFrontRear(side)
                ? new Vector3(width, height, 0.1f)
                : new Vector3(0.1f, height, width);
            CreateBox(root, "GlassPane", center, paneScale, _glass);
            AddRectTrim(root, "WindowTrim", side, center, width + 0.35f, height + 0.35f, 0.16f, _trim);

            if (!mullions) return;
            AddMullion(root, "VerticalMullion", side, center, 0.1f, height, _trim);
            AddHorizontalMullion(root, "HorizontalMullion", side, center, width, 0.1f, _trim);
        }

        void AddRectTrim(GameObject parent, string name, WallSide side, Vector3 center, float width, float height, float trim, Material mat)
        {
            var root = Group(name, parent);
            float sideOffset = TrimDepth * 0.55f;
            Vector3 zForward = side == WallSide.Front ? Vector3.back : side == WallSide.Rear ? Vector3.forward : Vector3.zero;
            Vector3 xForward = side == WallSide.Left ? Vector3.left : side == WallSide.Right ? Vector3.right : Vector3.zero;
            Vector3 offset = (zForward + xForward) * sideOffset;

            if (IsFrontRear(side))
            {
                CreateBox(root, "Top", center + offset + new Vector3(0f, height * 0.5f, 0f), new Vector3(width, trim, TrimDepth), mat);
                CreateBox(root, "Bottom", center + offset - new Vector3(0f, height * 0.5f, 0f), new Vector3(width, trim, TrimDepth), mat);
                CreateBox(root, "Left", center + offset - new Vector3(width * 0.5f, 0f, 0f), new Vector3(trim, height, TrimDepth), mat);
                CreateBox(root, "Right", center + offset + new Vector3(width * 0.5f, 0f, 0f), new Vector3(trim, height, TrimDepth), mat);
            }
            else
            {
                CreateBox(root, "Top", center + offset + new Vector3(0f, height * 0.5f, 0f), new Vector3(TrimDepth, trim, width), mat);
                CreateBox(root, "Bottom", center + offset - new Vector3(0f, height * 0.5f, 0f), new Vector3(TrimDepth, trim, width), mat);
                CreateBox(root, "Left", center + offset - new Vector3(0f, 0f, width * 0.5f), new Vector3(TrimDepth, height, trim), mat);
                CreateBox(root, "Right", center + offset + new Vector3(0f, 0f, width * 0.5f), new Vector3(TrimDepth, height, trim), mat);
            }
        }

        void AddMullion(GameObject parent, string name, WallSide side, Vector3 center, float thickness, float height, Material mat)
        {
            if (IsFrontRear(side))
            {
                CreateBox(parent, name, center + MullionSurfaceOffset(side), new Vector3(thickness, height, TrimDepth), mat);
            }
            else
            {
                CreateBox(parent, name, center + MullionSurfaceOffset(side), new Vector3(TrimDepth, height, thickness), mat);
            }
        }

        void AddHorizontalMullion(GameObject parent, string name, WallSide side, Vector3 center, float width, float thickness, Material mat)
        {
            if (IsFrontRear(side))
            {
                CreateBox(parent, name, center + MullionSurfaceOffset(side), new Vector3(width, thickness, TrimDepth), mat);
            }
            else
            {
                CreateBox(parent, name, center + MullionSurfaceOffset(side), new Vector3(TrimDepth, thickness, width), mat);
            }
        }

        Vector3 MullionSurfaceOffset(WallSide side)
        {
            return side switch
            {
                WallSide.Front => new Vector3(0f, 0f, -0.11f),
                WallSide.Rear => new Vector3(0f, 0f, 0.11f),
                WallSide.Left => new Vector3(-0.11f, 0f, 0f),
                _ => new Vector3(0.11f, 0f, 0f)
            };
        }

        bool IsFrontRear(WallSide side)
        {
            return side == WallSide.Front || side == WallSide.Rear;
        }

        void AddInteriorWall(GameObject parent, string name, Vector3 start, Vector3 end, float height)
        {
            Vector3 mid = (start + end) * 0.5f;
            mid.y += height * 0.5f;
            float dx = Mathf.Abs(end.x - start.x);
            float dz = Mathf.Abs(end.z - start.z);
            Vector3 scale = dx >= dz
                ? new Vector3(dx, height, 0.22f)
                : new Vector3(0.22f, height, dz);
            CreateBox(parent, name, mid, scale, _interiorWall);
        }

        void AddRailing(GameObject parent, string name, Vector3 start, Vector3 end, float height)
        {
            var root = Group(name, parent);
            Vector3 delta = end - start;
            float length = new Vector2(delta.x, delta.z).magnitude;
            int intervals = Mathf.Max(1, Mathf.CeilToInt(length / 3f));

            for (int i = 0; i <= intervals; i++)
            {
                float t = i / (float)intervals;
                Vector3 p = Vector3.Lerp(start, end, t);
                CreateBox(root, $"Post_{i + 1:00}", new Vector3(p.x, p.y + height * 0.5f, p.z), new Vector3(0.18f, height, 0.18f), _railing);
            }

            Vector3 mid = (start + end) * 0.5f;
            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.z))
            {
                CreateBox(root, "TopRail", new Vector3(mid.x, start.y + height, mid.z), new Vector3(length, 0.16f, 0.16f), _railing);
                CreateBox(root, "MidRail", new Vector3(mid.x, start.y + height * 0.55f, mid.z), new Vector3(length, 0.12f, 0.12f), _railing);
            }
            else
            {
                CreateBox(root, "TopRail", new Vector3(mid.x, start.y + height, mid.z), new Vector3(0.16f, 0.16f, length), _railing);
                CreateBox(root, "MidRail", new Vector3(mid.x, start.y + height * 0.55f, mid.z), new Vector3(0.12f, 0.12f, length), _railing);
            }
        }

        void CreateGableRoof(GameObject parent, string name, Vector3 center, float width, float depth, float baseHeight, float rise, float overhang, RoofOrientation orientation, Material material)
        {
            float roofWidth = width + overhang * 2f;
            float roofDepth = depth + overhang * 2f;

            Vector3[] vertices;
            int[] triangles;
            Vector2[] uvs;

            if (orientation == RoofOrientation.RidgeAlongDepth)
            {
                float xL = center.x - roofWidth * 0.5f;
                float xR = center.x + roofWidth * 0.5f;
                float zF = center.z - roofDepth * 0.5f;
                float zR = center.z + roofDepth * 0.5f;
                float ridgeY = baseHeight + rise;

                vertices = new[]
                {
                    new Vector3(xL, baseHeight, zF), new Vector3(xR, baseHeight, zF),
                    new Vector3(xL, baseHeight, zR), new Vector3(xR, baseHeight, zR),
                    new Vector3(center.x, ridgeY, zF), new Vector3(center.x, ridgeY, zR)
                };
                triangles = new[]
                {
                    0, 2, 5, 0, 5, 4,
                    1, 5, 3, 1, 4, 5,
                    0, 4, 1,
                    2, 3, 5
                };
                uvs = new[]
                {
                    new Vector2(0f, 0f), new Vector2(1f, 0f),
                    new Vector2(0f, roofDepth * 0.18f), new Vector2(1f, roofDepth * 0.18f),
                    new Vector2(0.5f, 0f), new Vector2(0.5f, roofDepth * 0.18f)
                };
            }
            else
            {
                float xL = center.x - roofWidth * 0.5f;
                float xR = center.x + roofWidth * 0.5f;
                float zF = center.z - roofDepth * 0.5f;
                float zR = center.z + roofDepth * 0.5f;
                float ridgeY = baseHeight + rise;

                vertices = new[]
                {
                    new Vector3(xL, baseHeight, zF), new Vector3(xL, baseHeight, zR),
                    new Vector3(xR, baseHeight, zF), new Vector3(xR, baseHeight, zR),
                    new Vector3(xL, ridgeY, center.z), new Vector3(xR, ridgeY, center.z)
                };
                triangles = new[]
                {
                    0, 4, 5, 0, 5, 2,
                    1, 3, 5, 1, 5, 4,
                    0, 1, 4,
                    2, 5, 3
                };
                uvs = new[]
                {
                    new Vector2(0f, 0f), new Vector2(1f, 0f),
                    new Vector2(0f, roofWidth * 0.18f), new Vector2(1f, roofWidth * 0.18f),
                    new Vector2(0.5f, 0f), new Vector2(0.5f, roofWidth * 0.18f)
                };
            }

            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var mesh = new Mesh { name = name };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        void CreateShedRoof(GameObject parent, string name, Vector3 center, float width, float depth, float lowY, float highY, Material material)
        {
            float xL = center.x - width * 0.5f;
            float xR = center.x + width * 0.5f;
            float zF = center.z - depth * 0.5f;
            float zR = center.z + depth * 0.5f;

            var vertices = new[]
            {
                new Vector3(xL, highY, zF), new Vector3(xR, highY, zF),
                new Vector3(xL, lowY, zR), new Vector3(xR, lowY, zR)
            };
            var triangles = new[] { 0, 2, 3, 0, 3, 1 };
            var uvs = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };

            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var mesh = new Mesh { name = name };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        void AddGableFascia(string prefix, Vector3 center, float width, float depth, float baseHeight, float rise, float overhang, RoofOrientation orientation)
        {
            float roofWidth = width + overhang * 2f;
            float roofDepth = depth + overhang * 2f;

            if (orientation == RoofOrientation.RidgeAlongDepth)
            {
                float xL = center.x - roofWidth * 0.5f;
                float xR = center.x + roofWidth * 0.5f;
                float zF = center.z - roofDepth * 0.5f;
                float zR = center.z + roofDepth * 0.5f;
                float ridgeY = baseHeight + rise;
                CreateBox(_eavesAndFascia, $"{prefix}_LeftEaveFascia", new Vector3(xL, baseHeight - 0.12f, center.z), new Vector3(0.22f, 0.36f, roofDepth), _fascia);
                CreateBox(_eavesAndFascia, $"{prefix}_RightEaveFascia", new Vector3(xR, baseHeight - 0.12f, center.z), new Vector3(0.22f, 0.36f, roofDepth), _fascia);
                CreateBox(_eavesAndFascia, $"{prefix}_RidgeCap", new Vector3(center.x, ridgeY + 0.06f, center.z), new Vector3(0.38f, 0.22f, roofDepth), _fascia);

                CreateBeamBetween(_eavesAndFascia, $"{prefix}_FrontLeftRakeFascia", new Vector3(xL, baseHeight, zF), new Vector3(center.x, ridgeY, zF), 0.22f, 0.28f, _fascia);
                CreateBeamBetween(_eavesAndFascia, $"{prefix}_FrontRightRakeFascia", new Vector3(center.x, ridgeY, zF), new Vector3(xR, baseHeight, zF), 0.22f, 0.28f, _fascia);
                CreateBeamBetween(_eavesAndFascia, $"{prefix}_RearLeftRakeFascia", new Vector3(xL, baseHeight, zR), new Vector3(center.x, ridgeY, zR), 0.22f, 0.28f, _fascia);
                CreateBeamBetween(_eavesAndFascia, $"{prefix}_RearRightRakeFascia", new Vector3(center.x, ridgeY, zR), new Vector3(xR, baseHeight, zR), 0.22f, 0.28f, _fascia);
            }
            else
            {
                float xL = center.x - roofWidth * 0.5f;
                float xR = center.x + roofWidth * 0.5f;
                float zF = center.z - roofDepth * 0.5f;
                float zR = center.z + roofDepth * 0.5f;
                float ridgeY = baseHeight + rise;
                CreateBox(_eavesAndFascia, $"{prefix}_FrontEaveFascia", new Vector3(center.x, baseHeight - 0.12f, zF), new Vector3(roofWidth, 0.36f, 0.22f), _fascia);
                CreateBox(_eavesAndFascia, $"{prefix}_RearEaveFascia", new Vector3(center.x, baseHeight - 0.12f, zR), new Vector3(roofWidth, 0.36f, 0.22f), _fascia);
                CreateBox(_eavesAndFascia, $"{prefix}_RidgeCap", new Vector3(center.x, ridgeY + 0.06f, center.z), new Vector3(roofWidth, 0.22f, 0.38f), _fascia);
                CreateBeamBetween(_eavesAndFascia, $"{prefix}_LeftFrontRakeFascia", new Vector3(xL, baseHeight, zF), new Vector3(xL, ridgeY, center.z), 0.22f, 0.28f, _fascia);
                CreateBeamBetween(_eavesAndFascia, $"{prefix}_LeftRearRakeFascia", new Vector3(xL, ridgeY, center.z), new Vector3(xL, baseHeight, zR), 0.22f, 0.28f, _fascia);
                CreateBeamBetween(_eavesAndFascia, $"{prefix}_RightFrontRakeFascia", new Vector3(xR, baseHeight, zF), new Vector3(xR, ridgeY, center.z), 0.22f, 0.28f, _fascia);
                CreateBeamBetween(_eavesAndFascia, $"{prefix}_RightRearRakeFascia", new Vector3(xR, ridgeY, center.z), new Vector3(xR, baseHeight, zR), 0.22f, 0.28f, _fascia);
            }
        }

        GameObject CreateBeamBetween(GameObject parent, string name, Vector3 start, Vector3 end, float thickness, float depth, Material mat)
        {
            Vector3 delta = end - start;
            var go = CreateBox(parent, name, (start + end) * 0.5f, new Vector3(delta.magnitude, thickness, depth), mat);
            go.transform.localRotation = Quaternion.FromToRotation(Vector3.right, delta.normalized);
            return go;
        }

        GameObject CreateBox(GameObject parent, string name, Vector3 localPosition, Vector3 localScale, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;

            go.AddComponent<MeshFilter>().sharedMesh = CubeMesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            return go;
        }

        GameObject Group(string name, GameObject parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        static Mesh CubeMesh
        {
            get
            {
                if (_cubeMesh != null) return _cubeMesh;
                _cubeMesh = BuildCubeMesh();
                return _cubeMesh;
            }
        }

        static Mesh BuildCubeMesh()
        {
            var mesh = new Mesh { name = "HouseRuntimeCube" };
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(0.5f, -0.5f, 0.5f), new Vector3(0.5f, 0.5f, 0.5f), new Vector3(-0.5f, 0.5f, 0.5f),
                new Vector3(0.5f, -0.5f, -0.5f), new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(-0.5f, 0.5f, -0.5f), new Vector3(0.5f, 0.5f, -0.5f),
                new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(-0.5f, 0.5f, 0.5f), new Vector3(-0.5f, 0.5f, -0.5f),
                new Vector3(0.5f, -0.5f, 0.5f), new Vector3(0.5f, -0.5f, -0.5f), new Vector3(0.5f, 0.5f, -0.5f), new Vector3(0.5f, 0.5f, 0.5f),
                new Vector3(-0.5f, 0.5f, 0.5f), new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.5f, 0.5f, -0.5f), new Vector3(-0.5f, 0.5f, -0.5f),
                new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, 0.5f), new Vector3(-0.5f, -0.5f, 0.5f)
            };
            mesh.triangles = new[]
            {
                0, 1, 2, 0, 2, 3,
                4, 5, 6, 4, 6, 7,
                8, 9, 10, 8, 10, 11,
                12, 13, 14, 12, 14, 15,
                16, 17, 18, 16, 18, 19,
                20, 21, 22, 20, 22, 23
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f)
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        void CreateMaterials()
        {
            // Palette matched to the private home reference photos:
            // charcoal stucco body, white trim/fascia/garage door/railings,
            // weathered gray composition shingles, paver driveway, turf lawn,
            // redwood-tone deck, dry earth side/rear slopes.
            _stucco = stuccoMaterialOverride != null ? stuccoMaterialOverride : CreateMaterial("RuntimeStucco", new Color(0.29f, 0.29f, 0.30f), 0.18f, 0f, CreateStuccoTexture());
            _stuccoAccent = stuccoAccentMaterialOverride != null ? stuccoAccentMaterialOverride : CreateMaterial("RuntimeStuccoSlightlyLight", new Color(0.34f, 0.34f, 0.35f), 0.18f, 0f, CreateStuccoTexture());
            _roof = roofMaterialOverride != null ? roofMaterialOverride : CreateMaterial("RuntimeCompositionShingle", new Color(0.38f, 0.37f, 0.36f), 0.12f, 0f, CreateShingleTexture());
            _fascia = fasciaMaterialOverride != null ? fasciaMaterialOverride : CreateMaterial("RuntimeFascia", new Color(0.92f, 0.92f, 0.90f), 0.2f, 0f, null);
            _trim = trimMaterialOverride != null ? trimMaterialOverride : CreateMaterial("RuntimeTrim", new Color(0.94f, 0.94f, 0.92f), 0.18f, 0f, null);
            _garageDoor = garageDoorMaterialOverride != null ? garageDoorMaterialOverride : CreateMaterial("RuntimeGarageDoor", new Color(0.93f, 0.93f, 0.91f), 0.22f, 0f, null);
            _glass = glassMaterialOverride != null ? glassMaterialOverride : CreateGlassMaterial("RuntimeBlueGrayGlass", new Color(0.58f, 0.66f, 0.70f, 0.22f));
            _door = doorMaterialOverride != null ? doorMaterialOverride : CreateMaterial("RuntimeFrontDoor", new Color(0.90f, 0.90f, 0.88f), 0.25f, 0f, null);
            _concrete = concreteMaterialOverride != null ? concreteMaterialOverride : CreateMaterial("RuntimeConcrete", new Color(0.62f, 0.62f, 0.59f), 0.1f, 0f, null);
            _ground = groundMaterialOverride != null ? groundMaterialOverride : CreateMaterial("RuntimeGround", new Color(0.52f, 0.45f, 0.33f), 0.25f, 0f, null);
            _driveway = drivewayMaterialOverride != null ? drivewayMaterialOverride : CreateMaterial("RuntimeDriveway", new Color(0.66f, 0.62f, 0.57f), 0.08f, 0f, null);
            _interiorWall = interiorWallMaterialOverride != null ? interiorWallMaterialOverride : CreateMaterial("RuntimeInteriorReference", new Color(0.62f, 0.69f, 0.72f), 0.2f, 0f, null);
            _railing = railingMaterialOverride != null ? railingMaterialOverride : CreateMaterial("RuntimeRailing", new Color(0.92f, 0.92f, 0.90f), 0.18f, 0f, null);
            _darkTrim = darkTrimMaterialOverride != null ? darkTrimMaterialOverride : CreateMaterial("RuntimeDarkTrim", new Color(0.18f, 0.17f, 0.15f), 0.15f, 0f, null);
            _deck = deckMaterialOverride != null ? deckMaterialOverride : CreateMaterial("RuntimeDeckWood", new Color(0.42f, 0.27f, 0.17f), 0.2f, 0f, null);
            _lawn = CreateMaterial("RuntimeTurfLawn", new Color(0.28f, 0.52f, 0.22f), 0.22f, 0f, null);

            // Interior finishes, matched to the scan photos: warm hardwood floors,
            // white walls, speckled gray garage epoxy, tan granite, wood cabinets,
            // and brushed-stainless appliances.
            _hardwood = CreateMaterial("RuntimeHardwood", new Color(0.40f, 0.26f, 0.15f), 0.35f, 0f, CreateWoodTexture());
            _hardwood.mainTextureScale = new Vector2(6f, 6f);
            _epoxy = CreateMaterial("RuntimeGarageEpoxy", new Color(0.48f, 0.49f, 0.50f), 0.45f, 0f, CreateSpeckleTexture(new Color(0.50f, 0.51f, 0.52f), new Color(0.30f, 0.31f, 0.33f), new Color(0.72f, 0.72f, 0.70f)));
            _epoxy.mainTextureScale = new Vector2(5f, 5f);
            _granite = CreateMaterial("RuntimeGranite", new Color(0.72f, 0.68f, 0.60f), 0.55f, 0f, CreateSpeckleTexture(new Color(0.74f, 0.70f, 0.62f), new Color(0.42f, 0.36f, 0.30f), new Color(0.90f, 0.88f, 0.82f)));
            _granite.mainTextureScale = new Vector2(2.5f, 2.5f);
            _cabinet = CreateMaterial("RuntimeWoodCabinet", new Color(0.34f, 0.20f, 0.11f), 0.30f, 0f, CreateWoodTexture());
            _cabinet.mainTextureScale = new Vector2(2f, 2f);
            _appliance = CreateMaterial("RuntimeStainless", new Color(0.62f, 0.63f, 0.65f), 0.85f, 0.85f, null);
            _wallPaint = CreateMaterial("RuntimeWallPaint", new Color(0.90f, 0.89f, 0.86f), 0.15f, 0f, null);
            _water = CreateGlassMaterial("RuntimeHotTubWater", new Color(0.18f, 0.45f, 0.55f, 0.72f));
        }

        static Material CreateMaterial(string name, Color color, float glossiness, float metallic, Texture2D texture)
        {
            var shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Legacy Shaders/Diffuse");
            if (shader == null) shader = Shader.Find("Diffuse");

            var mat = new Material(shader) { name = name, color = color };
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", glossiness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (texture != null && mat.HasProperty("_MainTex"))
            {
                mat.mainTexture = texture;
                mat.mainTextureScale = name.Contains("Shingle") ? new Vector2(3f, 5f) : new Vector2(4f, 4f);
            }
            return mat;
        }

        static Material CreateGlassMaterial(string name, Color color)
        {
            var mat = CreateMaterial(name, color, 0.72f, 0f, null);
            if (mat.HasProperty("_Mode"))
            {
                mat.SetFloat("_Mode", 3f);
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = (int)RenderQueue.Transparent;
            }
            return mat;
        }

        static Texture2D CreateStuccoTexture()
        {
            const int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "RuntimeStuccoNoise", wrapMode = TextureWrapMode.Repeat };
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float noise = 0.88f + (((x * 19 + y * 37) % 17) / 16f) * 0.16f;
                    tex.SetPixel(x, y, new Color(noise, noise, noise, 1f));
                }
            }
            tex.Apply();
            return tex;
        }

        // Warm plank lines with subtle grain, tinted white so the material color
        // multiplies through (used for hardwood floors and wood cabinets).
        static Texture2D CreateWoodTexture()
        {
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "RuntimeWoodGrain", wrapMode = TextureWrapMode.Repeat };
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool plankSeam = (y % 16) == 0;
                    float grain = 0.86f + (((x * 13 + y * 7) % 11) / 10f) * 0.14f;
                    float v = plankSeam ? 0.6f : grain;
                    tex.SetPixel(x, y, new Color(v, v, v, 1f));
                }
            }
            tex.Apply();
            return tex;
        }

        // Speckled stone/epoxy: a base tone flecked with darker and lighter
        // grains. Tinted so the material color tints the base.
        static Texture2D CreateSpeckleTexture(Color baseCol, Color darkFleck, Color lightFleck)
        {
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "RuntimeSpeckle", wrapMode = TextureWrapMode.Repeat };
            var rng = new System.Random(12345);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    double r = rng.NextDouble();
                    Color c = r < 0.10 ? darkFleck : (r > 0.90 ? lightFleck : baseCol);
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            return tex;
        }

        static Texture2D CreateShingleTexture()
        {
            const int width = 64;
            const int height = 64;
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false) { name = "RuntimeShingleLines", wrapMode = TextureWrapMode.Repeat };
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool horizontalLine = y % 10 == 0 || y % 10 == 1;
                    bool stagger = ((y / 10) % 2) == 1;
                    bool verticalBreak = ((x + (stagger ? 16 : 0)) % 32) == 0 && (y % 10) > 1;
                    float v = horizontalLine || verticalBreak ? 0.68f : 0.92f;
                    tex.SetPixel(x, y, new Color(v, v, v, 1f));
                }
            }
            tex.Apply();
            return tex;
        }

#if UNITY_EDITOR
        public static Material CreateEditorMaterialAsset(string assetPath, Color color, float glossiness, bool transparent, Texture2D texture = null)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (mat == null)
            {
                var shader = Shader.Find("Standard");
                if (shader == null) shader = Shader.Find("Legacy Shaders/Diffuse");
                if (shader == null) shader = Shader.Find("Diffuse");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, assetPath);
            }

            mat.color = color;
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", glossiness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
            if (texture != null)
            {
                mat.mainTexture = texture;
                mat.mainTextureScale = assetPath.Contains("Roof") ? new Vector2(3f, 5f) : new Vector2(4f, 4f);
            }

            if (transparent && mat.HasProperty("_Mode"))
            {
                mat.SetFloat("_Mode", 3f);
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = (int)RenderQueue.Transparent;
            }
            else if (mat.HasProperty("_Mode"))
            {
                mat.SetFloat("_Mode", 0f);
                mat.SetOverrideTag("RenderType", "");
                mat.SetInt("_SrcBlend", (int)BlendMode.One);
                mat.SetInt("_DstBlend", (int)BlendMode.Zero);
                mat.SetInt("_ZWrite", 1);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.DisableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = -1;
            }

            EditorUtility.SetDirty(mat);
            return mat;
        }

        public static Texture2D CreateEditorTextureAsset(string assetPath, bool shingles)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (tex != null) return tex;

            tex = shingles ? CreateShingleTexture() : CreateStuccoTexture();
            AssetDatabase.CreateAsset(tex, assetPath);
            EditorUtility.SetDirty(tex);
            return tex;
        }
#endif
    }
}
