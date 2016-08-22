/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System;

namespace Microsoft.ResourceStaticAnalysis.Core.Engine.Properties
{
    /// <summary>
    /// A property that is a floating number value. Provides convenience methods to operate on numbers.
    /// </summary>
    public class NumberProperty : Property
    {
        public NumberProperty(byte propertyId, float propertyValue) : base(propertyId) { _value = propertyValue; }

        private float _value;
        public float Value
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

        /// <summary>
        /// Value is within the range specified. Range is inclusive of n and m.
        /// </summary>
        /// <param name="n"></param>
        /// <param name="m"></param>
        /// <returns></returns>
        public bool WithinRange(float n, float m)
        {
            float x = this.Value;
            if (x >= n && x <= m)
                return true;

            else return false;
        }

        public static implicit operator int(NumberProperty np)
        {
            if (ReferenceEquals(np, null))
            {
                return default(int);
            }
            return (int)Math.Truncate(np.Value);

        }

        public static implicit operator float(NumberProperty np)
        {
            if (ReferenceEquals(np, null))
            {
                return default(float);
            }
            return np.Value;

        }
        public override bool Equals(Property other)
        {
            if (ReferenceEquals(other, null))
                return false;
            else
            {
                float cmpValue;
                object objValue = other.GetValue();
                if (objValue is float)
                {
                    cmpValue = (float)objValue;
                }
                else
                {
                    cmpValue = default(float);
                }
                return this.Value.Equals(cmpValue);
            }

        }
        public override string ToString()
        {
            return Value.ToString();
        }

        public override bool Equals(object obj)
        {
            return Value.Equals(obj);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }
}
