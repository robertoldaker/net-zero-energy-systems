namespace SmartEnergyLabDataApi.BoundCalc
{
    public abstract class ObjectWrapper<T> where T: class {

        public ObjectWrapper(T obj, int index) {
            Obj = obj;
            Index = index;
        }
        public T Obj {get; private set;}
        public int Index {get; private set;}
    }

}