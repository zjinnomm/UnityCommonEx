using System;

namespace UnityCommonEx
{

    public enum AttributeModifyType : byte
    {
        FlatValueBeforePercent = 0,
        Percent = 1,
        FlatValue = 2,
    }

    public enum AttributeModifyPriority : byte
    {
        Permanent = 0,
        Additional = 1,
        Temporary = 2,
    }

    public struct AttributeModifier<T> where T : Enum
    {
        public AttributeModifyPriority Priority;
        public AttributeModifyType ModifyType;
        public T Attribute;
        public float Value;
    }

    public interface IAttributeModifierProvider<T> where T : Enum
    {

        public AttributeModifier<T> GetAttributeModifier();

    }

}