using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using VamosAprendiendo.Challenges.Tracing;

namespace VamosAprendiendo.Gameplay
{
    /// <summary>
    /// Cuando una tarjeta de "Tu ruta" elige una actividad, al volver al mundo el personaje aparece frente al portal de esa
    /// actividad y camina hacia él; al cruzarlo, <see cref="PortalTrigger"/> abre el reto. Se crea solo (no hay que
    /// añadirlo a la escena) y solo actúa si <see cref="ActivityRoute.PendingPortalScene"/> tiene valor.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class PortalAutoWalker : MonoBehaviour
    {
        private const float ApproachDistance = 6f;
        private const float StartDelay = 0.9f;
        private const float MaxWalkSeconds = 8f;
        private const float GroundTolerance = 2.5f;

        private PlayerController _player;
        private PortalTrigger _portal;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Hook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (string.IsNullOrEmpty(ActivityRoute.PendingPortalScene)) return;

            var player = FindFirstObjectByType<PlayerController>();
            if (player == null) return;

            string target = ActivityRoute.TakePendingPortal();
            PortalTrigger portal = null;
            foreach (var p in FindObjectsByType<PortalTrigger>(FindObjectsSortMode.None))
                if (p.TargetSceneName == target) portal = p;

            if (portal == null)
            {
                ActivityRoute.Case = TraceCase.Both;
                return;
            }

            var walker = new GameObject("PortalAutoWalker").AddComponent<PortalAutoWalker>();
            walker._player = player;
            walker._portal = portal;
        }

        private IEnumerator Start()
        {
            // Un fotograma de espera: así corre después del Start del jugador y de su cámara.
            yield return null;

            Vector3 portalPos = _portal.transform.position;
            Vector3 spot = FindApproachPoint(portalPos, _player.transform.position);
            Vector3 toPortal = portalPos - spot;
            toPortal.y = 0f;

            var controller = _player.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            _player.transform.SetPositionAndRotation(spot + Vector3.up * 0.1f, Quaternion.LookRotation(toPortal));
            if (controller != null) controller.enabled = true;
            _player.ResetVerticalVelocity();

            var cam = Camera.main != null ? Camera.main.GetComponent<SmoothThirdPersonCamera>() : null;
            if (cam != null) cam.SetTarget(_player.transform);

            yield return new WaitForSeconds(StartDelay);

            float elapsed = 0f;
            while (_portal != null && elapsed < MaxWalkSeconds)
            {
                elapsed += Time.deltaTime;
                _player.SetInputMovement(CameraRelative(portalPos - _player.transform.position));
                yield return null;
            }

            // No llegó a entrar: se limpia lo que dejó la tarjeta para no afectar a otro portal.
            _player.SetInputMovement(Vector2.zero);
            ActivityRoute.Case = TraceCase.Both;
            Destroy(gameObject);
        }

        // El jugador se mueve respecto a la cámara, así que se convierte la dirección del mundo a ese marco.
        private static Vector2 CameraRelative(Vector3 worldDirection)
        {
            worldDirection.y = 0f;
            if (worldDirection.sqrMagnitude < 0.0001f) return Vector2.zero;
            worldDirection.Normalize();

            Transform cam = Camera.main != null ? Camera.main.transform : null;
            if (cam == null) return new Vector2(worldDirection.x, worldDirection.z);

            Vector3 forward = cam.forward;
            Vector3 right = cam.right;
            forward.y = right.y = 0f;
            forward.Normalize();
            right.Normalize();
            return new Vector2(Vector3.Dot(worldDirection, right), Vector3.Dot(worldDirection, forward));
        }

        // Punto firme a unos metros del portal, preferiblemente del lado desde donde estaba el jugador.
        private static Vector3 FindApproachPoint(Vector3 portalPos, Vector3 from)
        {
            Vector3 preferred = from - portalPos;
            preferred.y = 0f;
            if (preferred.sqrMagnitude < 0.01f) preferred = Vector3.back;
            preferred.Normalize();

            Vector3 best = portalPos + preferred * ApproachDistance;
            float bestScore = float.MinValue;
            for (int i = 0; i < 8; i++)
            {
                Vector3 dir = Quaternion.Euler(0f, i * 45f, 0f) * Vector3.forward;
                Vector3 candidate = portalPos + dir * ApproachDistance;
                if (!TryGround(candidate, portalPos.y, out Vector3 ground)) continue;

                // El camino hasta el portal no debe tener nada sólido en medio.
                Vector3 chest = ground + Vector3.up * 1.2f;
                Vector3 toPortal = portalPos - chest;
                if (Physics.Raycast(chest, toPortal.normalized, toPortal.magnitude - 1.5f, ~0, QueryTriggerInteraction.Ignore)) continue;

                float score = Vector3.Dot(dir, preferred);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = ground;
                }
            }
            return best;
        }

        private static bool TryGround(Vector3 point, float portalY, out Vector3 ground)
        {
            ground = point;
            Vector3 origin = new Vector3(point.x, portalY + 3f, point.z);
            if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 12f, ~0, QueryTriggerInteraction.Ignore)) return false;
            if (hit.point.y < portalY - GroundTolerance - 1.5f || hit.point.y > portalY) return false;
            ground = hit.point;
            return true;
        }
    }
}
