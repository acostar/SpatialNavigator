using System.Collections.Generic;
using UnityEngine;

namespace Acostar.SpatialNavigator.Samples
{
    /// <summary>Self-contained scene setup. No project tags, input bindings or custom layers required.</summary>
    public sealed class SpatialNavigatorDemo : MonoBehaviour
    {
        private SpatialAgent agent;
        private Vector3 destination;
        private readonly Vector3 left = new Vector3(-8f, -2f, 0f);
        private readonly Vector3 right = new Vector3(8f, 3f, 0f);
        private readonly List<Material> materials = new List<Material>();

        private void Start()
        {
            // Only Default-layer colliders are obstacles. Markers use built-in Ignore Raycast.
            Box("Central obstacle", Vector3.zero, new Vector3(4f, 10f, 8f), new Color(0.25f, 0.32f, 0.42f), true);
            Box("Floor", new Vector3(0f, -11f, 0f), new Vector3(24f, 1f, 24f), new Color(0.12f, 0.16f, 0.23f), true);
            Box("Target A", left, Vector3.one * 0.5f, Color.green, false);
            Box("Target B", right, Vector3.one * 0.5f, Color.yellow, false);
            var volumeObject = new GameObject("Navigation Volume");
            volumeObject.transform.SetParent(transform);
            var volume = volumeObject.AddComponent<SpatialNavigationMesh>();
            volume.Configure(24f, 4, 1 << 0);
            volume.Build();
            var flyer = Box("Flying Agent", left, Vector3.one * 0.4f, Color.cyan, false);
            agent = flyer.AddComponent<SpatialAgent>();
            agent.Configure(volume, 1 << 0, 0.2f);
            destination = right;

            var cameraObject = new GameObject("Demo Camera");
            cameraObject.transform.SetParent(transform);
            var camera = cameraObject.AddComponent<Camera>();
            cameraObject.transform.position = new Vector3(27f, 21f, -32f);
            cameraObject.transform.LookAt(Vector3.zero);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.035f, 0.05f, 0.08f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 150f;
            var lightObject = new GameObject("Demo Light");
            lightObject.transform.SetParent(transform);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            lightObject.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
        }

        private GameObject Box(string label, Vector3 position, Vector3 scale, Color color, bool obstacle)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = label;
            go.transform.SetParent(transform);
            go.transform.position = position;
            go.transform.localScale = scale;
            go.layer = obstacle ? 0 : 2;
            if (!obstacle) { go.GetComponent<Collider>().enabled = false; }
            var pipeline = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
            string shaderName = pipeline == null ? "Unlit/Color" :
                (pipeline.GetType().Name.Contains("HDRenderPipeline") ? "HDRP/Unlit" : "Universal Render Pipeline/Unlit");
            Shader shader = Shader.Find(shaderName);
            if (shader == null) { shader = Shader.Find("Unlit/Color"); }
            if (shader != null)
            {
                var material = new Material(shader);
                if (material.HasProperty("_BaseColor")) { material.SetColor("_BaseColor", color); }
                if (material.HasProperty("_UnlitColor")) { material.SetColor("_UnlitColor", color); }
                if (material.HasProperty("_Color")) { material.SetColor("_Color", color); }
                go.GetComponent<Renderer>().sharedMaterial = material;
                materials.Add(material);
            }
            return go;
        }

        private void Update()
        {
            if (agent == null) { return; }
            if (Vector3.Distance(agent.transform.position, destination) < 0.3f)
            { destination = destination == right ? left : right; }
            agent.Move(destination, 3f);
        }

        private void OnDestroy()
        {
            foreach (var material in materials) { if (material != null) { Destroy(material); } }
        }
    }
}
