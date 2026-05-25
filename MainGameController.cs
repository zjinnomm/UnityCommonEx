using UnityEngine;

namespace UnityCommonEx
{

    public abstract class MainGameController<T> : SingletonController<T> where T : MainGameController<T>
    {
        static float gameDilation = 1f;
        static int gameplayPauseRequestCount = 0;

        public static float GameDilation
        {
            get => gameDilation;
            set => gameDilation = Mathf.Max(0f, value);
        }

        public static float EffectiveGameDilation => gameplayPauseRequestCount > 0 ? 0f : gameDilation;

        public static void PushGameplayPause()
        {
            gameplayPauseRequestCount++;
        }

        public static void PopGameplayPause()
        {
            if (gameplayPauseRequestCount > 0)
            {
                gameplayPauseRequestCount--;
            }
        }

        public Transform NodeRoot;

        [Header("Manager")]
        public VFXManager VFXManager;
        public AudioManager AudioManager;

        [Header("Data")]
        public DataLoadMethodType EditorLoadMethod = DataLoadMethodType.ScatteredFile;
        public DataLoadMethodType RuntimeLoadMethod = DataLoadMethodType.PackedResourceFile;
        public string ScatteredDataLoadConfigPath;
        public string PackedDataPath;

        [Header("Config")]
        public DataLoadMethodType EditorConfigLoadMethod = DataLoadMethodType.ScatteredFile;
        public DataLoadMethodType RuntimeConfigLoadMethod = DataLoadMethodType.PackedResourceFile;
        public string ScatteredConfigPath;
        public string PackedConfigPath;

        protected override bool IsPersistent => true;

        protected override void OnInit()
        {
            base.OnInit();
            LogUtil.Init();
            TickingManager.Init();

            DataLoadMethodType dataLoadMethod;
            DataLoadMethodType configLoadMethod;
#if UNITY_EDITOR
            dataLoadMethod = EditorLoadMethod;
            configLoadMethod = EditorConfigLoadMethod;
#else
            dataLoadMethod = RuntimeLoadMethod;
            configLoadMethod = RuntimeConfigLoadMethod;
#endif

            if (configLoadMethod == DataLoadMethodType.ScatteredFile)
            {
                ConfigParser.ParseAllConfigs(ScatteredConfigPath);
            }
            else if (configLoadMethod == DataLoadMethodType.PackedResourceFile)
            {
                ConfigParser.ParsePackedConfigs(PackedConfigPath);
            }

            if (dataLoadMethod == DataLoadMethodType.ScatteredFile)
            {
                DataManager.LoadScattered(ScatteredDataLoadConfigPath);
            }
            else if (dataLoadMethod == DataLoadMethodType.PackedResourceFile)
            {
                DataManager.LoadPacked(PackedDataPath);
            }

            // 初始化通用 Manager
            if (VFXManager != null)
            {
                VFXManager.Initialize();
            }
            if (AudioManager != null)
            {
                AudioManager.Initialize();
            }

            DontDestroyOnLoad(this);

        }

        void Update()
        {
            InteractionModel.Update();
            float uiDelta = Time.deltaTime;
            float gameDelta = uiDelta * EffectiveGameDilation;
            TickingManager.Tick(gameDelta, uiDelta);
            TimerManager.Tick(uiDelta);
            if (gameDelta > 0f)
            {
                OnUpdate(gameDelta);
            }
        }

        virtual protected void OnUpdate(float delta) { }

        protected override void OnRelease()
        {
            LogUtil.Release();
            base.OnRelease();
        }

    }

}
