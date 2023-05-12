using UnityEngine;

namespace TrekVRApplication {

    public class PrimaryXRController : XRController {

        /// <summary>
        ///     Whether there was a hit in the current frame.
        /// </summary>
        private bool _hit;

        /// <summary>
        ///     The hit info for the current frame.
        /// </summary>
        private RaycastHit _hitInfo;

        /// <summary>
        ///     Max distance from the controller to detect hits.
        /// </summary>
        [SerializeField]
        private float _maxInteractionDistance = 20.0f;

        private MainModal MainModal {
            get => UserInterfaceManager.Instance.MainModal;
        }

        [SerializeField]
        private float _speedMultiplier = 0.1f;

        // private bool _padClicked = false;

        private XRInteractableObject _activeObject;

        #region Controller event handlers
        
        protected override void TriggerClickedHandler(object sender) {
             Debug.Log("Trigger Clicked - Primary Controller");
            XRInteractableObject obj = GetInteractableObjectIfHit();
            if (obj && obj.triggerDown) {
                obj.OnTriggerDown(this, _hitInfo);
            }
            base.TriggerClickedHandler(sender);
        }

        protected override void TriggerUnclickedHandler(object sender) {
            XRInteractableObject obj = GetInteractableObjectIfHit();
            if (obj && obj.triggerUp) {
                obj.OnTriggerUp(this, _hitInfo);
            }
            base.TriggerUnclickedHandler(sender);
        }

        protected override void TriggerDoubleClickedHandler(object sender) {
            XRInteractableObject obj = GetInteractableObjectIfHit();
            if (obj && obj.triggerDoubleClick) {
                obj.OnTriggerDoubleClick(this, _hitInfo);
            }
            base.TriggerDoubleClickedHandler(sender);
        }

        // protected override void PadClickedHandler(object sender) {
        //     //Debug.Log("Pad clicked at (" + e.padX + ", " + e.padY + ")");
        //     _padClicked = true;
        //     XRInteractableObject obj = GetInteractableObjectIfHit();
        //     if (obj && obj.padClick) {
        //         obj.OnPadDown(this, _hitInfo);
        //     }
        //     base.PadClickedHandler(sender);
        // }

        // protected override void PadUnclickedHandler(object sender) {
        //     //Debug.Log("Pad unclicked at (" + e.padX + ", " + e.padY + ")");
        //     _padClicked = false;
        //     XRInteractableObject obj = GetInteractableObjectIfHit();
        //     if (obj && obj.padClick) {
        //         obj.OnPadUp(this, _hitInfo);
        //     }
        //     base.PadUnclickedHandler(sender);
        // }

        // protected override void PadTouchedHandler(object sender) {
            
        //     XRInteractableObject obj = GetInteractableObjectIfHit();
        //     if (obj && obj.padTouch) {
        //         obj.OnPadTouch(this, _hitInfo);
        //     }   
        //     base.PadTouchedHandler(sender);
        // }

        // protected override void PadUntouchedHandler(object sender) {
        //     //Debug.Log("Pad untouched at (" + e.padX + ", " + e.padY + ")");
        //     XRInteractableObject obj = GetInteractableObjectIfHit();
        //     if (obj && obj.padTouch) {
        //         obj.OnPadUntouch(this, _hitInfo);
        //     }
        //     base.PadUntouchedHandler(sender);
        // }

        // protected override void PadSwipeHandler(object sender) {
        //     //Debug.Log("Pad input received at (" + e.padX + ", " + e.padY + ")");
        //     XRInteractableObject obj = GetInteractableObjectIfHit();
        //     if (obj && obj.padTouch) {
        //         obj.OnPadSwipe(this, _hitInfo);
        //     }
        //     //base.PadSwipeHandler(sender);
        // }

        protected override void GrippedHandler(object sender) {
            XRInteractableObject obj = GetInteractableObjectIfHit();
            if (obj && obj.gripDown) {
                // TODO Verify sender class.
                obj.OnGripDown(this, _hitInfo);
            }
            //base.GrippedHandler(sender);
        }

