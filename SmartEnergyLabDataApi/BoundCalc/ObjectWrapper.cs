namespace SmartEnergyLabDataApi.BoundCalc
{
    public abstract class ObjectWrapper<T> where T: class {

        public ObjectWrapper(T obj) {
            Obj = obj;
        }
        public T Obj {get; private set;}
    }

}