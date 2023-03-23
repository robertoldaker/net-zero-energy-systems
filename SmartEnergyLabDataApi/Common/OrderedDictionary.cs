namespace SmartEnergyLabDataApi.Common
{
    public class OrderedDictionary<K,T> where K : notnull where T: notnull {
        private Dictionary<K,T> _dict;
        private List<T> _list;
        private Dictionary<T,int> _indexDict;

        public OrderedDictionary() {
            _dict = new Dictionary<K, T>();
            _list = new List<T>();
            _indexDict = new Dictionary<T, int>();
        }

        public int Count {
            get {
                return _list.Count;
            }
        }

        public T Item(K key) {
            return _dict[key];
        }

        public T Item(int index) {
            // Since this re-creates the functionality of a VBA collection ensure its a 1 based index
            return _list[index-1];
        }

        public int Index(T value) {
            return _indexDict[value];
        }

        public List<T> Items {
            get {
                return _list;
            }
        }

        public void Add(K key, T obj) {
            _dict.Add(key,obj);
            _list.Add(obj);
            _indexDict.Add(obj, _list.Count);
        }

        public bool ContainsKey(K key) {
            return _dict.ContainsKey(key);
        }

        public void Remove(K key) {
            var val = _dict[key];
            _list.Remove(val);
            _dict.Remove(key);
            _indexDict.Remove(val);
        }

    }
}