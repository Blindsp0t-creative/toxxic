using UnityEngine;

namespace VertexField.VoluSmokeFX
{
    /// <summary>
    /// Attach to any object that should paint into ALL FlowmapReceiver instances in the scene.
    /// Automatically registers/unregisters this transform with each receiver's additionalReceivers list.
    /// </summary>
    [DefaultExecutionOrder(49)] // Register before FlowmapReceiver (50) runs.
    public class FlowmapGlobalPainter : MonoBehaviour
    {
        [Tooltip("Continuously re-scan the scene so newly spawned FlowmapReceivers also get this source.")]
        public bool autoRefresh = true;

        [Tooltip("Seconds between re-scans when autoRefresh is enabled.")]
        [Min(0.05f)] public float refreshInterval = 1f;

        float refreshTimer;

        void OnEnable()
        {
            refreshTimer = 0f;
            RegisterToAllReceivers();
        }

        void OnDisable()
        {
            UnregisterFromAllReceivers();
        }

        void Update()
        {
            if (!autoRefresh) return;

            refreshTimer -= Time.deltaTime;
            if (refreshTimer <= 0f)
            {
                refreshTimer = Mathf.Max(0.05f, refreshInterval);
                RegisterToAllReceivers();
            }
        }

        static FlowmapReceiver[] FindAllReceivers()
        {
#if UNITY_6000_5_OR_NEWER
            return Object.FindObjectsByType<FlowmapReceiver>(FindObjectsInactive.Include);
#else
            return Object.FindObjectsByType<FlowmapReceiver>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#endif
        }

        void RegisterToAllReceivers()
        {
            var receivers = FindAllReceivers();
            for (int i = 0; i < receivers.Length; i++)
            {
                var r = receivers[i];
                if (!r) continue;

                if (r.additionalReceivers == null)
                    r.additionalReceivers = new System.Collections.Generic.List<Transform>();

                if (!r.additionalReceivers.Contains(transform))
                    r.additionalReceivers.Add(transform);
            }
        }

        void UnregisterFromAllReceivers()
        {
            var receivers = FindAllReceivers();
            for (int i = 0; i < receivers.Length; i++)
            {
                var r = receivers[i];
                if (!r || r.additionalReceivers == null) continue;
                r.additionalReceivers.Remove(transform);
            }
        }
    }
}
