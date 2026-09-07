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

        private static T RunOnMainThread<T>(Func<T> action, int timeoutMs = 2000)
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

            // Forzar ciclo de actualización del editor si está en segundo plano
            EditorApplication.QueuePlayerLoopUpdate();

            if (tcs.Task.Wait(timeoutMs))
            {
                return tcs.Task.Result;
            }
            throw new TimeoutException("Unity Editor está en pausa o en segundo plano. Haz clic en la ventana de Unity para activar el hilo principal.");
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
                    EditorApplication.delayCall += () =>
                    {
                        AssetDatabase.Refresh();
                    };
                    EditorApplication.QueuePlayerLoopUpdate();
                    responseJson = "{\"status\":\"refresh_queued\"}";
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
