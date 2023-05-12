﻿using System.Collections.Generic;
using UnityEngine;

namespace TrekVRApplication
{

    [DisallowMultipleComponent]
    public class UserInterfaceManager : SingletonMonoBehaviour<UserInterfaceManager>
    {

        [SerializeField]
        private Material _material;
        public Material UIMaterial
        {
            get => _material;
        }

        [SerializeField]
        private PrimaryXRController _primaryController;
        public PrimaryXRController PrimaryController
        {
            get => _primaryController;
        }

        [SerializeField]
        private SecondaryXRController _secondaryController;
        public SecondaryXRController SecondaryController
        {
            get => _secondaryController;
        }

        [SerializeField]
        protected GameObject _cameraRig;
        public Camera XRCamera
        {
            get => _cameraRig.GetComponentInChildren<Camera>();
        }

        #region BrowserUserInterface instances

        public MainModal MainModal { get; private set; }

        public ControllerModal PrimaryControllerModal { get; private set; }

        public ControllerModal SecondaryControllerModal { get; private set; }

        private readonly ISet<BrowserUserInterface> _browserUserInterfaces = new HashSet<BrowserUserInterface>();

        #endregion
        protected override void Awake()
        {
            base.Awake();

            GameObject gameObject = new GameObject(typeof(MainModal).Name);
            MainModal = gameObject.AddComponent<MainModal>();

            if (_primaryController)
            {
                gameObject = new GameObject(typeof(ControllerModal).Name);
                gameObject.transform.parent = _primaryController.transform;
                PrimaryControllerModal = gameObject.AddComponent<PrimaryControllerModal>();
            }

            if (_secondaryController)
            {
                gameObject = new GameObject(typeof(ControllerModal).Name);
                gameObject.transform.parent = _secondaryController.transform;
                SecondaryControllerModal = gameObject.AddComponent<SecondaryControllerModal>();
            }

        }

        public void RegisterBrowserUserInterface(BrowserUserInterface ui)
        {
            if (_browserUserInterfaces.Contains(ui))
            {
                Debug.LogError($"BrowserUserInterface instance already registered ({ui.GetType().Name})");
            }
            _browserUserInterfaces.Add(ui);
        }

        public void HideControllerModals()
        {
            PrimaryControllerModal.StartActivity(ControllerModalActivity.Default);
            SecondaryControllerModal.StartActivity(ControllerModalActivity.Default);
        }

        public void HideControllerModalsWithActivity(ControllerModalActivity activity)
        {
            if (PrimaryControllerModal.CurrentActivity == activity)
            {
                PrimaryControllerModal.StartActivity(ControllerModalActivity.Default);
            }
            if (SecondaryControllerModal.CurrentActivity == activity)
            {
                SecondaryControllerModal.StartActivity(ControllerModalActivity.Default);
            }
        }

        public ControllerModal GetControllerModalWithActivity(ControllerModalActivity activity)
        {
            if (PrimaryControllerModal.CurrentActivity == activity)
            {
                return PrimaryControllerModal;
            }
            if (SecondaryControllerModal.CurrentActivity == activity)
            {
                return SecondaryControllerModal;
            }
            return null;
        }

    }

}
