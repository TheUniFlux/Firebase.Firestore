/*
Copyright (c) 2023 Xavier Arpa López Thomas Peter ('Kingdox')

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
using System;
using System.Threading;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Extensions;
using Firebase.Firestore;
using Kingdox.UniFlux;
namespace Kingdox.UniFlux.Firebase.Firestore
{
    public static class Key
    {
        public const string K = "Firebase.Firestore.";
        public const string Initialize = K + nameof(Initialize);
        public const string Set = K + nameof(Set);
        public const string Get = K + nameof(Get);
        public const string GetAll = K + nameof(GetAll);
        public const string GetId = K + nameof(GetId);
        public const string Subscribe = K + nameof(Subscribe);
    }
    public sealed partial class FirestoreFlux : MonoFlux
    {
        private FirebaseFirestore db = default;
        private Dictionary<string, (ListenerRegistration listener, Action<DocumentSnapshot> caller)> dic_listener = new Dictionary<string, (ListenerRegistration, Action<DocumentSnapshot>)>();

        private void OnDestroy() 
        {
            foreach (var item_listener in dic_listener) item_listener.Value.listener.Stop();
        }

        [Flux(Key.Initialize)] private void Initialize() => db = FirebaseFirestore.DefaultInstance;

        [Flux(Key.GetId)] private string GetId(string path) => this.db.Collection(path).Document().Id;

        [Flux(Key.Get)] private async Task<object> Get(string path)
        {
            try
            {
                return (await db.Document(path).GetSnapshotAsync()).ToDictionary();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MMA.Firebase.Firestore]: Error GET: '{path}' => {ex}");
                return null;
            }
        }
        [Flux(Key.Get)] private async Task<DocumentSnapshot> GetSnapshot(string path)
        {
            try
            {
                return (await db.Document(path).GetSnapshotAsync());
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MMA.Firebase.Firestore]: Error GET SNAPSHOT: '{path}' => {ex}");
                return null;
            }
        }

        [Flux(Key.Get)] private async Task<object> Get((string path, Type typeValue) arg)
        {
            // bug report: https://forum.unity.com/threads/no-ahead-of-time-aot-code-was-generated.1070144/
            // ~`This will be corrected in 2020.3.7.`~
            // https://unity.com/releases/editor/archive
            try
            {
                var _type = arg.typeValue;
                var snapshot  = await db.Document(arg.path).GetSnapshotAsync();
                var method =  snapshot.GetType().GetMethod(nameof(DocumentSnapshot.ConvertTo));
                method = method.MakeGenericMethod(_type); // Convert ~generic method to generic
                var val = method.Invoke(snapshot, new object[]{ServerTimestampBehavior.None});
                //ANDROID BUG => for which no ahead of time (AOT) code was generated.
                return val;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("~`This will be corrected in 2020.3.7.`~");
                Debug.LogWarning($"[MMA.Firebase.Firestore]: Error GET: '{arg.path}' {arg.typeValue}' => {ex}");
                return null;
            }
        }

        [Flux(Key.Get)] private async Task<object> Get((string path, object defaultValue) data) => (await Get(data.path)) ?? data.defaultValue;
        [Flux(Key.GetAll)] private async Task<List<(string id, Dictionary<string, object> document)>> GetAll(string path) => (await this.db.Collection(path).GetSnapshotAsync()).ToList().ConvertAll(_item => (_item.Id, _item.ToDictionary()));
        [Flux(Key.Set)] private async Task Set((string path, IDictionary<string, object> value) data) => await db.Document(data.path).SetAsync(data.value);
        [Flux(Key.Set)] private async Task Set((string path, object value) data) => await db.Document(data.path).SetAsync(data.value);
        

        [Flux(Key.Subscribe)] private void Subscribe((bool condition, string path, Action<DocumentSnapshot> callback) data)
        {
            if (data.condition)
            {
                //Si NO existe se añade
                if (!dic_listener.ContainsKey(data.path))
                {
                    dic_listener.Add(data.path, (
                        db.Document(data.path).Listen(__OnListenerResponse),
                        default
                    ));
                }
                var _item = dic_listener[data.path];
                _item.caller += data.callback;
                dic_listener[data.path] = _item;
            }
            else if (dic_listener.ContainsKey(data.path))
            {
                var _item = dic_listener[data.path];
                _item.caller -= data.callback;
                dic_listener[data.path] = _item;

                //Si no hay ninguno solicitando lo elimina del diccionario
                if (dic_listener[data.path].caller == null)
                {
                    dic_listener.Remove(data.path);
                }
            }
        }

        private void __OnListenerResponse(DocumentSnapshot snapshot)
        {
            if(dic_listener.ContainsKey(snapshot.Reference.Path))
            {
                dic_listener[snapshot.Reference.Path].caller?.Invoke(snapshot);
            }
            else
            {
                Debug.LogWarning($"Listening '{snapshot.Reference.Path}' but does not exist on Dictionary");
                StartCoroutine(__Request_Response(snapshot));
            }
        }

        private IEnumerator __Request_Response(DocumentSnapshot snapshot)
        {
            Debug.Log($"Snapshot Wait '{snapshot.Reference.Path}' ");
            yield return new WaitUntil(()=>dic_listener.ContainsKey(snapshot.Reference.Path));
            Debug.Log($"Snapshot Invoke '{snapshot.Reference.Path}' ");
            dic_listener[snapshot.Reference.Path].caller?.Invoke(snapshot);
        }
        
        // private async Task Set((string path, ITuple value) data)
        // {
        //     Dictionary<string, object> dic_value = new Dictionary<string, object>();
        //     for (int i = 0; i < data.value.Length; i++) dic_value.Add(i.ToString(), data.value[i]);
        //     await Set((data.path, dic_value));
        // }



        //public ListenerRegistration Sync(Action<T> callback, string _id = default)
        //{
        //    string _idToUse = _id ?? id ?? default; // si no encuentra ningún ID entonces toca añadir
        //    return DocRef(_idToUse).Listen(snap => {
        //        if (snap.Exists)
        //        {
        //            T data = snap.ConvertTo<T>();
        //            data.id = _idToUse;
        //            callback?.Invoke(data);
        //        }
        //    });
        //}


        //protected static ListenerRegistration SyncCollection(string colRef, Action<T[]> callback)
        //{
        //    return db.Collection(PrefixDevelop + colRef).Listen(snap => {
        //        T[] data = _ConvertCollection(snap);
        //        callback.Invoke(data);
        //    });
        //}

    }
}

/* de BD
 * 
 * 
 * /// <summary>
    /// Sync the information
    /// </summary>
    public ListenerRegistration Sync(Action<T> callback, string _id = default)
    {
        string _idToUse = _id ?? id ?? default; // si no encuentra ningún ID entonces toca añadir
        return DocRef(_idToUse).Listen(snap =>{
            if (snap.Exists)
            {
                T data = snap.ConvertTo<T>();
                data.id = _idToUse;
                callback?.Invoke(data);
            }
        });
    }

    /// <summary>
    /// Returns an array of Docs
    /// </summary>
    /// <param name="callBack"></param>
    public void GetAll(Action<T[]> callBack)
    {
        ColRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            Assert.IsNull(task.Exception);
            T[] data = _ConvertCollection(task.Result);
            callBack.Invoke(data);
        });
    }



    /// <summary>
    /// Apply the transformation of the collection
    /// </summary>
    private static T[] _ConvertCollection(QuerySnapshot snap)
    {
        DocumentSnapshot[] Ddata = snap.ToArray();
        T[] data = new T[Ddata.Length];
        for (int i = 0; i < data.Length; i++){
            data[i] = Ddata[i].ConvertTo<T>();
            data[i].id = Ddata[i].Id;
        }
        return data;
    }


 */


//private static T[] _ConvertCollection(QuerySnapshot snap)
//{
//    DocumentSnapshot[] Ddata = snap.ToArray();
//    T[] data = new T[Ddata.Length];
//    for (int i = 0; i < data.Length; i++)
//    {
//        data[i] = Ddata[i].ConvertTo<T>();
//        data[i].id = Ddata[i].Id;
//    }
//    return data;
//}
