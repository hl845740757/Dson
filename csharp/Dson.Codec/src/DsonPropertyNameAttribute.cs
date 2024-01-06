#region LICENSE

// Copyright 2023 wjybxx(845740757@qq.com)
// 
// Licensed under the Apache License, Version 2.0 (the "License")

#endregion

using System;

namespace Wjybxx.Dson.Codec;

/// <summary>
/// 该注解用于配置字段序列化时的名字
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class DsonPropertyNameAttribute : Attribute
{
}