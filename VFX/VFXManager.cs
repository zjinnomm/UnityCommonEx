using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace UnityCommonEx
{

    public class VFXManager : SingletonController<VFXManager>
    {

        public string VFXRootResPath;
        
        DataTable<string, VFXConfigRow> vfxConfigTable;
        Dictionary<string, AsyncOperationHandle<GameObject>> vfxHandles = new Dictionary<string, AsyncOperationHandle<GameObject>>();
        long nextSerialNumber = 0;

        protected override bool IsPersistent => true;

        struct PlayingVFX
        {
            public VFXController vfx;
            public float time;
        }
        List<PlayingVFX> playingVFXs = new List<PlayingVFX>();

        public void Initialize()
        {
            DontDestroyOnLoad(gameObject);
            vfxConfigTable = DataTableManager.Get<string, VFXConfigRow>();
            if (vfxConfigTable == null)
            {
                LogUtil.Error("lack vfx config data");
            }
        }

        public VFXController GetVFX(string name)
        {
            var vfx = CategorizedPool<VFXController, string>.Instance.GetInstance(name, (name) =>
            {
                VFXConfigRow row = vfxConfigTable.GetRow(name);
                if (row == null)
                {
                    LogUtil.Warn("vfx {0} not found", name);
                    return null;
                }
                string path = $"{VFXRootResPath}/{row.Path}";
                var result = ResourceUtil.LoadSync<GameObject>(path);
                if (!result.IsValid)
                {
                    LogUtil.Warn("vfx {0}'s path {1} is not valid", name, row.Path);
                    return null;
                }
                vfxHandles[name] = result.Handle;
                var created = Create<VFXController>(result.Asset, transform);
                if (created != null)
                {
                    created.prefabController = result.Asset.GetComponent<VFXController>();
                }
                return created;
            });
            vfx.SetSerialNumber(nextSerialNumber);
            nextSerialNumber++;
            return vfx;
        }

        public void ReturnVFX(VFXController vfx, bool immediatelyClear = false)
        {
            if (vfx.SerialNumber == -1)
            {
                return;
            }
            if (vfx.LingerDuration > 0 && !immediatelyClear)
            {
                vfx.transform.parent = transform;
                vfx.StopVFX();
                playingVFXs.Add(new PlayingVFX { vfx = vfx, time = vfx.LingerDuration });
            }
            else
            {
                vfx.Deactivate();
                vfx.transform.parent = transform;
                CategorizedPool<VFXController, string>.Instance.ReturnInstance(vfx);
            }
        }

        public VFXController PlayVFX(string name, Vector3 position)
        {
            return PlayVFX(name, position, Vector3.zero, 1);
        }

        public VFXController PlayVFX(string name, Vector3 position, Vector3 rotation, float size, float duration = -1)
        {
            VFXController vfx = GetVFX(name);
            if (vfx == null)
            {
                return null;
            }
            vfx.transform.position = position;
            vfx.Rotation = rotation;
            vfx.Size = size;
            if (duration <= 0)
            {
                duration = vfx.DefaultDuration + vfx.LingerDuration;
            }
            vfx.StartVFX();
            playingVFXs.Add(new PlayingVFX { vfx = vfx, time = duration });
            return vfx;
        }

        public void ClearVFX()
        {
            foreach(PlayingVFX vfx in playingVFXs)
            {
                ReturnVFX(vfx.vfx, true);
            }
            playingVFXs.Clear();
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            for (int i = playingVFXs.Count - 1; i >= 0; i--)
            {
                PlayingVFX playingVFX = playingVFXs[i];
                if (playingVFX.vfx == null)
                {
                    playingVFXs.RemoveAt(i);
                }
                playingVFX.time -= deltaTime;
                if (playingVFX.time <= 0)
                {
                    playingVFXs.RemoveAt(i);
                    ReturnVFX(playingVFX.vfx, true);
                }
                else
                {
                    playingVFXs[i] = playingVFX;
                }
            }
        }

        protected override void OnRelease()
        {
            foreach (var handle in vfxHandles.Values)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }
            vfxHandles.Clear();
            base.OnRelease();
        }

    }

}