﻿using UnityEngine;

namespace TrekVRApplication {

    public static class TerrainConstants {

        public const float TerrainModelScale = 2.5e-7f;

        public const int LocalTerrainDemTargetSize = 512;

        public const int LocalTerrainTextureTargetSize = 1024;

        public const int LocalTerrainPhysicsTargetDownsample = 3;

        public const float PhysicsMeshUpdateDelay = 0.5f;  // In seconds

        public const float ShaderSmoothness = 0.31f;

        public const float ShaderMetallic = 0.31f;

        public const float NoTextureShaderSmoothness = 0.37f;

        public const float NoTextureShaderMetallic = 0.31f;

        public const float GlobeModelLODCoefficient = 0.25f;

        // A value of 4 gives much better accuracy, but also has a much
        // larger delay when updating after rescaling height.
        public const int GlobeModelPhysicsTargetDownsample = 5;

        public const string GlobalMosaicUUID = "8bc9352d-ee73-4d1f-94b8-de5495fd8dfa";

        public const string GlobalDigitalElevationModelUUID = "1cc3cfbb-ac38-46d1-a3df-5fff16ca397e";

        public static readonly Color32 CoordinateIndicatorColor = new Color32(0, 224, 255, 255);

        public static readonly Color32 CoordinateIndicatorStaticColor = new Color32(255, 160, 96, 255);

    }

}
