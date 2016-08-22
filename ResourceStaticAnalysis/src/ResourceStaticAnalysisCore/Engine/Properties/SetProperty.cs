/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ResourceStaticAnalysis.Core.Misc;

namespace Microsoft.ResourceStaticAnalysis.Core.Engine.Properties
{
    /// <summary>
    /// A property representing a readonly set of objects of a type T. Internally uses a hash table for efficient lookups
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SetProperty<T> : Property, IReadOnlySet<T>
    {
        public SetProperty(byte propertyId, IEnumerable<T> propertyValue)
            : base(propertyId)
        {
            _value = new ReadonlyHashSet<T>(propertyValue);
        }

        public SetProperty(byte propertyId, IEnumerable<T> propertyValue, IEqualityComparer<T> comparer)
            : base(propertyId)
        {
            _value = new ReadonlyHashSet<T>(propertyValue, comparer);
        }
        private readonly ReadonlyHashSet<T> _value;

        public IReadOnlySet<T> Value
        {
            get { return _value; }
        }
        public override object GetValue()
        {
            return _value;
        }

        public override bool Equals(Property other)
        {
            if (ReferenceEquals(other, null))
                return false;
            var otherCollection = other.GetValue() as IReadOnlySet<T>;

            return ReferenceEquals(this._value, otherCollection) || this._value.SetEquals(otherCollection);
        }

        public override bool Equals(object obj)
        {
            return Value.Equals(obj);
        }
        public override string ToString()
        {
            return String.Join(";", _value.Select(item => item.ToString()).ToArray());
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        #region IReadOnlySet<T> Members

        public int Count
        {
            get { return _value.Count; }
        }

        public bool Contains(T item)
        {
            return _value.Contains(item);
        }

        public bool SetEquals(IReadOnlySet<T> other)
        {
            return _value.SetEquals(other);
        }

        #endregion

        #region IEnumerable<T> Members

        public IEnumerator<T> GetEnumerator()
        {
            return _value.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _value.GetEnumerator();
        }

        #endregion
    }
}
