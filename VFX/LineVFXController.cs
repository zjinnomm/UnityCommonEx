using UnityEngine;

namespace UnityCommonEx
{

    public class LineVFXController : VFXController
    {

        public void SetLine(Vector3 Start, Vector3 End)
        {
            Quaternion quat = Quaternion.FromToRotation(Vector3.right, (Start - End).normalized);
            Quaternion inverse = Quaternion.Inverse(quat);
            if (RegisteredComponents != null)
            {
                for (int i = 0; i < RegisteredComponents.Length; i++)
                {
                    ref VFXComponentEntry entry = ref RegisteredComponents[i];
                    if (entry.Component != null && entry.Component.IsParticleSystem)
                    {
                        var particle = entry.Component.AsParticleSystem;
                        if (particle != null && particle.shape.shapeType == ParticleSystemShapeType.SingleSidedEdge)
                        {
                            var shape = particle.shape;
                            shape.position = inverse * ((Start + End) * 0.5f);
                        }
                    }
                }
            }
            SetSize((Start - End).magnitude * 0.5f);
            SetRotation(quat.eulerAngles);
        }

    }

}