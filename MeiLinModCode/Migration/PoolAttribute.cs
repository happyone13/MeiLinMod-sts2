using System;

namespace MeiLinMod.MeiLinModCode.Migration;

[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
internal sealed class PoolAttribute(Type poolType) : Attribute
{
    public Type PoolType { get; } = poolType;
}
