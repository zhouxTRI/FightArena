using FightArena.Core.Input;
using UnityEngine;

namespace FightArena.Gameplay.CameraSystem
{
    [RequireComponent(typeof(Camera))]
    public sealed class CameraFollow2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform target;
        [SerializeField] private InputRouter inputRouter;

        [Header("Follow")]
        [SerializeField, Min(0f)] private float smoothTime = 0.15f;
        [SerializeField, Min(0.1f)] private float orthographicSize = 7.5f;

        [Header("Look Ahead")]
        [SerializeField, Min(0f)] private float lookAheadFactor = 0.08f;
        [SerializeField, Min(0f)] private float maxLookAhead = 0.8f;

        [Header("Arena Bounds")]
        [SerializeField] private Vector2 arenaMin = new Vector2(-24f, -14f);
        [SerializeField] private Vector2 arenaMax = new Vector2(24f, 14f);

        private Camera targetCamera;
        private Vector2 currentFocus;
        private Vector2 focusVelocity;
        private float initialZ;
        private bool initialized;

        private void Awake()
        {
            targetCamera = GetComponent<Camera>();
            initialZ = transform.position.z;

            targetCamera.orthographic = true;
            targetCamera.orthographicSize = orthographicSize;

            if (target == null)
            {
                Debug.LogError(
                    "CameraFollow2D requires a player target.",
                    this);
                enabled = false;
                return;
            }

            currentFocus = target.position;
            initialized = true;
        }

        private void LateUpdate()
        {
            if (!initialized || target == null)
            {
                return;
            }

            Vector2 desiredFocus = target.position;

            if (inputRouter != null)
            {
                Vector2 aimPosition =
                    inputRouter.CurrentInputIntent.AimWorldPosition;

                Vector2 lookOffset =
                    (aimPosition - desiredFocus) * lookAheadFactor;

                lookOffset = Vector2.ClampMagnitude(
                    lookOffset,
                    maxLookAhead);

                desiredFocus += lookOffset;
            }

            desiredFocus = ClampToArena(desiredFocus);

            if (smoothTime <= 0f)
            {
                currentFocus = desiredFocus;
                focusVelocity = Vector2.zero;
            }
            else
            {
                currentFocus = Vector2.SmoothDamp(
                    currentFocus,
                    desiredFocus,
                    ref focusVelocity,
                    smoothTime,
                    Mathf.Infinity,
                    Time.deltaTime);
            }

            transform.position = new Vector3(
                currentFocus.x,
                currentFocus.y,
                initialZ);
        }

        private Vector2 ClampToArena(Vector2 desired)
        {
            float halfHeight = targetCamera.orthographicSize;
            float halfWidth = halfHeight * targetCamera.aspect;

            Vector2 halfView = new Vector2(halfWidth, halfHeight);
            Vector2 minCenter = arenaMin + halfView;
            Vector2 maxCenter = arenaMax - halfView;

            Vector2 arenaCenter = (arenaMin + arenaMax) * 0.5f;

            if (minCenter.x > maxCenter.x)
            {
                desired.x = arenaCenter.x;
            }
            else
            {
                desired.x = Mathf.Clamp(
                    desired.x,
                    minCenter.x,
                    maxCenter.x);
            }

            if (minCenter.y > maxCenter.y)
            {
                desired.y = arenaCenter.y;
            }
            else
            {
                desired.y = Mathf.Clamp(
                    desired.y,
                    minCenter.y,
                    maxCenter.y);
            }

            return desired;
        }

        private void OnValidate()
        {
            smoothTime = Mathf.Max(0f, smoothTime);
            orthographicSize = Mathf.Max(0.1f, orthographicSize);
            lookAheadFactor = Mathf.Max(0f, lookAheadFactor);
            maxLookAhead = Mathf.Max(0f, maxLookAhead);
        }
    }
}
