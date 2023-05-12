﻿using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;
using ZenFulcrum.EmbeddedBrowser;
using static TrekVRApplication.ZFBrowserConstants;

namespace TrekVRApplication {

    public class UnityBrowserTerrainModelFunctions : UnityBrowserFunctionSet {
        
        protected override string FunctionsReadyVariable => "terrainFunctionsReady";
        public UnityBrowserTerrainModelFunctions(Browser browser) : base(browser) {
            // TODO Unsubscribe on destroy
            TerrainModelManager.Instance.OnCurrentTerrainModelChange += OnTerrainModelChange;
        }

        public override void RegisterFunctions() {
            OnTerrainModelChange(TerrainModelManager.Instance.CurrentVisibleModel);
            base.RegisterFunctions();
        }

        [RegisterToBrowser]
        public void ShowGlobeModel() {
            TerrainModelManager.Instance.ShowGlobeModel();
        }

        [RegisterToBrowser]
        public void NavigateToCoordinate(string bbox) {
            TerrainModelManager terrainModelManager = TerrainModelManager.Instance;
            XRInteractableGlobeTerrain globe = (XRInteractableGlobeTerrain)terrainModelManager.GlobeModel.InteractionController;
            if (globe) {
                BoundingBox boundingBox = BoundingBoxUtils.ParseBoundingBox(bbox);
                Vector2 latLon = BoundingBoxUtils.MedianLatLon(boundingBox);
                Camera eye = UserInterfaceManager.Instance.XRCamera;
                globe.NavigateTo(latLon, eye.transform.position);
            }
        }

        [RegisterToBrowser]
        public void HighlightBoundingBoxOnGlobe(string bbox) {
            TerrainModelManager terrainModelManager = TerrainModelManager.Instance;
            if (string.IsNullOrEmpty(bbox)) {
                terrainModelManager.ClearHighlightedAreaOnGlobe();
            }
            else {
                BoundingBox boundingBox = BoundingBoxUtils.ParseBoundingBox(bbox);
                terrainModelManager.HighlightAreaOnGlobe(boundingBox);
            }
        }

        [RegisterToBrowser]
        public void CreateLocalTerrainFromBookmark(string bookmarkJson) {
            Bookmark bookmark = JsonConvert.DeserializeObject<Bookmark>(bookmarkJson, JsonConfig.SerializerSettings);
            BoundingBox bbox = BoundingBoxUtils.ParseBoundingBox(bookmark.BoundingBox);
            TerrainProductMetadata baseProductMetadata = new TerrainProductMetadata(bookmark.TexturesUUID[0], bbox, 0);
            // TODO Add additional product layers if present.

            TerrainModelManager terrainModelManager = TerrainModelManager.Instance;
            LocalTerrainModel terrainModel = terrainModelManager.CreateLocalModelFromBookmark(bbox, bookmark.DemUUID, bookmark.TexturesUUID);
            terrainModelManager.ShowTerrainModel(terrainModel);
        }

        [RegisterToBrowser]
        public void GetCurrentViewSettings(string requestId) {
            // TODO Un-hardcode this data
            TerrainModelManager terrainModelManager = TerrainModelManager.Instance;
            XRInteractableGlobeTerrain globe = (XRInteractableGlobeTerrain)terrainModelManager.GlobeModel.InteractionController;
            TerrainModel currentTerrainModel = terrainModelManager.CurrentVisibleModel;
            IDictionary<string, object> settings = new Dictionary<string, object>() {
                { "terrainType", currentTerrainModel is GlobeTerrainModel ? "globe" : "local" },
                { "heightExaggeration", terrainModelManager.HeightExagerration },
                { "textures", terrainModelManager.TerrainTexturesEnabled },
                { "coordinates", globe.EnableCoordinateLines },
                { "locationNames", globe.EnableNomenclatures },
            };
            ZFBrowserUtils.SendDataResponse(_browser, requestId, settings);
        }

        [RegisterToBrowser]
        public void SetHeightExaggeration(double? value) {
            if (value == null) {
                return;
            }
            TerrainModelManager terrainModelManager = TerrainModelManager.Instance;
            terrainModelManager.HeightExagerration = (float)value;
        }

        [RegisterToBrowser]
        public void SetTexturesVisiblity(bool? visible) {
            if (visible == null) {
                return;
            }
            TerrainModelManager terrainModelManager = TerrainModelManager.Instance;
            terrainModelManager.TerrainTexturesEnabled = (bool)visible;
        }

        [RegisterToBrowser]
        public void SetCoordinateIndicatorsVisibility(bool? visible) {
            if (visible == null) {
                return;
            }
            TerrainModelManager terrainModelManager = TerrainModelManager.Instance;
            if (terrainModelManager.GlobeModelIsVisible()) {
                XRInteractableGlobeTerrain globe = (XRInteractableGlobeTerrain)terrainModelManager.GlobeModel.InteractionController;
                globe.EnableCoordinateLines = (bool)visible;
            }
        }

        [RegisterToBrowser]
        public void SetLocationNamesVisibility(bool? visible) {
            if (visible == null) {
                return;
            }
            TerrainModelManager terrainModelManager = TerrainModelManager.Instance;
            if (terrainModelManager.GlobeModelIsVisible()) {
                XRInteractableGlobeTerrain globe = (XRInteractableGlobeTerrain)terrainModelManager.GlobeModel.InteractionController;
                globe.EnableNomenclatures = (bool)visible;
            }
        }

        // TODO Move this somewhere else?
        [RegisterToBrowser]
        public void HideControlPanel() {
            Scenes.MainRoom.MainRoomTerrainControlPanelGroup controlPanelGroup =
                 Scenes.MainRoom.MainRoomTerrainControlPanelGroup.Instance;
            if (!controlPanelGroup) {
                return;
            }
            controlPanelGroup.ActiveControlPanel.MoveDown();
        }

        private void OnTerrainModelChange(TerrainModel terrainModel) {
            string terrainType = terrainModel is GlobeTerrainModel ? "globe" : "local";
            _browser.EvalJS($"{UnityGlobalObjectPath}.onTerrainTypeChange.next('{terrainType}');");
        }
    }

}
