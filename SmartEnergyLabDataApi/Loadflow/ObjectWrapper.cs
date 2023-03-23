namespace SmartEnergyLabDataApi.Loadflow
{
    public abstract class ObjectWrapper<T> where T: class {

        public ObjectWrapper(T obj) {
            Obj = obj;
        }
        public T Obj {get; private set;}
    }

}