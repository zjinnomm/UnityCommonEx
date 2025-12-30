using UnityEngine;

namespace UnityCommonEx
{

    public abstract class MainGameController<T> : SingletonController<T> where T : MainGameController<T>
    {

        public Transform NodeRoot;

        [Header("Data")]
        public DataLoadMethodType EditorLoadMethod = DataLoadMethodType.ScatteredFile;
        public DataLoadMethodType RuntimeLoadMethod = DataLoadMethodType.PackedResourceFile;
        public string ScatteredDataLoadConfigPath;
        public string PackedDataPath;

        protected override bool IsPersistent => true;

        protected override void OnInit()
        {
            base.OnInit();
            LogUtil.Init();

            DataLoadMethodType loadMethod;
#if UNITY_EDITOR
            loadMethod = EditorLoadMethod;
#else
            loadMethod = RuntimeLoadMethod;
#endif
            if (loadMethod == DataLoadMethodType.ScatteredFile)
            {
                DataManager.LoadScattered(ScatteredDataLoadConfigPath);
            }
            else if (loadMethod == DataLoadMethodType.PackedResourceFile)
            {
                DataManager.LoadPacked(PackedDataPath);
            }

            DontDestroyOnLoad(this);

        }

        void Update()
        {
            InteractionModel.Update();
            float delta = Time.deltaTime;
            TickingManager.Tick(delta);
            TimerManager.Tick(delta);
            OnUpdate(delta);
        }

        virtual protected void OnUpdate(float delta) { }

        protected override void OnRelease()
        {
            LogUtil.Release();
            base.OnRelease();
        }

    }

}