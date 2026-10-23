using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace TowerDefense.Tests
{
    public sealed class LevelGridTests
    {
        private GameObject gridObject;
        private LevelGrid grid;

        [SetUp]
        public void SetUp()
        {
            gridObject = new GameObject("LevelGrid test");
            grid = gridObject.AddComponent<LevelGrid>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(gridObject);
        }

        [Test]
        public void OneCellBuildingSnapsToItsCenterInRotatedGrid()
        {
            gridObject.transform.position = new Vector3(10f, 0f, 5f);
            gridObject.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            Vector3 pointer = gridObject.transform.TransformPoint(new Vector3(-28.2f, 0f, -28.8f));

            Assert.That(grid.TryGetPlacement(pointer, BuildSlotType.Defense, out RectInt cells), Is.True);
            Assert.That(cells, Is.EqualTo(new RectInt(0, 0, 1, 1)));
            Assert.That(Vector3.Distance(grid.GetPlacementCenter(cells),
                gridObject.transform.TransformPoint(new Vector3(-28.5f, 0f, -28.5f))), Is.LessThan(0.001f));
        }

        [Test]
        public void LargeBuildingCannotCrossGridEdgeOrOccupiedFootprint()
        {
            Assert.That(grid.TryGetPlacement(new Vector3(27f, 0f, 27f),
                BuildSlotType.TownHall, out RectInt inside), Is.True);
            Assert.That(inside.size, Is.EqualTo(new Vector2Int(2, 2)));
            Assert.That(grid.TryGetPlacement(new Vector3(29.9f, 0f, 29.9f),
                BuildSlotType.TownHall, out RectInt outside), Is.False);
            Assert.That(grid.Contains(outside), Is.False);
            Assert.That(LevelGrid.Overlaps(inside, new RectInt(19, 19, 1, 1)), Is.True);
            Assert.That(LevelGrid.Overlaps(inside, new RectInt(10, 10, 1, 1)), Is.False);
        }

        [Test]
        public void MultipleGridsUseTheirOwnOrigin()
        {
            var other = new GameObject("Second grid");
            try
            {
                other.transform.position = new Vector3(100f, 0f, 0f);
                var secondGrid = other.AddComponent<LevelGrid>();
                Vector3 pointer = new Vector3(100f, 0f, 0f);

                Assert.That(grid.TryGetPlacement(pointer, BuildSlotType.Defense, out _), Is.False);
                Assert.That(secondGrid.TryGetPlacement(pointer, BuildSlotType.Defense,
                    out RectInt cells), Is.True);
                Assert.That(Vector3.Distance(secondGrid.GetPlacementCenter(cells), pointer),
                    Is.LessThan(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(other);
            }
        }

        [Test]
        public void InteractionZoneSnapsOnlyToCellsBesideChosenBuilding()
        {
            var building = new RectInt(6, 6, 2, 2);
            int choices = 0;
            foreach (RectInt candidate in grid.GetAdjacentZoneCells(building))
            {
                choices++;
                Assert.That(grid.Contains(candidate), Is.True);
                Assert.That(LevelGrid.Overlaps(candidate, building), Is.False);
            }
            Assert.That(choices, Is.EqualTo(8));

            var eastCell = new RectInt(8, 6, 1, 1);
            Vector3 pointer = grid.GetPlacementCenter(eastCell) + new Vector3(0.2f, 0f, 0.1f);
            Assert.That(grid.TryGetAdjacentZoneCell(building, pointer, out RectInt snapped), Is.True);
            Assert.That(snapped, Is.EqualTo(eastCell));
            Assert.That(grid.TryGetAdjacentZoneCell(building,
                grid.GetPlacementCenter(building), out _), Is.False);
        }

        [Test]
        public void InteractionZoneNeverSnapsOutsideGrid()
        {
            var building = new RectInt(0, 0, 1, 1);
            int choices = 0;
            foreach (RectInt candidate in grid.GetAdjacentZoneCells(building))
            {
                choices++;
                Assert.That(grid.Contains(candidate), Is.True);
            }
            Assert.That(choices, Is.EqualTo(2));
        }

        [Test]
        public void LargeBuildingFacesTheSideOfItsInteractionZoneOnRotatedGrid()
        {
            gridObject.transform.rotation = Quaternion.Euler(0f, 37f, 0f);
            var building = new RectInt(6, 6, 2, 2);
            var zones = new[]
            {
                new RectInt(6, 5, 1, 1),
                new RectInt(7, 5, 1, 1),
                new RectInt(6, 8, 1, 1),
                new RectInt(7, 8, 1, 1),
                new RectInt(5, 6, 1, 1),
                new RectInt(5, 7, 1, 1),
                new RectInt(8, 6, 1, 1),
                new RectInt(8, 7, 1, 1)
            };
            var expected = new[]
            {
                Vector3.back, Vector3.back, Vector3.forward, Vector3.forward,
                Vector3.left, Vector3.left, Vector3.right, Vector3.right
            };
            for (int i = 0; i < zones.Length; i++)
                Assert.That(Vector3.Angle(grid.GetBuildingRotation(building, zones[i])
                    * Vector3.forward, grid.transform.TransformDirection(expected[i])),
                    Is.LessThan(0.01f), zones[i].ToString());
        }

        [Test]
        public void PlacedZoneIdentifiesBuildingAndInteractionCells()
        {
            var zoneObject = new GameObject("Grid-placed zone");
            var previewObject = new GameObject("Building preview");
            try
            {
                var placement = zoneObject.AddComponent<LevelGridPlacedZone>();
                placement.Configure(grid, new RectInt(4, 5, 2, 2),
                    new RectInt(6, 5, 1, 1), previewObject);

                Assert.That(placement.SourceGrid, Is.SameAs(grid));
                Assert.That(placement.PreviewPrefab, Is.SameAs(previewObject));
                Assert.That(placement.ContainsCell(new RectInt(5, 6, 1, 1)), Is.True);
                Assert.That(placement.ContainsCell(new RectInt(6, 5, 1, 1)), Is.True);
                Assert.That(placement.ContainsCell(new RectInt(7, 5, 1, 1)), Is.False);

                placement.Configure(grid, new RectInt(9, 9, 2, 2),
                    new RectInt(11, 9, 1, 1), previewObject);
                Assert.That(placement.ContainsCell(new RectInt(5, 6, 1, 1)), Is.False);
                Assert.That(placement.ContainsCell(new RectInt(6, 5, 1, 1)), Is.False);
                Assert.That(placement.ContainsCell(new RectInt(10, 10, 1, 1)), Is.True);
                Assert.That(placement.ContainsCell(new RectInt(11, 9, 1, 1)), Is.True);
                Assert.That(placement.PreviewPrefab, Is.SameAs(previewObject));
            }
            finally
            {
                Object.DestroyImmediate(zoneObject);
                Object.DestroyImmediate(previewObject);
            }
        }

        [Test]
        public void TileSurfaceRepeatsTextureOnEachCellWithoutEnablingCollision()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            Assert.That(shader, Is.Not.Null);
            var material = new Material(shader);
            try
            {
                var surface = gridObject.AddComponent<LevelGridTileSurface>();
                var data = new SerializedObject(surface);
                data.FindProperty("tileMaterial").objectReferenceValue = material;
                data.FindProperty("showTiles").boolValue = true;
                data.ApplyModifiedPropertiesWithoutUndo();
                surface.Rebuild();

                Mesh mesh = gridObject.GetComponent<MeshFilter>().sharedMesh;
                Assert.That(mesh, Is.Not.Null);
                Assert.That(mesh.vertexCount, Is.EqualTo(20 * 20 * 4));
                Assert.That(mesh.uv[0], Is.EqualTo(Vector2.zero));
                Assert.That(mesh.uv[4], Is.EqualTo(Vector2.zero));
                Assert.That(gridObject.GetComponent<MeshCollider>().enabled, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(material);
            }
        }
    }
}
