using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityCommonEx
{

    public partial class AttributeSet<T> where T : Enum
    {

        static int length;
        static readonly float[] defaultValues;
        static readonly float[] minValues;
        static readonly float[] maxValues;
        static readonly bool[] sharedTempModifyFlag;
        static readonly float[] sharedTempModifyValue;
        static readonly float[] oldValues; // 旧值数组（用于对比）
        static readonly bool[] valueChanged; // 值变化标志数组

        static AttributeSet()
        {
            int min = EnumUtil.Min(typeof(T));
            if (min == int.MaxValue || min < 0)
            {
                LogUtil.Error("should never use enum {0} with negative values as attributes", typeof(T).Name);
            }
            length = EnumUtil.Max(typeof(T)) + 1;
            defaultValues = new float[length];
            minValues = new float[length];
            maxValues = new float[length];
            for (int i = 0; i < length; i++)
            {
                defaultValues[i] = 0;
                minValues[i] = float.MinValue;
                maxValues[i] = float.MaxValue;
            }
            sharedTempModifyFlag = new bool[length];
            sharedTempModifyValue = new float[length * 9];
            oldValues = new float[length];
            valueChanged = new bool[length];
        }

        readonly IAttributeSetOwner<T> owner;
        readonly float[] values;

        /// <summary>
        /// 值变化回调，传出 valueChanged 数组（仅在有任何值发生变化时调用）
        /// </summary>
        public Action<bool[]> OnValueChanged;

        public AttributeSet(IAttributeSetOwner<T> owner)
        {
            if (owner == null)
            {
                LogUtil.Error("can not create attribute set without an owner");
                return;
            }
            this.owner = owner;
            values = new float[length];
        }

        public float this[int index]
        {
            get => values[index];
            set => values[index] = value;
        }

        public void ReloadAttributes()
        {
            // 开始时，先将当前 values 保存到 oldValues
            Array.Copy(values, oldValues, length);
            
            Array.Copy(defaultValues, values, length);
            owner.InitSetAttributes(this);
            using (ScopedPoolable<PoolableList<AttributeModifier<T>>> modifiers = new ScopedPoolable<PoolableList<AttributeModifier<T>>>())
            {
                var list = modifiers.Get();
                owner.GetAttributeModifiers(list);
                if (list.Count > 0)
                {
                    Array.Clear(sharedTempModifyFlag, 0, sharedTempModifyFlag.Length);
                    Array.Clear(sharedTempModifyValue, 0, sharedTempModifyValue.Length);
                    foreach (var modifier in list)
                    {
                        int offset = Convert.ToInt32(modifier.Attribute);
                        sharedTempModifyFlag[offset] = true;
                        sharedTempModifyValue[((int)modifier.Priority * 3 + (int)modifier.ModifyType) * length + offset] += modifier.Value;
                    }
                    for (int i = 0; i < length; i++)
                    {
                        if (!sharedTempModifyFlag[i])
                        {
                            continue;
                        }
                        for (int j = 0; j < 9; j++)
                        {
                            float value = sharedTempModifyValue[j * length + i];
                            if (value != 0)
                            {
                                if (j % 3 == (int)AttributeModifyType.Percent)
                                {
                                    values[i] *= 1 + value;
                                }
                                else
                                {
                                    values[i] += value;
                                }
                            }
                        }
                        values[i] = Mathf.Clamp(values[i], minValues[i], maxValues[i]);
                    }
                }
            }
            
            // 结束时，对比前后值，设置 valueChanged 数组
            bool hasAnyChange = false;
            Array.Clear(valueChanged, 0, valueChanged.Length);
            for (int i = 0; i < length; i++)
            {
                if (values[i] != oldValues[i])
                {
                    valueChanged[i] = true;
                    hasAnyChange = true;
                }
            }
            
            // 仅在有任何值发生变化时调用回调
            if (hasAnyChange && OnValueChanged != null)
            {
                OnValueChanged(valueChanged);
            }
        }

    }

}