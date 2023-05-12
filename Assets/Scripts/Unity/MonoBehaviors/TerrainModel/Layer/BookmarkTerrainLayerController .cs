﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static TrekVRApplication.TerrainConstants;

namespace TrekVRApplication {

    [DisallowMultipleComponent]
    public class TerrainBookmarkLayerController : TerrainLayerController {

        // NOTE: When modifying the global layers list within this class,
        // access the backing variable directly instead of the property,
        // as using the property accessor will return a new list object.
        private IList<TerrainLayer> _layers = new List<TerrainLayer>();
        public override IList<TerrainLayer> Layers => new List<TerrainLayer>(_layers);

        public override void AddLayer(string productUUID, int? index = null, Action callback = null) {

            // Check if the number of layers is already maxed out.
            if (_layers.Count >= MaxDiffuseLayers) {
                Debug.LogError($"Number of layers cannot exceed {MaxDiffuseLayers}.");
                return;
            }

            // Check if the product has already been added.
            if (_layers.Any(l => l.ProductUUID == productUUID)) {
                Debug.LogWarning($"{productUUID} has already been added as a layer.");
                return;
            }

            CreateLayer(productUUID, layer => {
                // Not thread safe
                if (index == null) {
                    index = _layers.Count;
                }
                _layers.Insert((int)index, layer);
                if (Started) {
                    ReloadTextures(_layers, index == 0);
                }
                callback?.Invoke();
            });
        }

        public override void UpdateLayer(TerrainLayerChange changes, Action callback = null) {
            int index = changes.Index;
            if (index > 0 && index < _layers.Count) {
                bool changed = false;
                TerrainLayer layer = _layers[index];
                if (changes.Opacity != null && layer.Opacity != changes.Opacity) {
                    layer.Opacity = (float)changes.Opacity;
                    changed = true;
                }
                if (changes.Visible != null && layer.Visible != changes.Visible) {
                    layer.Visible = (bool)changes.Visible;
                    changed = true;
                }
                if (changed) {
                    Material.SetFloat($"_Diffuse{index}Opacity", layer.Visible ? layer.Opacity : 0);
                    _layers[index] = layer;
                }
            }
            callback?.Invoke();
        }

        public override void MoveLayer(int from, int to, Action callback = null) {
            if (from >= 0 && from < _layers.Count && to >= 0 && to < _layers.Count && from != to) {
                TerrainLayer temp = _layers[from];
                _layers.RemoveAt(from);
                _layers.Insert(to, temp);
                if (Started) {
                    ReloadTextures(_layers, false);
                }
            }
            callback?.Invoke();
        }

        public override void RemoveLayer(int index, Action callback = null) {
            if (index >= 0 && index < _layers.Count) {
                _layers.RemoveAt(index);
                if (Started) {
                    ReloadTextures(_layers, false);
                }
            }
            callback?.Invoke();
        }

        protected override Material GenerateMaterial() {
            Material material = new Material(Shader.Find("Custom/Terrain/MultiDiffuseOverlay"));
            material.SetFloat("_DiffuseOpacity", 1);
            material.SetFloat("_Glossiness", ShaderSmoothness);
            material.SetFloat("_Metallic", ShaderMetallic);
            material.SetFloat("_OverlayOpacity", 0);
            return material;
        }


    }

}