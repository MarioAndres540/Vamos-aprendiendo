using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Antigravity.MCP
{
    [InitializeOnLoad]
    public static class UnityMcpBridge
    {
        private const int PORT = 8089;
        private static HttpListener _listener;
        private static Thread _listenerThread;
        private static bool _isRunning = false;

        static UnityMcpBridge()
        {
            AssemblyReloadEvents.beforeAssemblyReload += StopServer;
            EditorApplication.quitting += StopServer;
            StartServer();
        }

        [MenuItem("Tools/Antigravity MCP/Start Server")]
        public static void StartServer()
        {
            if (_isRunning) return;

            try
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add($"http://localhost:{PORT}/");
                _listener.Start();
                _isRunning = true;

                _listenerThread = new Thread(ListenLoop)
                {
                    IsBackground = true
                };
                _listenerThread.Start();
                Debug.Log($"[Antigravity MCP] Servidor iniciado en http://localhost:{PORT}/");
            }
            catch (HttpListenerException)
            {
                // El socket ya está en uso por la instancia previa durante el reload
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Antigravity MCP] Aviso iniciando servidor: {ex.Message}");
            }
        }

        [MenuItem("Tools/Antigravity MCP/Stop Server")]
        public static void StopServer()
        {
            if (!_isRunning) return;

            _isRunning = false;
            try
            {
                _listener?.Stop();
                _listenerThread?.Abort();
            }
            catch { }
            Debug.Log("[Antigravity MCP] Servidor detenido.");
        }

        private static void ListenLoop()
        {
            while (_isRunning && _listener != null && _listener.IsListening)
            {
                try
                {
                    var context = _listener.GetContext();
                    ThreadPool.QueueUserWorkItem((_) => ProcessRequest(context));
                }
                catch (HttpListenerException)
                {
                    break;
                }
                catch (ThreadAbortException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[Antigravity MCP] Exception en bucle: {ex.Message}");
                }
            }
        }

        private static T RunOnMainThread<T>(Func<T> action, int timeoutMs = 5000)
        {
            var tcs = new TaskCompletionSource<T>();
            EditorApplication.delayCall += () =>
            {
                try
                {
                    tcs.SetResult(action());
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            };

            if (tcs.Task.Wait(timeoutMs))
            {
                return tcs.Task.Result;
            }
            throw new TimeoutException("Unity Editor no respondió a tiempo. Asegúrate de que la ventana de Unity no esté congelada.");
        }

        private static void ProcessRequest(HttpListenerContext context)
        {
            var req = context.Request;
            var res = context.Response;

            res.Headers.Add("Access-Control-Allow-Origin", "*");
            res.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            res.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

            if (req.HttpMethod == "OPTIONS")
            {
                res.StatusCode = 200;
                res.Close();
                return;
            }

            string responseJson = "{}";
            try
            {
                string path = req.Url.AbsolutePath.ToLowerInvariant();

                // /status y /ping responden de forma inmediata sin esperar al hilo principal
                if (path == "/status" || path == "/ping")
                {
                    string unityVersion = Application.unityVersion;
                    string dataPath = Application.dataPath.Replace("\\", "/");
                    responseJson = $"{{\"status\":\"connected\",\"unity_version\":\"{unityVersion}\",\"project_path\":\"{dataPath}\"}}";
                }
                else if (path == "/refresh")
                {
                    responseJson = RunOnMainThread(() =>
                    {
                        AssetDatabase.Refresh();
                        return "{\"status\":\"refreshed\"}";
                    }, 10000);
                }
                else if (path == "/scene/info")
                {
                    responseJson = RunOnMainThread(() =>
                    {
                        var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                        var rootObjects = activeScene.GetRootGameObjects();
                        var sb = new StringBuilder();
                        sb.Append("{\"scene\":\"" + activeScene.name + "\",\"root_objects\":[");
                        for (int i = 0; i < rootObjects.Length; i++)
                        {
                            if (i > 0) sb.Append(",");
                            sb.Append($"\"{rootObjects[i].name}\"");
                        }
                        sb.Append("]}");
                        return sb.ToString();
                    });
                }
                else if (path == "/scene/organize")
                {
                    responseJson = RunOnMainThread(() =>
                    {
                        Project.EditorTools.SceneHierarchyOrganizer.OrganizeActiveScene();
                        return "{\"status\":\"success\",\"message\":\"Jerarquía organizada correctamente.\"}";
                    }, 5000);
                }
                else if (path == "/scene/generate-login")
                {
                    responseJson = RunOnMainThread(() =>
                    {
                        VamosAprendiendo.EditorTools.LoginSceneGenerator.GenerateLoginScene();
                        return "{\"status\":\"success\",\"message\":\"Escena LoginScene creada exitosamente y configurada en Build Settings.\"}";
                    }, 10000);
                }
                else if (path == "/scene/generate-challenges")
                {
                    responseJson = RunOnMainThread(() =>
                    {
                        VamosAprendiendo.EditorTools.ChallengeScenesCreator.CreateAllChallengeScenes();
                        return "{\"status\":\"success\",\"message\":\"Todas las escenas de desafíos fueron creadas con éxito.\"}";
                    }, 15000);
                }
                else if (path == "/camera/fix")
                {
                    responseJson = RunOnMainThread(() =>
                    {
                        VamosAprendiendo.EditorTools.CameraSetupTool.FixCameraInBaseSceneDirectly();
                        return "{\"status\":\"success\",\"message\":\"Cámara configurada y guardada en BaseScene.\"}";
                    }, 10000);
                }
                else
                {
                    responseJson = "{\"status\":\"ok\",\"message\":\"endpoint received\"}";
                }
            }
            catch (Exception ex)
            {
                responseJson = $"{{\"error\":\"{ex.Message}\"}}";
            }

            byte[] buffer = Encoding.UTF8.GetBytes(responseJson);
            res.ContentType = "application/json";
            res.ContentLength64 = buffer.Length;
            res.OutputStream.Write(buffer, 0, buffer.Length);
            res.OutputStream.Close();
        }
    }
}
