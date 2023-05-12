﻿using System.Collections.Generic;
using UnityEngine;
using static TrekVRApplication.FlagUtils;
using static TrekVRApplication.TerrainConstants;
using static TrekVRApplication.ServiceManager;
using System;

namespace TrekVRApplication {

    public abstract class TerrainLayerController : MonoBehaviour {

        /// <summary>
        ///     The maximum number of diffuese layers, including the base layer.
        /// </summary>
        protected const int MaxDiffuseLayers = 8;

        protected readonly TerrainModelManager _terrainModelManager = TerrainModelManager.Instance;

        protected bool Started { get; private set; }

        public abstract IList<TerrainLayer> Layers { get; }

        private Material _material;
        public Material Material {
            get {
                if (!_material) {
                    _material = GenerateMaterial();
                }
                return _material;
            }
            set {
                if (Started) {
                    Debug.LogError("Material cannot be set externally after Start() has been called.");
                    return;
                }
                _material = new Material(value);
            }
        }

        private IBoundingBox _boundingBox = UnrestrictedBoundingBox.Global;
        public IBoundingBox BoundingBox {
            get => _boundingBox;
            set {
                if (Started) {
                    Debug.LogError("Bounding box cannot be directly set after Start() has been called. Use UpdateBoundingBox() instead.");
                    return;
                }
                _boundingBox = value;
            }
        }

        public Vector2Int TargetTextureSize { get; set; } = Vector2Int.one * LocalTerrainTextureTargetSize;

        #region Render mode properties

        public bool EnableOverlay {
            get => ContainsFlag((int)RenderMode, (int)TerrainRenderMode.Overlay);
            set => RenderMode = (TerrainRenderMode)AddOrRemoveFlag((int)RenderMode, (int)TerrainRenderMode.Overlay, value);
        }

        public bool DisableTextures {
            get => ContainsFlag((int)RenderMode, (int)TerrainRenderMode.NoTextures);
            set => RenderMode = (TerrainRenderMode)AddOrRemoveFlag((int)RenderMode, (int)TerrainRenderMode.NoTextures, value);
        }

        public bool UseDisabledMaterial {
            get => ContainsFlag((int)RenderMode, (int)TerrainRenderMode.Disabled);
            set => RenderMode = (TerrainRenderMode)AddOrRemoveFlag((int)RenderMode, (int)TerrainRenderMode.Disabled, value);
        }

        private TerrainRenderMode _renderMode;
        public TerrainRenderMode RenderMode {
            get => _renderMode;
            private set {
                _renderMode = value;
                UpdateMaterialProperties();
            }
        }

        #endregion

        #region Unity lifecycle methods

        protected virtual void Start() {
            Started = true;
            UseDisabledMaterial = !_terrainModelManager.TerrainInteractionEnabled;
            DisableTextures = !_terrainModelManager.TerrainTexturesEnabled;
            ReloadTextures(Layers);
        }

        protected virtual void OnDestroy() {
            Destroy(Material);
            // TODO Destroy more things
        }

        #endregion

        public void AddLayer(string productUUID, Action callback) {
            AddLayer(productUUID, null, callback);
        }

        public void AddLayer(string productUUID, int? index) {
            AddLayer(productUUID, index, null);
        }

        public abstract void AddLayer(string productUUID, int? index = null, Action callback = null);

        public abstract void UpdateLayer(TerrainLayerChange changes, Action callback = null);

        public abstract void MoveLayer(int from, int to, Action callback = null);

        public abstract void RemoveLayer(int index, Action callback = null);

        /// <summary>
        ///     Updates the bounding box of the terrain model. The layer controller
        ///     will reload the textures accordingly.
        /// </summary>
        /// <param name="bbox">The new bounding box.</param>
        /// <param name="useTemporaryTextures">
        ///     Whether to use existing textures to the mesh as a placeholder until  
        ///     the more detailed textures can be loaded.
        /// </param>
        public void UpdateBoundingBox(IBoundingBox bbox, bool useTemporaryTextures = true) {
            if (bbox == BoundingBox) {
                return;
            }

            if (useTemporaryTextures) {
                UVScaleOffset uvScaleOffset = BoundingBoxUtils.CalculateUVScaleOffset(BoundingBox, bbox);
                for (int i = 0; i < MaxDiffuseLayers; i++) {
                    int layerId = GetShaderTextureId(i);
                    if (!Material.GetTexture(layerId)) {
                        continue;
                    }
                    Material.SetTextureScale(layerId, uvScaleOffset.Scale);
                    Material.SetTextureOffset(layerId, uvScaleOffset.Offset);
                }
            }
            else {
                for (int i = 0; i < MaxDiffuseLayers; i++) {
                    int layerId = GetShaderTextureId(i);
                    if (Material.GetTexture(layerId)) {
                        Material.SetTexture(layerId, null);
                        if (i > 0) {
                            Material.SetFloat($"_Diffuse{i}Opacity", 0);
                        }
                    }
                }
            }

            BoundingBox = bbox;

            if (Started) {
                ReloadTextures(Layers);
            }
        }

