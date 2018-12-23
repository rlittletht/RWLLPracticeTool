
namespace Rwp
{
    public class RSRBase
    {
        public bool Result { get; set; }
        public string Reason { get; set; }
        public bool Succeeded => Result;
    }

    public class RSR : RSRBase
    {
    }

    public class TRSR<T> : RSRBase
    {
        public T TheValue { get; set; }
    }
}