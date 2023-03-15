using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

#nullable enable

namespace PortalJsonClient
{
    public interface IWebPortalJsonLoader<T> where T : PortalJsonLike
    {
        public IEnumerator FetchWebPortalJsonString(
            string url,
            WebPortalJsonLoader<T>.RequestHandler handler
        );
    }

    public class WebPortalJsonLoader<T> : IWebPortalJsonLoader<T> where T : PortalJsonLike
    {
        public IEnumerator FetchWebPortalJsonString(
            string url,
            RequestHandler handler
        )
        {
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("Accept", "application/json");
                yield return request.SendWebRequest();

                switch (request.result)
                {
                    case UnityWebRequest.Result.ConnectionError:
                        Debug.Log(request.error);
                        handler.OnFailure(request.downloadHandler.text);
                        break;
                    case UnityWebRequest.Result.DataProcessingError:
                        Debug.Log(request.error);
                        handler.OnFailure(request.downloadHandler.text);
                        break;
                    case UnityWebRequest.Result.ProtocolError:
                        Debug.Log(request.error);
                        handler.OnFailure(request.downloadHandler.text);
                        break;
                    case UnityWebRequest.Result.Success:
                        var data = handler.preprocessSuccessData(request.downloadHandler.text);
                        handler.OnSuccess(data);
                        break;
                }
            }
        }

        public class RequestHandler
        {
            private Action<T> success;
            private Action<string> failure;

            public RequestHandler(Action<T> onSuccess, Action<string> onFailure, PortalJsonClient<T>.ParsePortalJson preprocessor)
            {
                success = onSuccess;
                failure = onFailure;
                preprocessSuccessData = preprocessor;
            }

            public PortalJsonClient<T>.ParsePortalJson preprocessSuccessData
            {
                get;
                private set;
            }

            public void OnSuccess(T portalJson)
            {
                success(portalJson);
            }

            public void OnFailure(string error)
            {
                failure(error);
            }
        }

    }
}

public class PortalJsonClient<T> where T : PortalJsonLike
{
    public delegate T ParsePortalJson(string portalJsonString);

    protected ParsePortalJson parseFn;
    protected PortalJsonClient.IWebPortalJsonLoader<T> webLoader;

    public PortalJsonClient(
        ParsePortalJson parsePortalJson,
        PortalJsonClient.IWebPortalJsonLoader<T>? webPortalJsonLoader = null
    )
    {
        parseFn = parsePortalJson;
        webLoader = (
            webPortalJsonLoader ??
            new PortalJsonClient.WebPortalJsonLoader<T>()
        );
    }

    protected string _GetRemoteUrl(string url)
    {
        return url;
    }

    protected PortalJsonClient.WebPortalJsonLoader<T>.RequestHandler
    webRequestHandlerFactory(Action<T> callback, Action<string>? errorHandler)
    {
        return new PortalJsonClient.WebPortalJsonLoader<T>.RequestHandler(
            callback,
            (
                errorHandler ??
                ((string _message) => { /* not implemented, no-op */ })
            ),
            parseFn
        );
    }

    public IEnumerator RetrivePortalJson(string url, Action<T> callback)
    {
        var handler = webRequestHandlerFactory(callback, null);
        return RetrivePortalJson(url, handler);
    }

    public IEnumerator RetrivePortalJson(
        string url,
        PortalJsonClient.WebPortalJsonLoader<T>.RequestHandler handler
    )
    {
        string remoteUrl = _GetRemoteUrl(url);
        return webLoader.FetchWebPortalJsonString(remoteUrl, handler);
    }

}
