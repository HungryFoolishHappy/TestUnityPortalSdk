using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System.Threading.Tasks;
#if UNITY_EDITOR
using UnityEditor;
#endif


public class PortalMaterial : Material
{
    public PortalMaterial(Shader shader) : base(shader)
    { }
}

class GltfFileUtil
{
    const string MimeTypeGltfBinary = "model/gltf-binary";
    const string MimeTypeGltf = "model/gltf+json";

    static public bool IsBinary(UnityWebRequest file)
    {
        if (file.result != UnityWebRequest.Result.Success)
        {
            string contentType = file.GetResponseHeader("Content-Type");
            if (contentType == MimeTypeGltfBinary)
                return true;
            if (contentType == MimeTypeGltf)
                return false;
        }
        return false;
    }
}

class PortalLoader
{
    protected static PortalJsonClient<PortalJson> jsonloader = (
        new PortalJsonClient<PortalJson>(PortalJson.CreateFromJSON)
    );

    private string url { get; }
    private Portal portal { get; }
    private GameObject gameObject { get; }
    private Settings portalSettings { get; }

    public PortalMaterial NewlyCreatedMaterial
    {
        get;
        protected set;
    }

    public PortalLoader(string url, Portal portal, Settings portalSettings)
    {
        this.url = url;
        this.portal = portal;
        this.gameObject = portal.gameObject;
        this.portalSettings = portalSettings;
    }

    private IEnumerator LoadJsonTo(Action<PortalJson> setJson)
    {
        yield return jsonloader.RetrivePortalJson(url, setJson);
    }

    private IEnumerator LoadTextureTo(Action<Texture> setTexture, PortalJson json)
    {
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(
            json.Presentation.PanoramaUrl
        );
        yield return www.SendWebRequest();

        Texture myTexture = null;
        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(www.error);
        }
        else
        {
            myTexture = ((DownloadHandlerTexture)www.downloadHandler).texture;
        }

