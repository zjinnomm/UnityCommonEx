namespace UnityCommonEx
{

    public abstract class FSMState<M> where M : FSM<M>
    {

        protected M fsm;

        public abstract int Type { get; }

        public FSMState(M fsm)
        {
            this.fsm = fsm;
        }

        public virtual void OnEnter(Properties properties) { }

        public virtual void OnLeave() { }

        public virtual int OnEvent(int eventType, Properties properties, out Properties outProperties)
        {
            outProperties = null;
            return Type;
        }

        public virtual int OnUpdate(float delta, out Properties properties)
        {
            properties = null;
            return Type;
        }

    }

}