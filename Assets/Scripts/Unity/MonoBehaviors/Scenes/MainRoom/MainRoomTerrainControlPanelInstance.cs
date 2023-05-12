﻿using System;
using UnityEngine;

namespace TrekVRApplication.Scenes.MainRoom
{

    [DisallowMultipleComponent]
    public class MainRoomTerrainControlPanelInstance : MonoBehaviour
    {

        private const float ScreenRotationSpeed = 234.56f;

        private const float ScreenRotationDelay = 0.420f;

        private const float VerticalMovementDuration = 1.337f;

        private const float VerticalMovementDistance = 0.69f;

        [SerializeField]
        private XRSubscribableCollider _supports;

        [SerializeField]
        private XRSubscribableCollider _screenAssembly;

        private TerrainControlPanel _screen;

        public bool Active { get; private set; } = false;

        /// <summary>
        ///     True if up, false if down.
        /// </summary>
        public bool Position { get; private set; } = false;

        private float _screenTargetRotation = -180;

        private bool _screenAnimationInProgress = false;

        private float _verticalAnimationProgress = 1f;

        private void Awake()
        {
            if (_supports)
            {
                _supports.OnColliderClicked += ColliderClickedHandler;
            }
            if (_screenAssembly)
            {
                _screenAssembly.OnColliderClicked += ColliderClickedHandler;
            }
        }

        private void Update()
        {
            if (_verticalAnimationProgress < 1f)
            {
                Vector3 position = transform.localPosition;
                float delta = Time.deltaTime / VerticalMovementDuration * VerticalMovementDistance;
                if (Position)
                {
                    position.y = MathUtils.Clamp(position.y + delta, 0, VerticalMovementDistance);
                    _verticalAnimationProgress = position.y / VerticalMovementDistance;
                }
                else
                {
                    position.y = MathUtils.Clamp(position.y - delta, 0, VerticalMovementDistance);
                    _verticalAnimationProgress = 1 - position.y / VerticalMovementDistance;
                }
                transform.localPosition = position;
                if (_verticalAnimationProgress >= ScreenRotationDelay && !_screenAnimationInProgress)
                {
                    _screenAnimationInProgress = true;
                }
            }
            if (_screenAnimationInProgress)
            {
                Vector3 rotation = _screenAssembly.transform.localEulerAngles;
                float delta = Time.deltaTime * ScreenRotationSpeed;
                if (rotation.z < _screenTargetRotation)
                {
                    rotation.z += delta;
                    if (rotation.z >= _screenTargetRotation)
                    {
                        rotation.z = _screenTargetRotation;
                        _screenAnimationInProgress = false;
                    }
                }
                else
                {
                    rotation.z -= delta;
                    if (rotation.z <= _screenTargetRotation)
                    {
                        rotation.z = _screenTargetRotation;
                        _screenAnimationInProgress = false;
                    }
                }
                _screenAssembly.transform.localEulerAngles = rotation;
            }
        }

        private void OnDestroy()
        {
            if (_supports)
            {
                _supports.OnColliderClicked -= ColliderClickedHandler;
            }
            if (_screenAssembly)
            {
                _screenAssembly.OnColliderClicked -= ColliderClickedHandler;
            }
        }

        public void Activate(TerrainControlPanel screen)
        {
            _screen = screen;
            _screen.transform.SetParent(_screenAssembly.transform, false);
            Active = true;
            MoveUp();
        }

        public void Deactivate()
        {
            Active = false;
            if (!Position)
            {
                _screenTargetRotation = 180;
                _screenAnimationInProgress = true;
            }
            else
            {
                MoveDown();
            }
        }

        public void MoveUp()
        {
            Position = true;
            Vector3 position = transform.localPosition;
            _verticalAnimationProgress = position.y / VerticalMovementDistance;
            _screenTargetRotation = 315;
            _screenAssembly.GetComponent<Collider>().enabled = false;
        }

        public void MoveDown()
        {
            Position = false;
            Vector3 position = transform.localPosition;
            _verticalAnimationProgress = 1 - position.y / VerticalMovementDistance;
            _screenTargetRotation = Active ? 359.9f : 180;
            _screenAssembly.GetComponent<Collider>().enabled = true;
        }

        public void SetEnabled(bool enabled)
        {
            this.enabled = enabled;

            _supports.GetComponent<Collider>().enabled = enabled;
            _screenAssembly.GetComponent<Collider>().enabled = enabled && !Position;

            // Move the screen assembly without animations.
            // This is temporary; the final implementation should
            // make the screen assembly semi-transparent instead. 
            Vector3 position = transform.localPosition;
            Vector3 rotation = _screenAssembly.transform.localEulerAngles;
            if (enabled)
            {
                if (Active)
                {
                    position.y = Position ? VerticalMovementDistance : 0;
                    rotation.z = Position ? 315 : 359.9f;
                }
            }
            else
            {
                position.y = 0;
                rotation.z = 180;
            }
            transform.localPosition = position;
            _screenAssembly.transform.localEulerAngles = rotation;
        }

        private void ColliderClickedHandler()
        {
            if (!enabled)
            {
                return;
            }
            if (Active)
            {
                if (Position)
                {
                    MoveDown();
                }
                else
                {
                    MoveUp();
                }
            }
            else
            {
                MainRoomTerrainControlPanelGroup.Instance.ActivateControlPanel(this);
            }
        }

    }

}
