using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
#if UNITY_EDITOR
using UnityEditor;
#endif

#if UNITY_EDITOR
[ExecuteInEditMode]
#endif
public class Portal : MonoBehaviour
{

#if UNITY_EDITOR
    private bool _editorPropertyChanged = false;
    [SerializeField] [HideInInspector]
    protected string _currentUrl;
#endif

    [SerializeField] [HideInInspector]
    private bool _assetLoaded = false;

    protected PortalMaterial _materialCreated;

    public string Url;

    [SerializeField]
    public Settings settings;

    [SerializeField] [HideInInspector]
    private List<PortalOnCollideHandler> onCollideHandlers = new List<PortalOnCollideHandler>();

    protected PortalJson portalJsonData = null;

    void Start()
    {
        Debug.Log("Start");
#if ! UNITY_EDITOR
        if (_assetLoaded) return;
        if (!string.IsNullOrEmpty(Url)) LoadPortal();
#else
        var assign_to_prevent_unused_property_warning = _assetLoaded;
#endif
    }

    void Update()
    {
#if UNITY_EDITOR
        if (_editorPropertyChanged)
        {
            _editorPropertyChanged = false;
            if (_currentUrl == Url)
            {
                /* url not updated => no action required => no-op */
            }
            else
            {
                _currentUrl = Url;
                LoadPortal();
            }
        }
#endif
    }

    public void OnTriggerEnter(Collider other)
    {
        Debug.Log("OnTriggerEnter");
        if (settings.PlayerTags.FirstOrDefault(other.gameObject.CompareTag) != null)
        {
            foreach(var handler in onCollideHandlers)
            {
                Debug.Log(handler);
                handler.Call(other);
            }
        }
    }
    public void AddCollideHandlers(PortalOnCollideHandler handler)
    {
        onCollideHandlers.Add(handler);
#if UNITY_EDITOR
        if (!EditorApplication.isPlaying) EditorUtility.SetDirty(this);
#endif
    }
    public void ClearCollideHandlers()
    {
        onCollideHandlers = new List<PortalOnCollideHandler>();
#if UNITY_EDITOR
        if (!EditorApplication.isPlaying) EditorUtility.SetDirty(this);
#endif
    }

    protected void LoadPortal()
    {
        StartCoroutine(_LoadPortal());
    }

    protected IEnumerator _LoadPortal()
    {
        PortalLoader.ResetData(this);
        _assetLoaded = false;

#if UNITY_EDITOR
        if (!EditorApplication.isPlaying) EditorUtility.SetDirty(this);
#endif

        if (string.IsNullOrEmpty(Url))
        {
            // data already resetted, no-op
        }
        else
        {
            var matCleaner = MaterialCleanerFactory();
            var loader = new PortalLoader(
                url: Url,
                portal: this,
                portalSettings: settings
            );
            yield return loader.Load();
            _materialCreated = loader.NewlyCreatedMaterial;
            if (_materialCreated != null) matCleaner.CleanOldMaterial();
            _assetLoaded = true;

#if UNITY_EDITOR
            if (!EditorApplication.isPlaying) EditorUtility.SetDirty(this);
#endif
        }
    }

    void OnValidate()
    {
#if UNITY_EDITOR
        _editorPropertyChanged = true;
#endif
    }

    void OnDestroy()
    {
        if (_materialCreated != null) MaterialCleanerFactory().CleanOldMaterial();
    }

    MaterialCleaner MaterialCleanerFactory()
    {
        return new MaterialCleaner {
            material = GetComponentInChildren<Renderer>(true).sharedMaterial,
        };
    }

    class MaterialCleaner
    {
        protected internal Material material
        {
            set;
            protected get;
        }

        public void CleanOldMaterial()
        {
            if (material is PortalMaterial) _Destroy(material);
        }

        private void _Destroy(Object obj)
        {
#if UNITY_EDITOR
            if (EditorApplication.isPlaying)
            {
                UnityEngine.Object.Destroy(obj);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(obj);
            }
#else
            UnityEngine.Object.Destroy(obj);
#endif
        }
    }
}

