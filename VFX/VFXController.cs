using System;
using UnityEngine;

namespace UnityCommonEx
{

    [Serializable]
    public class VFXComponent
    {
        [SerializeField]
        private Component component;

        public Component Component => component;

        public bool IsSpriteRenderer => component is SpriteRenderer;
        public bool IsParticleSystem => component is ParticleSystem;
        public bool IsTrailRenderer => component is TrailRenderer;

        public SpriteRenderer AsSpriteRenderer => component as SpriteRenderer;
        public ParticleSystem AsParticleSystem => component as ParticleSystem;
        public TrailRenderer AsTrailRenderer => component as TrailRenderer;

        public bool IsValid => IsSpriteRenderer || IsParticleSystem || IsTrailRenderer;
    }

    [Serializable]
    public struct VFXComponentEntry
    {
        public VFXComponent Component;
        public VFXResizeScaleStrategy ScaleStrategy;
        public VFXResizeDensityStrategy AmountStrategy;
        public VFXLingerStrategy LingerStrategy;
    }

    public class VFXController : BaseCategorizedPoolableController
    {
        public VFXRootResizeStrategy RootResizeStrategy = VFXRootResizeStrategy.Default;
        public VFXComponentEntry[] RegisteredComponents;
        public float LingerDuration = 0;
        [HideInInspector]
        public VFXController prefabController;

        protected float size = 1;
        protected Vector3 rotation = new Vector3(0, 0, 0);
        protected long serialNumber = -1;

        public float Size { set => SetSize(value); get => size; }
        public Vector3 Rotation { set => SetRotation(value); get => rotation; }
        public long SerialNumber => serialNumber;

        public float DefaultDuration
        {
            get
            {
                float duration = 0;
                if (RegisteredComponents != null)
                {
                    for (int i = 0; i < RegisteredComponents.Length; i++)
                    {
                        ref VFXComponentEntry entry = ref RegisteredComponents[i];
                        if (entry.Component == null || !entry.Component.IsValid)
                        {
                            continue;
                        }
                        
                        if (entry.Component.IsParticleSystem)
                        {
                            var particle = entry.Component.AsParticleSystem;
                            if (particle != null)
                            {
                                duration = Mathf.Max(duration, particle.totalTime);
                            }
                        }
                    }
                }
                return duration;
            }
        }

        public void SetSerialNumber(long serialNumber)
        {
            this.serialNumber = serialNumber;
        }

        public void SetSize(float size)
        {
            if (this.size == size)
            {
                return;
            }
            this.size = size;

            switch (RootResizeStrategy)
            {
                case VFXRootResizeStrategy.Default: transform.localScale = Vector3.one * size; break;
                default: break;
            }

            if (RegisteredComponents != null)
            {
                for (int i = 0; i < RegisteredComponents.Length; i++)
                {
                    ref VFXComponentEntry entry = ref RegisteredComponents[i];
                    if (entry.Component == null || !entry.Component.IsValid)
                    {
                        continue;
                    }
                    ModifyComponent(ref entry, prefabController.RegisteredComponents[i].Component);
                }
            }
        }

        void ModifyComponent(ref VFXComponentEntry entry, VFXComponent prefab)
        {
            if (entry.Component.IsSpriteRenderer)
            {
                ModifySpriteRenderer(entry.Component.AsSpriteRenderer, prefab.AsSpriteRenderer, entry.ScaleStrategy);
            }
            else if (entry.Component.IsParticleSystem)
            {
                ModifyParticleSystem(entry.Component.AsParticleSystem, prefab.AsParticleSystem, entry.ScaleStrategy, entry.AmountStrategy);
            }
            else if (entry.Component.IsTrailRenderer)
            {
                ModifyTrailRenderer(entry.Component.AsTrailRenderer, prefab.AsTrailRenderer, entry.ScaleStrategy);
            }
        }

