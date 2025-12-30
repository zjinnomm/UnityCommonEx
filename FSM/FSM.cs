namespace UnityCommonEx
{

    public abstract class FSM<M> where M: FSM<M>
    {

        public static M Instance { get; private set; }

        private FSMState<M> state;
        private int nextStateType;
        private Properties nextProperties;

        public FSMState<M> State => state;

        public FSM(int initStateType)
        {
            Instance = (M)this;
            ChangeState(initStateType);
        }

        public virtual void Update(float delta)
        {
            var stateType = state.OnUpdate(delta, out var properties);
            if (nextStateType != state.Type)
            {
                ChangeState(nextStateType, nextProperties);
            }
            else if (stateType != state.Type)
            {
                nextStateType = stateType;
                nextProperties = properties;
                ChangeState(nextStateType, nextProperties);
            }
        }

        public virtual void Destroy()
        {
            if (state != null)
            {
                state.OnLeave();
                OnStateChanged(state, null);
            }
            Instance = null;
        }

        public void InputEvent(int eventType, Properties properties = null)
        {
            var stateType = state.OnEvent(eventType, properties, out var outProperties);
            if (!stateType.Equals(state.Type))
            {
                nextStateType = stateType;
                nextProperties = outProperties;
            }
        }

        protected virtual void ChangeState(int stateType, Properties properties = null)
        {
            FSMState<M> oldState = state;
            FSMState<M> nextState = GetStateByType(stateType);
            if (nextState == null)
            {
                LogUtil.Error("{0} has not registered a state of type {1}.", GetType(), stateType);
                return;
            }
            if (oldState != null)
            {
                oldState.OnLeave();
            }
            state = nextState;
            nextStateType = state.Type;
            nextProperties = null;
            state.OnEnter(properties);
            OnStateChanged(oldState, nextState);
        }

        protected virtual void OnStateChanged(FSMState<M> originalState, FSMState<M> newState) { }

        protected abstract FSMState<M> GetStateByType(int stateType);

    }

}