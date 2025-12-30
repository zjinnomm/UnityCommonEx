namespace UnityCommonEx
{
    public interface ITickable
    {

        void Tick(float delta);

        bool IsTicking();

    }
}