        void ModifySpriteRenderer(SpriteRenderer spriteRenderer, SpriteRenderer prefab, VFXResizeScaleStrategy scaleStrategy)
        {
            if (spriteRenderer == null || prefab == null) return;

            switch (scaleStrategy)
            {
                case VFXResizeScaleStrategy.Linear:
                    spriteRenderer.transform.localScale = Vector3.one * size;
                    break;
                default:
                    spriteRenderer.transform.localScale = Vector3.one;
                    break;
            }
        }

        void ModifyParticleSystem(ParticleSystem particle, ParticleSystem prefab, VFXResizeScaleStrategy scaleStrategy, VFXResizeDensityStrategy amountStrategy)
        {
            if (particle == null || prefab == null) return;

            var shape = particle.shape;
            switch (scaleStrategy)
            {
                case VFXResizeScaleStrategy.Linear:
                    shape.radius = prefab.shape.radius * size;
                    break; 
                default:
                    shape.radius = prefab.shape.radius;
                    break;
            }

            var emission = particle.emission;
            switch (amountStrategy)
            {
                case VFXResizeDensityStrategy.Linear:
                    emission.rateOverTimeMultiplier = prefab.emission.rateOverTimeMultiplier * size;
                    emission.rateOverDistanceMultiplier = prefab.emission.rateOverDistanceMultiplier * size;
                    break;
                case VFXResizeDensityStrategy.Quadratic:
                    emission.rateOverTimeMultiplier = prefab.emission.rateOverTimeMultiplier * size * size;
                    emission.rateOverDistanceMultiplier = prefab.emission.rateOverDistanceMultiplier * size * size;
                    break;
                default:
                    emission.rateOverTimeMultiplier = prefab.emission.rateOverTimeMultiplier;
                    emission.rateOverDistanceMultiplier = prefab.emission.rateOverDistanceMultiplier;
                    break;
            }
        }

        void ModifyTrailRenderer(TrailRenderer trailRenderer, TrailRenderer prefab, VFXResizeScaleStrategy scaleStrategy)
        {
            if (trailRenderer == null || prefab == null) return;

            switch (scaleStrategy)
            {
                case VFXResizeScaleStrategy.Linear:
                    trailRenderer.startWidth = prefab.startWidth * size;
                    trailRenderer.endWidth = prefab.endWidth * size;
                    break;
                default:
                    trailRenderer.startWidth = prefab.startWidth;
                    trailRenderer.endWidth = prefab.endWidth;
                    break;
            }
        }

        public void SetRotation(Vector3 newRotation)
        {
            if (rotation == newRotation)
            {
                return;
            }
            rotation = newRotation;
            transform.rotation = Quaternion.Euler(newRotation);
        }

        public void StartVFX()
        {
            if (RegisteredComponents != null)
            {
                for (int i = 0; i < RegisteredComponents.Length; i++)
                {
                    ref VFXComponentEntry entry = ref RegisteredComponents[i];
                    if (entry.Component == null || !entry.Component.IsValid)
                    {
                        continue;
                    }
                    StartComponent(ref entry);
                }
            }
        }

        void StartComponent(ref VFXComponentEntry entry)
        {
            if (entry.Component.IsSpriteRenderer)
            {
                var spriteRenderer = entry.Component.AsSpriteRenderer;
                if (spriteRenderer != null)
                {
                    spriteRenderer.enabled = true;
                }
            }
            else if (entry.Component.IsParticleSystem)
            {
                var particle = entry.Component.AsParticleSystem;
                if (particle != null)
                {
                    particle.Play();
                }
            }
            else if (entry.Component.IsTrailRenderer)
            {
                var trailRenderer = entry.Component.AsTrailRenderer;
                if (trailRenderer != null)
                {
                    trailRenderer.enabled = true;
                    trailRenderer.Clear();
                    trailRenderer.emitting = true;
                }
            }
        }

        public void StopVFX()
        {
            if (RegisteredComponents != null)
            {
                for (int i = 0; i < RegisteredComponents.Length; i++)
                {
                    ref VFXComponentEntry entry = ref RegisteredComponents[i];
                    if (entry.Component == null || !entry.Component.IsValid)
                    {
                        continue;
                    }
                    StopComponent(ref entry);
                }
            }
        }