        setTexture(myTexture);
    }

    private IEnumerator LoadModelTo(Action<string> setModel, PortalJson json)
    {
        string url = json.Presentation.ModelUrl;
        UnityWebRequest www = UnityWebRequest.Get(url);
        string filePrefix = Path.Combine(Application.temporaryCachePath, portalSettings.PortalAssetFolder);
        string tmpFilePath = Path.Combine(filePrefix, Path.GetFileName(url));
        Directory.CreateDirectory(filePrefix);
        www.downloadHandler = new DownloadHandlerFile(tmpFilePath);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(www.error);
        }
        else
        {
            string fileSuffix = "";
            if (!(tmpFilePath.EndsWith(".glb") || tmpFilePath.EndsWith(".gltf")))
            {
                fileSuffix = GltfFileUtil.IsBinary(www) ? ".glb" : ".gltf";
            }
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
            {
                Directory.CreateDirectory(Path.Combine(Application.dataPath, "Portal Assets"));
                var uniqueFileName = AssetDatabase.GenerateUniqueAssetPath($"Assets/Portal Assets/portal model{fileSuffix}");
                Debug.Log("gee");
                Debug.Log($"uniqueFileName :: {fileSuffix} :: {uniqueFileName} :: {tmpFilePath}");
                File.Move(tmpFilePath, uniqueFileName);
                AssetDatabase.Refresh();
                setModel(uniqueFileName);
            }
            else
            {
                var filePath = string.Concat(tmpFilePath, fileSuffix);
                File.Move(tmpFilePath, filePath);
                setModel(filePath);
            }
#else
            var filePath = File.Move(tmpFilePath, string.Concat(tmpFilePath, fileSuffix));
            setModel(filePath);
#endif
        }
    }

    private void ApplyTexture(Texture texture)
    {
        var renderer = gameObject.GetComponent<Renderer>();
        renderer.enabled = true;

        PortalMaterial material = new PortalMaterial(renderer.sharedMaterial.shader);
        material.CopyPropertiesFromMaterial(renderer.sharedMaterial);

        this.NewlyCreatedMaterial = material;
        material.SetTexture("_MainTex", texture);
        renderer.sharedMaterial = material;
        gameObject.GetComponent<MeshFilter>().sharedMesh = portalSettings.DefaultColliderMesh;
    }

    private void EmableCollider()
    {
        gameObject.GetComponent<MeshCollider>().enabled = true;
    }

    protected void ApplyEventHandlers(PortalJson json)
    {
        // portal.targetUrl = json.Destination.Url;
        // portal.SetTargetUrl(json.Destination.Url);
        portal.AddCollideHandlers(new PortalOnCollideHandler(json.Destination.Url));
    }

    private IEnumerator _RunTimeLoadModel(string gltfPath)
    {
        var gltf = new GLTFast.GltfImport();

        var settings = new GLTFast.ImportSettings {
            GenerateMipMaps = true,
            AnisotropicFilterLevel = 3,
            NodeNameMethod = GLTFast.NameImportMethod.OriginalUnique
        };

        var loadTask = Task.Run(async () => {
            return await gltf.Load(string.Concat("file://", gltfPath), settings);
        });
        yield return new WaitUntil(() => loadTask.IsCompleted);
        bool loadGltfSuccessfully = loadTask.Result;

        if (loadGltfSuccessfully) {
            var t = Task.Run(async () => {
                await gltf.InstantiateMainSceneAsync(gameObject.transform);
            });
            yield return new WaitUntil(() => t.IsCompleted);
        }
        else {
            Debug.LogError("Loading glTF failed!");
        }
    }

    protected IEnumerator ApplyModel(string gltfPath, Action<GameObject> setObject)
    {
#if UNITY_EDITOR
        if (!EditorApplication.isPlaying)
        {
            Debug.Log($"ApplyModel: {gltfPath}");
            UnityEngine.Object prefab = AssetDatabase.LoadAssetAtPath(
                gltfPath,
                typeof(GameObject)
            );
            var newObject = (GameObject)UnityEngine.GameObject.Instantiate(prefab, gameObject.transform);
            setObject(newObject);
            Debug.Log($"should work");
        }
        else
        {
            yield return _RunTimeLoadModel(gltfPath);
        }
#else
        yield return _RunTimeLoadModel(gltfPath);
#endif
    }

    protected IEnumerator ApplyAnimation(GameObject model)
    {
        // TODO: figure out how to load/play the animations
        // model.GetComponent<Animator>().runtimeAnimatorController
        yield return null;
    }

    protected bool Is3dModel(PortalJson json)
    {
        return !String.IsNullOrEmpty(json.Presentation?.ModelUrl);
    }

    protected bool IsPanorama(PortalJson json)
    {
        return !String.IsNullOrEmpty(json.Presentation?.PanoramaUrl);
    }

    public IEnumerator Load()
    {
#if UNITY_EDITOR
        Debug.Log("Loading Portal data");
#endif

        var json = new Data<PortalJson>();
        yield return LoadJsonTo(json.Store);

        if (Is3dModel(json))
        {
            var gltfPath = new Data<string>();
            yield return LoadModelTo(gltfPath.Store, json);

            if (String.IsNullOrEmpty(gltfPath)) yield break;

            var newObject = new Data<GameObject>();
            yield return ApplyModel(gltfPath, newObject.Store);
            yield return ApplyAnimation(newObject);
        }
        else if (IsPanorama(json))
        {
            var texture = new Data<Texture>();
            yield return LoadTextureTo(texture.Store, json);

            if (texture.IsNull()) yield break;

            ApplyTexture(texture);
            EmableCollider();
        }

        ApplyEventHandlers(json);
    }

    public static void ResetData(Portal portal)
    {
        GameObject gameObject = portal.gameObject;
        portal.ClearCollideHandlers();
        foreach (Transform child in gameObject.transform) {
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
            {
                GameObject.DestroyImmediate(child.gameObject);
            }
            else
            {
                GameObject.Destroy(child.gameObject);
            }
#else
            GameObject.Destroy(child.gameObject);
#endif
        }

        gameObject.GetComponent<Renderer>().enabled = false;
        gameObject.GetComponent<MeshFilter>().sharedMesh = null;
        gameObject.GetComponent<MeshCollider>().enabled = false;
    }

    class Data<T>
    {
        private T data;
        public void Store(T value)
        {
            data = value;
        }
        public static implicit operator T(Data<T> instance)
        {
            return instance.data;
        }
        public bool IsNull()
        {
            return data == null;
        }
    }
}


public class GLTFDynamicTrigger : MonoBehaviour
{
    public Portal portal;

    // public GLTFDynamicTrigger(Portal portal)
    // {
    //     this.portal = portal;
    // }

    public void OnTriggerEnter(Collider other)
    {
        portal.OnTriggerEnter(other);
    }
}

public class PortalEventHandler
{
}

[System.Serializable]
public class PortalOnCollideHandler : PortalEventHandler
{
    [SerializeField]
    public string targetUrl;

    public PortalOnCollideHandler(string targetUrl)
    {
        this.targetUrl = targetUrl;
    }

    public void Call(Collider other)
    {
        Debug.Log($"[MHTTP Portal] Teleporting to: {targetUrl}");
        Application.OpenURL(targetUrl);
    }
}