        #region Render mode methods

        /// <summary>
        ///     Update the material properties to reflect current render mode.
        /// </summary>
        public void UpdateMaterialProperties() {

            Debug.Log($"DisableTextures={DisableTextures}, UseDisabledMaterial ={UseDisabledMaterial}, EnableOverlay={EnableOverlay}");

            string shaderName;
            if (DisableTextures) {
                shaderName = UseDisabledMaterial ? "NoTexturesOverlayTransparent" : "NoTexturesOverlay";
            }
            else {
                shaderName = UseDisabledMaterial ? "MultiDiffuseOverlayTransparent" : "MultiDiffuseOverlay";
            }

            SwitchToShader($"Custom/Terrain/{shaderName}");

            // This is redundant, since if UseDisabledMaterial is false, the shader
            // is not used and the _DiffuseOpacity parameter has no effect.
            Material.SetFloat("_DiffuseOpacity", UseDisabledMaterial ? 0.69f : 1);   // TODO Define the opacity as a constant.

            Material.SetFloat("_Glossiness", DisableTextures ? NoTextureShaderSmoothness : ShaderSmoothness);
            Material.SetFloat("_Metallic", DisableTextures ? NoTextureShaderMetallic : ShaderMetallic);

            Material.SetFloat("_OverlayOpacity", EnableOverlay ? 1 : 0);

            if (DisableTextures) {
                Material.SetColor("_Color", Color.white);
            }
        }

        /// <summary>
        ///     Helper method for switching between different shaders.
        /// </summary>
        private void SwitchToShader(string shaderName) {
            Shader shader = Shader.Find(shaderName);
            if (shader) {
                Material.shader = shader;
            }
            else {
                Debug.LogError($"Could not find shader {shaderName}.");
            }
        }

        #endregion

        protected abstract Material GenerateMaterial();

        protected void ReloadTextures(IList<TerrainLayer> layers, bool reloadBase = true) {
            TerrainModelTextureManager textureManager = TerrainModelTextureManager.Instance;
            for (int i = reloadBase ? 0 : 1; i < MaxDiffuseLayers; i++) {
                int _i = i; // Copy the index for the anonymous function.
                int layerId = GetShaderTextureId(i);
                if (i < layers.Count) {
                    TerrainLayer layer = layers[i];
                    textureManager.GetTexture(GenerateProductMetadata(layer.ProductUUID), texture => {
                        Material.SetTexture(layerId, texture);
                        Material.SetTextureScale(layerId, Vector2.one);
                        Material.SetTextureOffset(layerId, Vector2.zero);
                        if (_i > 0) {
                            Material.SetFloat($"_Diffuse{_i}Opacity", layer.Visible ? layer.Opacity : 0);
                        }
                    });
                }
                else {
                    Material.SetTexture(layerId, null);
                    if (i > 0) {
                        Material.SetFloat($"_Diffuse{i}Opacity",0);
                    }
                }
            }
        }

        protected void CreateLayer(string uuid, Action<TerrainLayer> callback) {
            RasterSubsetWebService.GetRaster(uuid, raster => {
                if (raster == null) {
                    throw new Exception("No result.");
                }
                callback(new TerrainLayer(raster.Name, uuid, raster.ThumbnailUrl));
            });
        }

        protected TerrainProductMetadata GenerateProductMetadata(string uuid) {
            return new TerrainProductMetadata(uuid, BoundingBox, TargetTextureSize.x, TargetTextureSize.y);
        }

        private int GetShaderTextureId(int index) {
            return Shader.PropertyToID(index == 0 ? "_DiffuseBase" : $"_Diffuse{index}");
        }

    }

}