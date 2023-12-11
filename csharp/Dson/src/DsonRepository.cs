#region LICENSE

//  Copyright 2023 wjybxx(845740757@qq.com)
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to iBn writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

#endregion

using Wjybxx.Dson.Types;

namespace Wjybxx.Dson;

/// <summary>
/// 简单的Dson对象仓库实现 -- 提供简单的引用解析功能。
/// </summary>
public class DsonRepository
{
    private readonly Dictionary<string, DsonValue> _indexMap = new();
    private readonly DsonArray<string> _container;

    public DsonRepository() {
        _container = new();
    }

    public DsonRepository(DsonArray<string> container) {
        _container = container ?? throw new ArgumentNullException(nameof(container));
        foreach (var dsonValue in container) {
            string localId = Dsons.GetLocalId(dsonValue);
            if (localId != null) {
                _indexMap[localId] = dsonValue;
            }
        }
    }

    public Dictionary<string, DsonValue> IndexMap => _indexMap;

    /// <summary>
    /// 顶层容器
    /// </summary>
    public DsonArray<string> Container => _container;

    public int Count => _container.Count;

    public DsonValue this[int index] => _container[index];

    public DsonRepository Add(DsonValue value) {
        if (!value.DsonType.IsContainerOrHeader()) {
            throw new ArgumentException();
        }
        _container.Add(value);

        string localId = Dsons.GetLocalId(value);
        if (localId != null) {
            if (_indexMap.Remove(localId, out DsonValue exist)) {
                DsonInternals.RemoveRef(_container, exist);
            }
            _indexMap[localId] = value;
        }
        return this;
    }

    public DsonValue RemoveAt(int idx) {
        DsonValue dsonValue = _container[idx];
        _container.RemoveAt(idx); // 居然没返回值...

        string localId = Dsons.GetLocalId(dsonValue);
        if (localId != null) {
            _indexMap.Remove(localId, out DsonValue _);
        }
        return dsonValue;
    }

    public bool Remove(DsonValue dsonValue) {
        int idx = DsonInternals.IndexOfRef(_container, dsonValue);
        if (idx >= 0) {
            RemoveAt(idx);
            return true;
        }
        else {
            return false;
        }
    }

    public DsonValue? RemoveById(string localId) {
        if (localId == null) throw new ArgumentNullException(nameof(localId));
        if (_indexMap.Remove(localId, out DsonValue exist)) {
            DsonInternals.RemoveRef(_container, exist);
        }
        return exist;
    }

    public DsonValue? Find(string localId) {
        if (localId == null) throw new ArgumentNullException(nameof(localId));
        _indexMap.TryGetValue(localId, out DsonValue exist);
        return exist;
    }

    public DsonRepository ResolveReference() {
        foreach (DsonValue dsonValue in _container) {
            ResolveReference(dsonValue);
        }
        return this;
    }

    private void ResolveReference(DsonValue dsonValue) {
        if (dsonValue is AbstractDsonObject<string>
            dsonObject) { // 支持header...
            foreach (KeyValuePair<string, DsonValue> entry in dsonObject) {
                DsonValue value = entry.Value;
                if (value.DsonType == DsonType.Reference) {
                    ObjectRef objectRef = value.AsReference();
                    if (_indexMap.TryGetValue(objectRef.LocalId, out DsonValue targetObj)) {
                        dsonObject[entry.Key] = targetObj; // 迭代时覆盖值是安全的
                    }
                }
                else if (value.DsonType.IsContainer()) {
                    ResolveReference(value);
                }
            }
        }
        else if (dsonValue is DsonArray<string> dsonArray) {
            for (int i = 0; i < dsonArray.Count; i++) {
                DsonValue value = dsonArray[i];
                if (value.DsonType == DsonType.Reference) {
                    ObjectRef objectRef = value.AsReference();
                    if (_indexMap.TryGetValue(objectRef.LocalId, out DsonValue targetObj)) {
                        dsonArray[i] = targetObj;
                    }
                }
                else if (value.DsonType.IsContainer()) {
                    ResolveReference(value);
                }
            }
        }
    }

    //

    public static DsonRepository FromDson(IDsonReader<string> reader, bool resolveRef = false) {
        using (reader) {
            DsonArray<string> topContainer = Dsons.ReadTopContainer(reader);
            DsonRepository repository = new DsonRepository(topContainer);
            if (resolveRef) {
                repository.ResolveReference();
            }
            return repository;
        }
    }

    // 解析引用后可能导致循环，因此equals等不实现
    public override string ToString() {
        // 解析引用后可能导致死循环，因此不输出
        return "DsonRepository:" + base.ToString();
    }
}