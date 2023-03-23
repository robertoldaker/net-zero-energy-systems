using System;
using System.Collections.Generic;

namespace HaloSoft.DataAccess
{
    public class ObjectCache<T> where T : class, new()
    {
        private Dictionary<string, T> _objectDict;
        private DataAccessBase _da;
        private Action<T,string> _setKeyFunc;
        public ObjectCache(DataAccessBase da, IList<T> initialObjects, Func<T,string> getKeyFunc, Action<T,string> setKeyFunc=null) {
            //
            _da = da;
            _setKeyFunc = setKeyFunc;
            // Setup initial dictionary
            _objectDict = new Dictionary<string,T>();
            foreach( var obj in initialObjects) {
                var key = getKeyFunc.Invoke(obj);
                if ( !_objectDict.ContainsKey(key) ) {
                    _objectDict.Add(key,obj);
                }
            }
        }
        public T GetOrCreate(string key, out bool created) {
            // If its in the dictionary then return it
            if ( _objectDict.ContainsKey(key)) {
                created = false;
                return _objectDict[key];
            } else {
                created = true;
                // Other create a new instance set the key and add it NHibernate and the dictionary
                var obj = new T();
                if ( _setKeyFunc!=null ) {
                    _setKeyFunc.Invoke(obj,key);
                }
                _da.Session.Save(obj);
                _objectDict.Add(key, obj);
                return obj;
            }
        }
        public bool TryGetValue(string key, out T value) {
            return _objectDict.TryGetValue(key, out value);
        }

    }
}