﻿/*
 * Copyright 2023 wjybxx(845740757@qq.com)
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

namespace Dson;

public class DsonNull : DsonValue, IEquatable<DsonNull>
{
    public static readonly DsonNull Null = new DsonNull();
    /** 可用于特殊情况下的测试 -- 一般不建议使用 */
    public static readonly DsonNull Undefine = new DsonNull();

    public override DsonType DsonType => DsonType.NULL;

    #region equals

    public bool Equals(DsonNull? other) {
        return !ReferenceEquals(other, null);
    }

    public override bool Equals(object? obj) {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((DsonNull)obj);
    }

    public override int GetHashCode() {
        return 0;
    }

    public static bool operator ==(DsonNull? left, DsonNull? right) {
        return Equals(left, right);
    }

    public static bool operator !=(DsonNull? left, DsonNull? right) {
        return !Equals(left, right);
    }

    #endregion

    public override string ToString() {
        return $"{nameof(DsonType)}: {DsonType}";
    }
}