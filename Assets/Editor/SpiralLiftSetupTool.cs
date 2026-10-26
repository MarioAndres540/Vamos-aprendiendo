#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VamosAprendiendo.Gameplay;

namespace VamosAprendiendo.EditorTools
{
    /// <summary>
    /// Convierte las escaleras en espiral de BaseScene en ascensores: el escalón más bajo de cada espiral pasa a ser una
    /// plataforma única que sube al jugador recorriendo la curva que forman los escalones. Los demás escalones se
    /// conservan (se pueden ocultar con el segundo menú).
    /// </summary>
    public static class SpiralLiftSetupTool
    {
        private const string BASE_SCENE_PATH = "Assets/_Project/Scenes/BaseScene.unity";
        private const float LinkDistance = 7f; // escalones a menos de 7 m (3D) pertenecen a la misma espiral

        private class Step
        {
            public Transform transform;
            public Renderer renderer;
            public Vector3 center;
        }

        [MenuItem("Tools/Scenes/Spiral Stairs - Add Lifts (BaseScene)", false, 26)]
        public static void AddLifts()
        {
            Scene scene = OpenBaseScene();
            RevertExistingLifts(); // permite repetir el menú sin duplicar plataformas

            var steps = CollectSteps();
            if (steps.Count == 0)
            {
                Debug.LogError("[SpiralLift] No se encontraron escalones ('Escalon*') en BaseScene.");
                return;
            }

            var spirals = Cluster(steps);
            int index = 1;
            foreach (var spiral in spirals)
            {
                spiral.Sort((a, b) => a.center.y.CompareTo(b.center.y));
                if (spiral.Count < 3) continue; // escalones sueltos que no forman una espiral

                var carrier = spiral[0].transform.gameObject;
                var path = BuildExtendedPath(spiral);
                PrepareCarrier(carrier, path, index);
                Debug.Log($"[SpiralLift] Espiral {index}: {spiral.Count} escalones, de y={spiral[0].center.y:F1} a y={spiral[spiral.Count - 1].center.y:F1}.");
                index++;
            }

            // Una sola escalera: los demás escalones se ocultan (se pueden volver a mostrar con el menú).
            SetExtraStepsActive(false);
        }

        [MenuItem("Tools/Scenes/Spiral Stairs - Hide Extra Steps", false, 27)]
        public static void HideExtraSteps() => SetExtraStepsActive(false);

        [MenuItem("Tools/Scenes/Spiral Stairs - Show All Steps", false, 28)]
        public static void ShowAllSteps() => SetExtraStepsActive(true);

