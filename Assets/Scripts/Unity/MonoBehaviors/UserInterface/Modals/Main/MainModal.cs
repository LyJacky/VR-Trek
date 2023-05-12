﻿using UnityEngine;
using ZenFulcrum.EmbeddedBrowser;
using static TrekVRApplication.ZFBrowserConstants;

namespace TrekVRApplication {

    public class MainModal : XRBrowserUserInterface {

        private const float AngleSweep = 135;
        private const float Radius = 1.0f;
        private const float WorldHeight = 0.69f;
        private const float SpawnDistanceMultiplier = 1.6f;

        /// <summary>
        ///     Vertical resolution of the modal in pixels.
        /// </summary>
        public const int Resolution = 720;

        protected override int Width => Mathf.RoundToInt(Mathf.PI * Radius * AngleSweep / 180 / WorldHeight * Resolution);

        protected override int Height => Resolution;

        protected override bool RegisterToUserInterfaceManager => false;

        private UnityBrowserWebFunctions _webFunctions;
        private UnityBrowserSearchFunctions _searchFunctions;
        private UnityBrowserUserInterfaceFunctions _userInterfaceFunctions;
        private UnityBrowserTerrainModelFunctions _terrainModelFunctions;
        private UnityBrowserLayerManagerFunctions _layerManagerFunctions;

        /// <summary>
        ///     <para>
        ///         Whether the main model is visible.
        ///     </para>
        ///     <para>
        ///         When the main modal's visiblity is set to true, then the mode of the
        ///         current visible model will automatically be set to Disabled.
        ///     </para>
        ///     <para>
        ///         However, when setting the main modal's visiblity to false, the mode of
        ///         the currently visible model must be set manaully.
        ///     </para>
        /// </summary>
        
        public override bool Visible {
            get => _visible;
            set {
                if (value) {
                    OpenModal();
                }
                TerrainModelManager.Instance.TerrainInteractionEnabled = !value;
                base.Visible = value;
            }
        }
        
        protected override string DefaultUrl => BaseUrl;

        private readonly GenerateCylindricalMenuMeshTask _generateMenuMeshTask = 
            new GenerateCylindricalMenuMeshTask(AngleSweep, WorldHeight, Radius);
        protected override GenerateMenuMeshTask GenerateMenuMeshTask => _generateMenuMeshTask;

        #region Unity lifecycle methods
        
        protected override void Awake() {
            transform.localEulerAngles = new Vector3(0, 180);
            base.Awake();
        }
        
        #endregion
        
        protected override void Init(Mesh mesh) {
            base.Init(mesh);
            _webFunctions = new UnityBrowserWebFunctions(Browser);
            _searchFunctions = new UnityBrowserSearchFunctions(Browser);
            _userInterfaceFunctions = new UnityBrowserUserInterfaceFunctions(Browser);
            _terrainModelFunctions = new UnityBrowserTerrainModelFunctions(Browser);
            _layerManagerFunctions = new UnityBrowserLayerManagerFunctions(Browser);
        }

        protected override void OnBrowserLoad(JSONNode loadData) {
            _webFunctions.RegisterFunctions();
            _searchFunctions.RegisterFunctions();
            _userInterfaceFunctions.RegisterFunctions();
            _terrainModelFunctions.RegisterFunctions();
            _layerManagerFunctions.RegisterFunctions();
        }
        

        
        public void NavigateToRootMenu() {
            ZFBrowserUtils.NavigateTo(Browser, $"{MainModalUrl}");
        }
        
        private void OpenModal() {
            Camera eye = UserInterfaceManager.Instance.XRCamera;

            Vector3 menuOrientation = eye.transform.forward;
            menuOrientation.y = 0;
            transform.right = menuOrientation;

            Vector3 menuPosition = eye.transform.position + (SpawnDistanceMultiplier - 1) * Radius * menuOrientation;
            menuPosition.y -= Mathf.Clamp(WorldHeight / 2, 0, float.PositiveInfinity); // TODO Max value
            transform.position = menuPosition;
        }

    }

}