        protected override void UngrippedHandler(object sender) {
            XRInteractableObject obj = GetInteractableObjectIfHit();
            if (obj && obj.gripUp) {
                // TODO Verify sender class.
                obj.OnGripUp(this, _hitInfo);
            }
            base.UngrippedHandler(sender);
        }

        // TODO Move this event handler to MainModal.cs
        protected override void MenuButtonPressedHandler(object sender) {
            UserInterfaceManager userInterfaceManager = UserInterfaceManager.Instance;
            MainModal mainModal = userInterfaceManager.MainModal;
            if (mainModal.Visible) {
                mainModal.XRBrowser.RegisterKeyPress(KeyCode.M);
            }
            else {
                userInterfaceManager.SecondaryControllerModal.StartActivity(ControllerModalActivity.Default);
                mainModal.Visible = true;
            }
        }

        protected override void MenuButtonLongPressedHandler(object sender) {
            UserInterfaceManager userInterfaceManager = UserInterfaceManager.Instance;
            MainModal mainModal = userInterfaceManager.MainModal;
            if (mainModal.Visible) {

                XRInteractableTerrain interactableTerrain = 
                    TerrainModelManager.Instance.GetComponentFromCurrentModel<XRInteractableTerrain>();
                interactableTerrain.SwitchToActivity(XRInteractableTerrainActivity.Default);

                mainModal.Visible = false;
                mainModal.NavigateToRootMenu();
            }
            else {
                userInterfaceManager.SecondaryControllerModal.StartActivity(ControllerModalActivity.Default);
                mainModal.Visible = true;
            }
        }

        #endregion
        
        protected override void Awake() {
            base.Awake();
            LaserPointer.MaxDistance = _maxInteractionDistance;
        }
        
        protected override void Update() {

            base.Update();

            // Update player movement
            // if (_padClicked) {

            //     // Get the pad position from the controller device.
            //     SteamVR_Controller.Device device = SteamVR_Controller.Input((int)Controller.controllerIndex);
            //     Vector2 axis = device.GetAxis();

            //     // Move the player based on controller direction and pad position.
            //     // Movement is limited along the xz-plane.
            //     Transform cameraRig = UserInterfaceManager.Instance.XRCamera.transform.parent;
            //     Vector3 direction = Vector3.Scale(transform.forward, new Vector3(1, 0, 1));
            //     cameraRig.position += (axis.y > 0 ? 1 : -1) * _speedMultiplier * direction;

            // }

            // Calculate the raycast hit for the current frame.
            RaycastHit hitInfo;
            _hit = Physics.Raycast(transform.position, transform.forward, out hitInfo, _maxInteractionDistance);
            _hitInfo = hitInfo;

            XRInteractableObject obj = GetInteractableObjectIfHit();
            if (obj) {
                if (_activeObject != obj) {

                    // If there was previously a different object being hovered over, then
                    //  we send a OnCursorLeave event to it before setting the new object
                    if (_activeObject) {
                        _activeObject.OnCursorLeave(this, _hitInfo);
                    }

                    if (obj) {
                        obj.OnCursorEnter(this, _hitInfo);
                    }

                    _activeObject = obj;
                }

                if (_activeObject) {
                    _activeObject.OnCursorOver(this, _hitInfo);
                    LaserPointer.Distance = _hitInfo.distance;
                }
                else {
                    LaserPointer.Distance = float.PositiveInfinity;
                }
            }
            else {
                // TODO Send a cursor leave event
                _activeObject = null;
                LaserPointer.Distance = float.PositiveInfinity;
            }

        }

        private XRInteractableObject GetInteractableObjectIfHit() {
            if (!_hit) {
                return null;
            }
            return _hitInfo.transform.GetComponent<XRInteractableObject>();
        }

    }

}