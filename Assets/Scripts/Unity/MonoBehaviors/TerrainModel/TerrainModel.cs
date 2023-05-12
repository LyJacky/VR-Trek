using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering;
using static TrekVRApplication.TerrainConstants;
using Debug = UnityEngine.Debug;

namespace TrekVRApplication {

    public abstract class TerrainModel : MonoBehaviourWithTaskQueue {

        protected GameObject _lodGroupContainer;

        public abstract XRInteractableTerrain InteractionController { get; }

        // Don't forget to call AddLayerController() after adding a TerrainModel component.
        public TerrainLayerController LayerController { get; private set; }

        public abstract string DemUUID { get; set; }

        [SerializeField]
        protected float _radius;
        public float Radius {
            get => _radius;
            set { if (_initTaskStatus == TaskStatus.NotStarted) _radius = value; }
        }

        // TODO Add option to use linear LOD downsampling.

        [SerializeField]
        protected int _lodLevels = 1;
        /// <summary>
        ///     The number of LOD levels to be generated, excluding LOD 0 and physics LOD.
        /// </summary>
        public int LodLevels {
            get => _lodLevels;
            set {
                if (_initTaskStatus == TaskStatus.NotStarted) {
                    // Number of LOD levels must be a non-negative integer.
                    _lodLevels = MathUtils.Clamp(value, 0);
                }
            }
        }

        [SerializeField]
        protected int _baseDownsampleLevel = 0;
        /// <summary>
        ///     <para>
        ///         The amount of downsampling applied to the DEM file to generate the 
        ///         mesh (LOD 0). The actual amount of downsampling applied is 2^value.
        ///     </para>
        ///     <para>
        ///         For example, a value of 0 will have no downsampling, while a value
        ///         of 3 will downsample the DEM image by a factor of 8.
        ///     </para>
        /// </summary>
        public int BaseDownsampleLevel {
            get => _baseDownsampleLevel;
            set {
                if (_initTaskStatus == TaskStatus.NotStarted) {
                    // Downsampling level must be a non-negative integer.
                    _baseDownsampleLevel = MathUtils.Clamp(value, 0);
                }
            }
        }

        [SerializeField]
        protected int _physicsDownsampleLevel = -1;
        /// <summary>
        ///     <para>
        ///         The amount of downsampling applied to the DEM file to generate the 
        ///         physics mesh. The actual amount of downsampling applied is 2^value.
        ///     </para>
        ///     <para>
        ///         For example, a value of 0 will have no downsampling, while a value
        ///         of 3 will downsample the DEM image by a factor of 8.
        ///     </para>
        ///     <para>
        ///         Set this to a negative number to indicate that a physics mesh does
        ///         not need to be generated.
        ///     </para>
        /// </summary>
        public int PhysicsDownsampleLevel {
            get => _physicsDownsampleLevel;
            set {
                if (_initTaskStatus == TaskStatus.NotStarted) {
                    _physicsDownsampleLevel = value;
                }
            }
        }

        /// <summary>
        ///     Copy of the mesh data with 1.0x height scale.
        /// </summary>
        protected TerrainMeshData[] _referenceMeshData;

        protected float _physicsMeshUpdateTimer;

        protected MeshData _physicsMeshUpdatedData;

        protected float _pendingHeightScale = float.NaN;

        protected TaskStatus _heightRescaleTaskStatus = TaskStatus.NotStarted;

        [SerializeField]
        protected float _heightScale = 1.0f;
        /// <summary>
        ///     Terrain height exaggeration.
        /// </summary>
        public float HeightScale {
            get => _heightScale;
            set {
                RequestTerrainHeightRescale(value);
            }
        }

        protected TaskStatus _initTaskStatus = TaskStatus.NotStarted;

        /// <summary>
        ///     The last time this terrain was set to visible via the Visible property.
        /// </summary>
        public long LastVisible { get; private set; } = 0L;

        public bool Visible {
            get => gameObject.activeSelf;
            set {
                if (!value) {
                    gameObject.SetActive(false);
                }
                else if (!gameObject.activeSelf) {
                    gameObject.SetActive(true);
                    LastVisible = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    InteractionController.SwitchToActivity(XRInteractableTerrainActivity.Default);
                }
            }
        }

        #region Unity lifecycle methods

