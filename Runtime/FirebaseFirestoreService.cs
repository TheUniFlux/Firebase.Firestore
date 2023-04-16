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
using System.Threading.Tasks;
using System.Collections.Generic;
using Firebase.Firestore;
namespace Kingdox.UniFlux.Firebase.Firestore
{
    public static partial class FirebaseFirestoreService // Data
    {
        private static partial class Data
        {

        }
    }
    public static partial class FirebaseFirestoreService // Key
    {
        public static partial class Key
        {
            private const string _FirebaseFirestoreService =  nameof(FirebaseFirestoreService) + ".";
            public const string Initialize = _FirebaseFirestoreService + nameof(Initialize);
            public const string Set = _FirebaseFirestoreService + nameof(Set);
            public const string Get = _FirebaseFirestoreService + nameof(Get);
            public const string GetAll = _FirebaseFirestoreService + nameof(GetAll);
            public const string GetId = _FirebaseFirestoreService + nameof(GetId);
            public const string Subscribe = _FirebaseFirestoreService + nameof(Subscribe);
        }
    }
    public static partial class FirebaseFirestoreService // Methods
    {
        public static void Initialize() => Key.Initialize.Dispatch();
        public static Task Set(in (string path, object value) data) => Key.Set.Task(data);
        public static Task Set(in (string path, IDictionary<string, object> value) data) => Key.Set.Task(data);
        public static Task<object> Get(in string path) => Key.Get.Task<string, object>(path);
        public static Task<object> Get(in (string path, object defaultValue) data) => Key.Get.Task<(string path, object defaultValue), object>(data);
        public static Task<object> Get(in (string path, Type typeValue) data) => Key.Get.Task<(string path, Type typeValue), object>(data);
        public static Task<DocumentSnapshot> GetSnapshot(in string path) => Key.Get.Task<string, DocumentSnapshot>(path);
        public static Task<List<(string id, Dictionary<string, object> document)>> GetAll(in string path) => Key.GetAll.Task<string, List<(string id, Dictionary<string, object> document)>>(path);
        public static string GetId(string pathCollection) => Key.GetId.Dispatch<string, string>(pathCollection);
        public static void Subscribe((bool condition, string path, Action<DocumentSnapshot> callback) data) => Key.Subscribe.Dispatch<(bool condition, string path, Action<DocumentSnapshot> callback)>(data);
    }
}