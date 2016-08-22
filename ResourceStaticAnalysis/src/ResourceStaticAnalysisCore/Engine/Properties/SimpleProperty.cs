/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System.Collections.Generic;

namespace Microsoft.ResourceStaticAnalysis.Core.Engine.Properties
{
    /// <summary>
    /// A basic property with no specific methods, operators.
    /// </summary>
    /// <typeparam name="T">Type of value to be stored in property.</typeparam>
    public class SimpleProperty<T> : Property
    {
        public SimpleProperty(byte id, T propertyValue)
            : base(id)
        {
            _value = propertyValue;
        }

        T _value;
        public T Value
        {
            get
            {
                return _value;
            }
        }
        public override object GetValue()
        {
            return _value;
        }
        public static implicit operator T(SimpleProperty<T> sp)
        {
            return sp.Value;
        }

        public static bool operator ==(SimpleProperty<T> p1, T p2)
        {
            return p1.Value.Equals(p2);
        }

        public static bool operator !=(SimpleProperty<T> p1, T p2)
        {
            return !(p1 == p2);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override bool Equals(Property other)
        {
            if (ReferenceEquals(other, null)) return false;
            T cmpValue;
            object obj = other.GetValue();
            if (obj is T)
            {
                cmpValue = (T)obj;
            }
            else
                cmpValue = default(T);

            return EqualityComparer<T>.Default.Equals(Value, cmpValue);
        }
        public override bool Equals(object obj)
        {
            return Value.Equals(obj);
        }
        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
