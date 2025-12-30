using System;
using System.Collections.Generic;

namespace UnityCommonEx
{
    public interface IAttributeSetOwner<T> where T : Enum
    {

        public void InitSetAttributes(AttributeSet<T> attributeSet);
        public void GetAttributeModifiers(List<AttributeModifier<T>> result);


    }
}