#region LICENSE

// Copyright 2023 wjybxx(845740757@qq.com)
// 
// Licensed under the Apache License, Version 2.0 (the "License")

#endregion

using System;

#pragma warning disable CS1591
namespace Wjybxx.Dson.Codec;

/// <summary>
/// 该注解用于标注一个字段是否需要被忽略
///
/// 1. 有<see cref="NonSerializedAttribute"/>注解的字段默认不被序列化
/// 2. private字段默认不序列化
/// 3. 属性默认都是不序列化的，通过该注解可将属性序列化
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class DsonIgnoreAttribute : Attribute
{
    /// <summary>
    /// 是否忽略
    /// </summary>
    public readonly bool Value;

    public DsonIgnoreAttribute(bool value = true) {
        Value = value;
    }
}