        private static void SetExtraStepsActive(bool active)
        {
            Scene scene = OpenBaseScene();
            int changed = 0;
            foreach (var step in CollectSteps())
            {
                var go = step.transform.gameObject;
                if (go.GetComponent<SpiralLift>() != null) continue; // la plataforma elevadora siempre se queda
                if (go.activeSelf != active) { go.SetActive(active); changed++; }
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[SpiralLift] {changed} escalones {(active ? "mostrados" : "ocultos")}.");
        }

        // Devuelve cada plataforma elevadora a su estado de escalón normal (posición, nombre y colisión de malla).
        private static void RevertExistingLifts()
        {
            var lifts = Object.FindObjectsByType<SpiralLift>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var lift in lifts)
            {
                var go = lift.gameObject;
                var r = go.GetComponent<Renderer>();
                var offset = r.bounds.center - go.transform.position;
                if (!string.IsNullOrEmpty(lift.OriginalName))
                {
                    go.transform.position = lift.OriginalCenter - offset;
                    go.name = lift.OriginalName;
                }

                var box = go.GetComponent<BoxCollider>();
                if (box != null) Object.DestroyImmediate(box);
                var body = go.GetComponent<Rigidbody>();
                if (body != null) Object.DestroyImmediate(body);
                Object.DestroyImmediate(lift);

                if (go.GetComponent<Collider>() == null)
                {
                    go.AddComponent<MeshCollider>().sharedMesh = go.GetComponent<MeshFilter>().sharedMesh;
                }
                go.SetActive(true);
            }
        }
        private static Scene OpenBaseScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.path.Contains("BaseScene"))
            {
                scene = EditorSceneManager.OpenScene(BASE_SCENE_PATH, OpenSceneMode.Single);
            }
            return scene;
        }

        private static List<Step> CollectSteps()
        {
            var list = new List<Step>();
            var group = GameObject.Find("plataformas");
            if (group == null) return list;

            foreach (Transform child in group.transform)
            {
                if (!child.name.StartsWith("Escalon")) continue;
                var r = child.GetComponent<Renderer>();
                if (r == null) continue;
                list.Add(new Step { transform = child, renderer = r, center = r.bounds.center });
            }
            return list;
        }

        // Agrupa escalones por cercanía (componentes conexos): cada grupo es una espiral.
        private static List<List<Step>> Cluster(List<Step> steps)
        {
            var parent = new int[steps.Count];
            for (int i = 0; i < parent.Length; i++) parent[i] = i;
            int Find(int x) { while (parent[x] != x) { parent[x] = parent[parent[x]]; x = parent[x]; } return x; }

            for (int i = 0; i < steps.Count; i++)
            {
                for (int j = i + 1; j < steps.Count; j++)
                {
                    if (Vector3.Distance(steps[i].center, steps[j].center) <= LinkDistance)
                    {
                        parent[Find(i)] = Find(j);
                    }
                }
            }

            var groups = new Dictionary<int, List<Step>>();
            for (int i = 0; i < steps.Count; i++)
            {
                int root = Find(i);
                if (!groups.ContainsKey(root)) groups[root] = new List<Step>();
                groups[root].Add(steps[i]);
            }

            var result = new List<List<Step>>(groups.Values);
            result.Sort((a, b) => MinY(a).CompareTo(MinY(b)));
            return result;
        }

        private static float MinY(List<Step> s)
        {
            float m = float.MaxValue;
            foreach (var x in s) m = Mathf.Min(m, x.center.y);
            return m;
        }

        // Une las plataformas: añade un punto antes del primer escalón, sobre la plataforma de abajo, y otro después del
        // último, sobre la plataforma de arriba, para que el jugador suba y baje de la plataforma elevadora sin saltar.
        private static List<Vector3> BuildExtendedPath(List<Step> orderedSpiral)
        {
            var path = new List<Vector3>();
            Vector3 first = orderedSpiral[0].center;
            Vector3 last = orderedSpiral[orderedSpiral.Count - 1].center;

            var bottom = FindPlatformNear(first);
            var top = FindPlatformNear(last);

            if (bottom != null) path.Add(TowardPlatform(first, bottom, first.y));
            foreach (var s in orderedSpiral) path.Add(s.center);
            if (top != null)
            {
                // La plataforma elevadora (0,27 de semialtura) queda 0,15 m por encima del suelo de arriba: se baja con un paso mínimo.
                float y = top.bounds.max.y + 0.27f + 0.15f;
                path.Add(TowardPlatform(last, top, y));
            }
            return path;
        }

        private static readonly string[] PlatformNames =
        {
            "Plataforma_uno", "Plataforma_uno (1)", "Plataforma_uno (2)", "Plataforma_uno (3)", "Plataforma_uno (4)",
            "Plataforma_dos", "Plataforma_tres"
        };

        // Plataforma cuya superficie está a la altura del escalón y que queda más cerca en planta.
        private static Renderer FindPlatformNear(Vector3 p)
        {
            Renderer best = null;
            float bestDistance = float.MaxValue;
            foreach (var n in PlatformNames)
            {
                var go = GameObject.Find(n);
                if (go == null) continue;
                var r = go.GetComponent<Renderer>();
                if (r == null || Mathf.Abs(r.bounds.max.y - p.y) > 3f) continue;

                Vector3 closest = r.bounds.ClosestPoint(new Vector3(p.x, r.bounds.center.y, p.z));
                float d = Vector2.Distance(new Vector2(closest.x, closest.z), new Vector2(p.x, p.z));
                if (d < bestDistance) { bestDistance = d; best = r; }
            }
            return best;
        }

        // Punto 4 m hacia el interior de la plataforma, en dirección a su centro.
        private static Vector3 TowardPlatform(Vector3 from, Renderer platform, float y)
        {
            Vector3 dir = platform.bounds.center - from;
            dir.y = 0f;
            dir = dir.sqrMagnitude > 0.01f ? dir.normalized : Vector3.forward;
            Vector3 p = from + dir * 4f;
            p.y = y;
            return p;
        }
        private static void PrepareCarrier(GameObject carrier, List<Vector3> path, int index)
        {
            string originalName = carrier.name;
            var originalRenderer = carrier.GetComponent<Renderer>();
            Vector3 originalCenter = originalRenderer.bounds.center;
            carrier.name = $"Spiral_Lift_{index}";

            // Colisión simple y estable para una plataforma que se mueve: caja del tamaño de la malla.
            var oldCollider = carrier.GetComponent<Collider>();
            if (oldCollider != null) Object.DestroyImmediate(oldCollider);
            var mesh = carrier.GetComponent<MeshFilter>().sharedMesh;
            var box = carrier.AddComponent<BoxCollider>();
            box.center = mesh.bounds.center;
            box.size = mesh.bounds.size;

            var rb = carrier.GetComponent<Rigidbody>();
            if (rb == null) rb = carrier.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            var lift = carrier.GetComponent<SpiralLift>();
            if (lift == null) lift = carrier.AddComponent<SpiralLift>();

            lift.SetSource(originalName, originalCenter);
            lift.SetWaypoints(path.ToArray());
            EditorUtility.SetDirty(lift);

            // En el Editor la plataforma queda en su punto de partida (en juego se coloca sola al empezar).
            var r = carrier.GetComponent<Renderer>();
            carrier.transform.position = path[0] - (r.bounds.center - carrier.transform.position);
        }
    }
}
#endif
