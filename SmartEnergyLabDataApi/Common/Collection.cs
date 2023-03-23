namespace SmartEnergyLabDataApi.Common
{
    public class Collection<T> {
        private Dictionary<string,T> _dict;
        private List<T> _list;

        public Collection() {
            _dict = new Dictionary<string, T>();
            _list = new List<T>();
        }

        public int Count {
            get {
                return _list.Count;
            }
        }

        public T Item(string key) {
            return _dict[key];
        }

        public T Item(int index) {
            // Since this re-creates the functionality of a VBA collection ensure its a 1 based index
            return _list[index-1];
        }

        public List<T> Items {
            get {
                return _list;
            }
        }

        public void Add(T obj, string key) {
            _dict.Add(key,obj);
            _list.Add(obj);
        }

        public bool ContainsKey(string key) {
            return _dict.ContainsKey(key);
        }

        public void Remove(string key) {
            var val = _dict[key];
            _list.Remove(val);
            _dict.Remove(key);
        }

    }
}