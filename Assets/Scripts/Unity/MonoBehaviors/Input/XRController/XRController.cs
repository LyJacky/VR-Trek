using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace TrekVRApplication {

    public abstract class XRController : MonoBehaviour {

        private bool _padTouched = false;

        private readonly DoubleKeyPressTimer _triggerDoubleClickTimer = new DoubleKeyPressTimer();

        private readonly LongKeyPressTimer _menuButtonLongPressTimer = new LongKeyPressTimer();

        private bool _menuButtonClicked = false;

        public XRControllerLaserPointer LaserPointer { get; protected set; }

        //public SteamVR_TrackedController Controller { get; private set; }

        public CustomController Controller{ get; private set;}

        #region External event handlers
        
        public event Action<object> OnTriggerClicked = (sender) => {};
        public event Action<object> OnTriggerUnclicked = (sender) => {};
        public event Action<object> OnTriggerDoubleClicked = (sender) => {};
        public event Action<object> OnPadClicked = (sender) => {};
        public event Action<object> OnPadUnclicked = (sender) => {};
        public event Action<object> OnMenuButtonPressed = (sender) => {};
        public event Action<object> OnMenuButtonLongPressed = (sender) => {};
        public event Action<object> OnGripped = (sender) => {};
        public event Action<object> OnUngripped = (sender) => {};
        public event Action<object> OnPadTouched = (sender) => {};
        public event Action<object> OnPadUntouched = (sender) => {};
        public event Action<object> OnPadSwipe = (sender) => {};
        
        #endregion
        
        protected virtual void Awake() {
            _triggerDoubleClickTimer.OnActionSuccess += TriggerDoubleClickedInternal;
            _menuButtonLongPressTimer.OnActionSuccess += MenuButtonLongPressInternal;

            LaserPointer = GetComponent<XRControllerLaserPointer>();
        }
        
        private void OnEnable() {
            //Controller = GetComponent<SteamVR_TrackedController>();


            Controller = GetComponent<CustomController>();


        
            //testing xri as trigger
            Controller.TriggerClicked += TriggerClickedHandler;
            Controller.TriggerUnclicked += TriggerUnclickedHandler;
            // Controller.PadClicked += PadClickedHandler;
            // Controller.PadUnclicked += PadUnclickedHandler;
            Controller.MenuButtonClicked += MenuButtonClickedInternal;
            Controller.MenuButtonUnclicked += MenuButtonUnclickedInternal;
            Controller.Gripped += GrippedHandler;
            Controller.Ungripped += UngrippedHandler;
        }



        private void OnDisable() {

            //testing xri trigger
            Controller.TriggerClicked -= TriggerClickedHandler;
            Controller.TriggerUnclicked -= TriggerUnclickedHandler;
            // Controller.PadClicked -= PadClickedHandler;
            // Controller.PadUnclicked -= PadUnclickedHandler;
            Controller.MenuButtonClicked -= MenuButtonClickedInternal;
            Controller.MenuButtonUnclicked -= MenuButtonUnclickedInternal;
            Controller.Gripped -= GrippedHandler;
            Controller.Ungripped -= UngrippedHandler;
           
        }
        
        private void OnDestroy() {

            // Does this need to be unsubscribed, since the timer
            // instances are only referenced in this object, so it
            // will be destroyed anyways?
            _triggerDoubleClickTimer.OnActionSuccess -= TriggerDoubleClickedInternal;
            _menuButtonLongPressTimer.OnActionSuccess -= MenuButtonLongPressInternal;
        }

        // Implementing classes should make a super call to this method if
        // it is overwritten.
        
        protected virtual void Update() {
            //removed pad update


            _menuButtonLongPressTimer.Update();
        }

        #region Internally used event handlers

        private void MenuButtonClickedInternal(object sender) {
            _menuButtonClicked = true;
            _menuButtonLongPressTimer.RegisterKeyDown();
        }

        private void MenuButtonUnclickedInternal(object sender) {
            if (_menuButtonClicked) {
                _menuButtonClicked = false;
                MenuButtonPressedHandler(sender);
            }
            _menuButtonLongPressTimer.RegisterKeyUp();
        }

        private void MenuButtonLongPressInternal() {
            _menuButtonClicked = false;
            MenuButtonLongPressedHandler(Controller);
        }

        private void TriggerDoubleClickedInternal() {
            TriggerDoubleClickedHandler(Controller);
        }

        #endregion

        protected virtual void TriggerClickedHandler(object sender) {
            //_triggerDoubleClickTimer.RegisterKeyDown();
             Debug.Log("right trigger, xrcontroller override");
            OnTriggerClicked(sender);
        }

        protected virtual void TriggerUnclickedHandler(object sender) {
            _triggerDoubleClickTimer.RegisterKeyUp();
            OnTriggerUnclicked(sender);
        }

        protected virtual void TriggerDoubleClickedHandler(object sender) {
            OnTriggerDoubleClicked(sender);
        }

        // protected virtual void PadClickedHandler(object sender) {
        //     OnPadClicked(sender);
        // }

        // protected virtual void PadUnclickedHandler(object sender) {
        //     OnPadUnclicked(sender);
        // }

        protected virtual void MenuButtonPressedHandler(object sender) {
            OnMenuButtonPressed(sender);
        }

        protected virtual void MenuButtonLongPressedHandler(object sender) {
            OnMenuButtonLongPressed(sender);
        }

        protected virtual void GrippedHandler(object sender) {
            OnGripped(sender);
        }

        protected virtual void UngrippedHandler(object sender) {
            OnUngripped(sender);
        }

        // Pad touch handlers are called from this class instead of being 
        // registered to SteamVR_TrackedController as event handlers.

        // protected virtual void PadTouchedHandler(object sender) {
        //     OnPadTouched(sender);
        // }

        // protected virtual void PadUntouchedHandler(object sender) {
        //     OnPadUntouched(sender);
        // }

        // protected virtual void PadSwipeHandler(object sender, ClickedEventArgs clickedEventArgs) {
        //     OnPadSwipe(sender);
        // }

    }

}