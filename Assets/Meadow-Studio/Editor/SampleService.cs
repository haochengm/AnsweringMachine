using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Meadow.Studio
{
    public class SampleService
    {
        public async Task<HTTPSResponse> GetAllSamplesMetadata()
        {
            var request = new UnityWebRequest("https://europe-west1-xref-client.cloudfunctions.net/plugin-getSamples", "GET");
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            await request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.Log(request.error);
                return new HTTPSResponse { success = false, message = request.error };
            }
            else
            {
                string jsonText = request.downloadHandler.text;
                JObject parsedResponse = new JObject(JObject.Parse(jsonText).Properties());
                return new HTTPSResponse { success = true, data = parsedResponse };
            }
        }

        public void DownloadSampleUnityPackage(string id, string packageKey, string name)
        {
            var json = JsonUtility.ToJson(new RequestData { id = id, type = "experience", packageKey = packageKey });
            var request = new UnityWebRequest("https://europe-west1-xref-client.cloudfunctions.net/exp-downloadPackage", "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            EditorUtility.DisplayProgressBar("Downloading Sample", "Please wait...", 0.5f);

            request.SendWebRequest().completed += _ =>
            {
                EditorUtility.ClearProgressBar();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("Download failed: " + request.error);
                }
                else
                {
                    name = name.Replace(":", "").Replace(" ", "-").ToLower();
                    //path to assets folder
                    var path = EditorUtility.SaveFilePanel("Save Meadow Sample", Application.dataPath, name + ".unitypackage", "unitypackage");
                    if (!string.IsNullOrEmpty(path))
                    {
                        File.WriteAllBytes(path, request.downloadHandler.data);
                        // Debug.Log("Package downloaded and saved to: " + path);
                    }
                }
            };
        }

        [System.Serializable]
        private class RequestData
        {
            public string id;
            public string packageKey;
            public string type;
        }
    }
}