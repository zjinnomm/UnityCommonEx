using DataInspector;

namespace UnityCommonEx
{

    public class ExtendedInspector : Inspector
    {

        public ExtendedInspector() : base()
        {
            SetVisualizer(typeof(FloatRange), new FloatRangeVisualizer());
            SetVisualizer(typeof(IntRange), new IntRangeVisualizer());
        }
    }

}