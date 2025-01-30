namespace SmartEnergyLabDataApi.BoundCalc
{
    public abstract class DataStore<T>
    {
        protected Dictionary<string,T> _dict;
        protected Dictionary<string,int> _dictIndex;
        protected IList<T> _list;
        private int _idx;

        protected DataStore() {
            _dict = new Dictionary<string, T>();
            _list = new List<T>();
            _dictIndex = new Dictionary<string, int>();
            _idx = 0;
        }

        protected void add(string key, T obj) {
                _list.Add(obj);
                _dict.Add(key, obj);
                _dictIndex.Add(key, _idx++);
        }

        public T get(string key) {
            return _dict[key];            
        }

        // Note gets using 1 based index
        public T get(int index) {
            return _list[index-1];
        }

        public int getIndex(string key) {
            return _dictIndex[key];
        }

        public int Count {
            get 
            {
                return _list.Count;
            }
        }

        public IList<T> Objs {
            get {
                return _list;
            }
        }

    }
}