        void StopComponent(ref VFXComponentEntry entry)
        {
            if (entry.Component.IsSpriteRenderer)
            {
                var spriteRenderer = entry.Component.AsSpriteRenderer;
                if (spriteRenderer != null)
                {
                    switch (entry.LingerStrategy)
                    {
                        case VFXLingerStrategy.Stop:
                            // 保持显示，不做任何改变
                            break;
                        case VFXLingerStrategy.StopAndHide:
                            spriteRenderer.enabled = false;
                            break;
                        default:
                            break;
                    }
                }
            }
            else if (entry.Component.IsParticleSystem)
            {
                var particle = entry.Component.AsParticleSystem;
                if (particle != null)
                {
                    particle.Stop();
                    switch (entry.LingerStrategy)
                    {
                        case VFXLingerStrategy.Stop:
                            // 保持粒子系统停止状态
                            break;
                        case VFXLingerStrategy.StopAndHide:
                            particle.gameObject.SetActive(false);
                            break;
                        default:
                            break;
                    }
                }
            }
            else if (entry.Component.IsTrailRenderer)
            {
                var trailRenderer = entry.Component.AsTrailRenderer;
                if (trailRenderer != null)
                {
                    trailRenderer.emitting = false;
                    switch (entry.LingerStrategy)
                    {
                        case VFXLingerStrategy.Stop:
                            // 保持显示，不做任何改变
                            break;
                        case VFXLingerStrategy.StopAndHide:
                            trailRenderer.enabled = false;
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        public void ClearVFX()
        {
            if (RegisteredComponents != null)
            {
                for (int i = 0; i < RegisteredComponents.Length; i++)
                {
                    ref VFXComponentEntry entry = ref RegisteredComponents[i];
                    if (entry.Component == null || !entry.Component.IsValid)
                    {
                        continue;
                    }
                    ClearComponent(ref entry);
                }
            }
        }

        void ClearComponent(ref VFXComponentEntry entry)
        {
            if (entry.Component.IsSpriteRenderer)
            {
                var spriteRenderer = entry.Component.AsSpriteRenderer;
                if (spriteRenderer != null)
                {
                    spriteRenderer.enabled = false;
                }
            }
            else if (entry.Component.IsParticleSystem)
            {
                var particle = entry.Component.AsParticleSystem;
                if (particle != null)
                {
                    particle.Stop();
                    particle.Clear();
                }
            }
            else if (entry.Component.IsTrailRenderer)
            {
                var trailRenderer = entry.Component.AsTrailRenderer;
                if (trailRenderer != null)
                {
                    trailRenderer.enabled = false;
                    trailRenderer.Clear();
                }
            }
        }

        public bool IsPlaying()
        {
            if (RegisteredComponents != null)
            {
                for (int i = 0; i < RegisteredComponents.Length; i++)
                {
                    ref VFXComponentEntry entry = ref RegisteredComponents[i];
                    if (entry.Component == null || !entry.Component.IsValid)
                    {
                        continue;
                    }

                    if (IsComponentPlaying(ref entry))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        bool IsComponentPlaying(ref VFXComponentEntry entry)
        {
            if (entry.Component.IsSpriteRenderer)
            {
                var spriteRenderer = entry.Component.AsSpriteRenderer;
                return spriteRenderer != null && spriteRenderer.enabled;
            }
            else if (entry.Component.IsParticleSystem)
            {
                var particle = entry.Component.AsParticleSystem;
                return particle != null && particle.IsAlive();
            }
            else if (entry.Component.IsTrailRenderer)
            {
                var trailRenderer = entry.Component.AsTrailRenderer;
                return trailRenderer != null && trailRenderer.enabled;
            }

            return false;
        }

        public override void OnPoolableReturned()
        {
            serialNumber = -1;
            base.OnPoolableReturned();
        }

        protected override void OnDeactivate()
        {
            base.OnDeactivate();
            ClearVFX();
        }

    }
}