        // Start is used instead of Awake so that property values can 
        // be assigned before the model intialization starts.
        protected virtual void Start() {

            // If the global height scale is not 1.0x, then height
            // scaling need to be applied after mesh is generated.
            float globalHeightScale = TerrainModelManager.Instance.HeightExagerration;
            if (_heightScale != globalHeightScale) {
                RequestTerrainHeightRescale(globalHeightScale);
            }

            // Initialize the mesh/model.
            _initTaskStatus = TaskStatus.InProgress;
            AddRenderTextureOverlay();
            GenerateMesh();
        }

        protected override void Update() {
            base.Update();
            if (!float.IsNaN(_pendingHeightScale)) {
                RequestTerrainHeightRescale(_pendingHeightScale);
            }
            if (_physicsMeshUpdatedData) {
                if (_physicsMeshUpdateTimer <= 0) {
                    UpdatePhysicsMesh(_physicsMeshUpdatedData);
                    _physicsMeshUpdatedData = null;
                } else {
                    _physicsMeshUpdateTimer -= Time.deltaTime;
                }
            }
        }

        #endregion

        /// <summary>
        ///     Adds a TerrainLayerController to the TerrainModel. This can only be called
        ///     once, and should be called right after the TerrainModel component is created.
        /// </summary>
        public T AddLayerController<T>() where T : TerrainLayerController {
            if (_initTaskStatus > TaskStatus.NotStarted) {
                throw new Exception("Cannot add layer controller after model initialization has already started.");
            }
            if (LayerController) {
                throw new Exception("Terrain model already contains a layer controller.");
            }
            LayerController = gameObject.AddComponent<T>();
            return (T)LayerController;
        }

        protected abstract void AddRenderTextureOverlay();

        protected abstract void GenerateMesh();

        /// <summary>
        ///     <para>
        ///         Processes the generated mesh data. Creates a mesh renderer and mesh
        ///         filter for each LOD mesh, and assigns material to the mesh. Also creates
        ///         a LOD group containing the LOD mesh levels. Note that this method does
        ///         not process the physics mesh (if one has been generated) � it is up to
        ///         the implementing class to add logic to process the phycics mesh.
        ///     </para>
        ///     <para>
        ///         This method is intended to be called from implementations of TerrainModel;
        ///         it is not called by the TerrainModel abstract class itself.
        ///     </para>
        ///     <para>
        ///         After this is called, the member variable _lodGroupContainer will be accessible.
        ///     </para>
        /// </summary>
        protected virtual void ProcessMeshData(TerrainMeshData[] meshData) {

            // Add LOD group manager.
            _lodGroupContainer = new GameObject(GameObjectName.LODGroupContainer) {
                layer = (int)CullingLayer.Terrain
            };
            _lodGroupContainer.transform.SetParent(transform, false);

            LODGroup lodGroup = _lodGroupContainer.AddComponent<LODGroup>();
            LOD[] lods = new LOD[_lodLevels + 1];

            // Create a child GameObject containing a mesh for each LOD level.
            for (int i = 0; i <= _lodLevels; i++) {
                CreateLod(meshData, lods, i);
            }

            // Assign LOD meshes to LOD group.
            lodGroup.SetLODs(lods);

            // Calculate bounds if there are one or more LOD level. If there are no LOD levels, 
            // then we can just disable LOD, so there is no need to calculate bounds.
            if (_lodLevels > 0) {
                lodGroup.RecalculateBounds();
            }
            else {
                lodGroup.enabled = false;
            }

        }

        protected virtual void CreateLod(TerrainMeshData[] meshData, LOD[] lods, int index) {
            GameObject child = new GameObject($"LOD_{index}") {
                layer = (int)CullingLayer.Terrain
            };
            child.transform.SetParent(_lodGroupContainer.transform);

            // Use the parent's tranformations.
            child.transform.localPosition = Vector3.zero;
            child.transform.localScale = Vector3.one;
            child.transform.localEulerAngles = Vector3.zero;

            // Add MeshRenderer to child, and to the LOD group.
            MeshRenderer meshRenderer = child.AddComponent<MeshRenderer>();
            lods[index] = new LOD(Mathf.Pow(GlobeModelLODCoefficient, index + 1), new Renderer[] { meshRenderer });

            // Add material to the MeshRenderer.
            meshRenderer.material = LayerController.Material;

            // Create a Mesh from the mesh data and add the mesh to a MeshFilter.
            TerrainMeshData data = meshData[index];
            Mesh mesh = ConvertToMesh(data.Vertices, data.TexCoords, data.Triangles);
            child.AddComponent<MeshFilter>().mesh = mesh;
        }

        protected Mesh ConvertToMesh(Vector3[] vertices, Vector2[] texCoords, int[] triangles, bool recaculateNormals = true) {
            Mesh mesh = new Mesh();
            UpdateMesh(mesh, vertices, texCoords, triangles, recaculateNormals);
            return mesh;
        }

