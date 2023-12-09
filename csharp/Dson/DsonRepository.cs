#region LICENSE

//  Copyright 2023 wjybxx
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

using Wjybxx.Dson.Text;
using Wjybxx.Dson.Types;

namespace Wjybxx.Dson;

/// <summary>
/// 简单的Dson对象仓库实现 -- 提供简单的引用解析功能。
/// </summary>
public class DsonRepository
{
    private readonly Dictionary<string, DsonValue> indexMap = new();
    private readonly List<DsonValue> valueList = new List<DsonValue>();

    public DsonRepository() {
    }

    public DsonArray<string> ToDsonArray() {
        DsonArray<string> dsonArray = new DsonArray<string>(valueList.Count);
        dsonArray.AddAll(valueList);
        return dsonArray;
    }

    public Dictionary<string, DsonValue> IndexMap => indexMap;

    public List<DsonValue> Values => valueList;

    public int Count => valueList.Count;

    public DsonValue this[int index] => valueList[index];

    public DsonRepository Add(DsonValue value) {
        if (!value.DsonType.IsContainerOrHeader()) {
            throw new ArgumentException();
        }
        valueList.Add(value);

        string localId = Dsons.GetLocalId(value);
        if (localId != null) {
            if (indexMap.Remove(localId, out DsonValue? exist)) {
                DsonInternals.RemoveRef(valueList, exist);
            }
            indexMap[localId] = value;
        }
        return this;
    }

    public DsonValue RemoveAt(int idx) {
        DsonValue dsonValue = valueList[idx];
        valueList.RemoveAt(idx); // 居然没返回值...

        string localId = Dsons.GetLocalId(dsonValue);
        if (localId != null) {
            indexMap.Remove(localId, out DsonValue _);
        }
        return dsonValue;
    }

    public bool Remove(DsonValue dsonValue) {
        int idx = DsonInternals.IndexOfRef(valueList, dsonValue);
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
        if (indexMap.Remove(localId, out DsonValue exist)) {
            DsonInternals.RemoveRef(valueList, exist);
        }
        return exist;
    }

    public DsonValue? Find(string localId) {
        if (localId == null) throw new ArgumentNullException(nameof(localId));
        indexMap.TryGetValue(localId, out DsonValue exist);
        return exist;
    }

    public void ResolveReference() {
        foreach (DsonValue dsonValue in valueList) {
            ResolveReference(dsonValue);
        }
    }

    private void ResolveReference(DsonValue dsonValue) {
        if (dsonValue is AbstractDsonObject<string>
            dsonObject) { // 支持header...
            foreach (KeyValuePair<string, DsonValue> entry in dsonObject) {
                DsonValue value = entry.Value;
                if (value.DsonType == DsonType.Reference) {
                    ObjectRef objectRef = value.AsReference();
                    if (indexMap.TryGetValue(objectRef.LocalId, out DsonValue targetObj)) {
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
                    if (indexMap.TryGetValue(objectRef.LocalId, out DsonValue targetObj)) {
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

    public static DsonRepository FromDson(string dsonString, bool resolveRef = false) {
        DsonRepository repository = new DsonRepository();
        using (DsonTextReader reader = new DsonTextReader(DsonTextReaderSettings.Default, dsonString)) {
            DsonValue value;
            while ((value = Dsons.ReadTopDsonValue(reader)) != null) {
                repository.Add(value);
            }
        }
        if (resolveRef) {
            repository.ResolveReference();
        }
        return repository;
    }

    // 解析引用后可能导致循环，因此equals等不实现
    public override string ToString() {
        // 解析引用后可能导致死循环，因此不输出
        return "DsonRepository:" + base.ToString();
    }
}