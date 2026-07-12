using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace CommerceDemo
{
    /// <summary>
    /// Loads JSON text files from StreamingAssets via UnityWebRequest so the
    /// same code path works in the editor, on desktop, and in WebGL builds
    /// (where StreamingAssets is served over HTTP, not the filesystem).
    /// </summary>
    public static class StreamingAssetsJson
    {
        public static IEnumerator Load(string fileName, Action<string> onLoaded, Action<string> onError = null)
        {
            string path = Path.Combine(Application.streamingAssetsPath, fileName);
            if (!path.Contains("://"))
            {
                path = "file://" + path;
            }

            using (var request = UnityWebRequest.Get(path))
            {
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[StreamingAssetsJson] Failed to load '{fileName}': {request.error}");
                    onError?.Invoke(request.error);
                }
                else
                {
                    onLoaded?.Invoke(request.downloadHandler.text);
                }
            }
        }
    }
}