        protected void UpdateMesh(Mesh mesh, Vector3[] vertices, Vector2[] texCoords, int[] triangles, bool recaculateNormals = true) {

            // If needed, set the index format of the mesh to 32-bits,
            // so that the mesh can have more than 65k vertices.
            if (vertices.Length > (1 << 16)) {
                mesh.indexFormat = IndexFormat.UInt32;
            }

            if (vertices != null) {
                float start = Time.realtimeSinceStartup;
                mesh.vertices = vertices;
                Debug.Log($"Took {Time.realtimeSinceStartup - start} seconds to assign vertices.");
            }

            if (texCoords != null) {
                float start = Time.realtimeSinceStartup;
                mesh.uv = texCoords;
                Debug.Log($"Took {Time.realtimeSinceStartup - start} seconds to assign UVs.");
            }

            if (triangles != null) {
                float start = Time.realtimeSinceStartup;
                mesh.triangles = triangles;
                Debug.Log($"Took {Time.realtimeSinceStartup - start} seconds to assign triangles.");
            }

            if (recaculateNormals) {
                float start = Time.realtimeSinceStartup;

                // This is a time consuming operation, and may cause the app to pause
                // for a couple of miliseconds since it runs on the main thread.
                mesh.RecalculateNormals();
                Debug.Log($"Took {Time.realtimeSinceStartup - start} seconds to recalculate normals.");
            }

            // TODO Try using mesh.UploadMeshData(true) to save system memory.
            // TODO Try using mesh.MarkDynamic() to see if there is an improvement when continuously updating the mesh.

        }

        protected virtual void ApplyRescaledMeshData(TerrainMeshData[] rescaledMeshData) {
            for (int i = 0; i <= _lodLevels; i++) {

                // TODO Add null checks.
                Transform child = _lodGroupContainer.transform.Find($"LOD_{i}");
                MeshFilter meshFilter = child.GetComponent<MeshFilter>();
                Mesh mesh = meshFilter.mesh;

                TerrainMeshData meshData = rescaledMeshData[i];
                UpdateMesh(mesh, meshData.Vertices, null, null, true);  // Only update vertices
            }
        }

        /** Helper method for added a shadow casting mesh to the terrain. */
        protected GameObject AddShadowCaster(Mesh mesh) {
            GameObject shadowCaster = new GameObject(GameObjectName.TerrainShadowCaster) {
                layer = (int)CullingLayer.TerrainShadowCaster
            };
            shadowCaster.transform.SetParent(transform, false);
            MeshRenderer meshRenderer = shadowCaster.AddComponent<MeshRenderer>();
            meshRenderer.material = LayerController.Material; // TODO Change this to a generic material.
            meshRenderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            meshRenderer.receiveShadows = false;
            MeshFilter meshFilter = shadowCaster.AddComponent<MeshFilter>();
            meshFilter.mesh = mesh;
            return shadowCaster;
        }

        protected virtual TerrainModelMeshMetadata GenerateMeshMetadata() {
            return new TerrainModelMeshMetadata() {
                DemUUID = DemUUID,
                Radius = Radius * TerrainModelScale,
                HeightScale = HeightScale * TerrainModelScale,
                LodLevels = LodLevels,
                BaseDownsample = BaseDownsampleLevel,
                PhysicsDownsample = PhysicsDownsampleLevel
            };
        }

        private void RequestTerrainHeightRescale(float scale) {
            if (float.IsNaN(scale)) {
                return;
            }
            if (!CanRescaleTerrainHeight()) {
                _pendingHeightScale = scale;
                return;
            }
            if (scale != _heightScale) {
                _heightScale = scale;
                RescaleTerrainHeight(_heightScale);
            }
            _pendingHeightScale = float.NaN;
        }

        protected abstract bool CanRescaleTerrainHeight();

        protected abstract void RescaleTerrainHeight(float scale);

        protected void UpdatePhysicsMesh(MeshData meshData) {
            MeshCollider collider = GetComponent<MeshCollider>();
            if (collider) {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                Mesh mesh = collider.sharedMesh;
                UpdateMesh(mesh, meshData.Vertices, null, null, false); // Only update vertices
                collider.sharedMesh = mesh;
                Debug.Log($"Took {stopwatch.ElapsedMilliseconds}ms to update physics mesh.");
                stopwatch.Stop();
                TerrainModelManager.Instance.ReportPhysicsMeshUpdated(this);
            }
        }

    }

}
