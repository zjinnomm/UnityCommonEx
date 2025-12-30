namespace UnityCommonEx
{

    public enum VFXRootResizeStrategy : byte
    {
        Default,
        DoNotChange,
    }

    public enum VFXResizeScaleStrategy : byte
    {
        DoNotChange = 0,
        Linear = 1,
        
    }

    public enum VFXResizeDensityStrategy : byte
    {
        DoNotChange = 0,
        Linear = 1,
        Quadratic = 2,
    }

    public enum VFXLingerStrategy : byte
    {
        DoNotChange = 0,
        Stop = 1,
        StopAndHide = 2,
    }

}