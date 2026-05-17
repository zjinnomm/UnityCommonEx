namespace UnityCommonEx
{
    public enum TickType
    {
        Game,
        UI,
    }

    public interface ITickable
    {

        void Tick(float delta);

        bool IsTicking();

        TickType GetTickType();

    }
}
