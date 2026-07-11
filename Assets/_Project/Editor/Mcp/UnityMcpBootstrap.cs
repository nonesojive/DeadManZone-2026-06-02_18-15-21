using System;
using System.Threading.Tasks;
using MCPForUnity.Editor.Services;
using MCPForUnity.Editor.Services.Transport;
using UnityEditor;
using UnityEngine;

namespace DeadManZone.Editor.Mcp
{
    /// <summary>
    /// Ensures Coplay Unity MCP HTTP bridge is running after editor load.
    /// Branch switches that drop the package used to leave Cursor/Claude pointing at a dead :8080.
    /// </summary>
    [InitializeOnLoad]
    internal static class UnityMcpBootstrap
    {
        private const string SessionKey = "DeadManZone.UnityMcpBootstrap.Done";
        private const string AutoStartPref = "MCPForUnity.AutoStartOnLoad";

        static UnityMcpBootstrap()
        {
            EditorApplication.delayCall += () => _ = EnsureRunningAsync(force: false);
        }

        [MenuItem("DeadManZone/Start Unity MCP Bridge", priority = 1000)]
        public static void ForceStartFromMenu()
        {
            SessionState.SetBool(SessionKey, false);
            _ = EnsureRunningAsync(force: true);
        }

        private static async Task EnsureRunningAsync(bool force)
        {
            if (!force && SessionState.GetBool(SessionKey, false))
                return;

            try
            {
                EditorPrefs.SetBool(AutoStartPref, true);
                EditorConfigurationCache.Instance.SetUseHttpTransport(true);

                if (MCPServiceLocator.TransportManager.IsRunning(TransportMode.Http))
                {
                    SessionState.SetBool(SessionKey, true);
                    Debug.Log("[DeadManZone] Unity MCP already connected.");
                    return;
                }

                if (!MCPServiceLocator.Server.IsLocalHttpServerReachable())
                {
                    if (!MCPServiceLocator.Server.StartLocalHttpServer(quiet: true))
                    {
                        Debug.LogWarning("[DeadManZone] Failed to start Unity MCP HTTP server on :8080.");
                        return;
                    }
                }

                for (int i = 0; i < 40; i++)
                {
                    if (MCPServiceLocator.Server.IsLocalHttpServerReachable())
                        break;
                    await Task.Delay(250);
                }

                if (!await MCPServiceLocator.Bridge.StartAsync())
                {
                    Debug.LogWarning("[DeadManZone] Unity MCP HTTP server is up but the editor bridge failed to connect.");
                    return;
                }

                SessionState.SetBool(SessionKey, true);
                Debug.Log("[DeadManZone] Unity MCP connected (http://127.0.0.1:8080/mcp).");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DeadManZone] Unity MCP bootstrap skipped: {ex.Message}");
            }
        }
    }
}
