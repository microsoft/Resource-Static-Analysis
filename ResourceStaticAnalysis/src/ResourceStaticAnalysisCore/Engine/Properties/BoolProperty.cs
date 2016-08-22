/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

namespace Microsoft.ResourceStaticAnalysis.Core.Engine.Properties
{
    /// <summary>
    /// Property representing bool value. Adds convenience methods for clearer rules.
    /// </summary>
    public class BoolProperty : Property
    {
        public BoolProperty(byte propertyId, bool propertyValue) : base(propertyId) { _value = propertyValue; }

        private bool _value;
        public bool Value
        {
            get { return _value; }
        }

        public override object GetValue()
        {
            return _value;
        }

        /// <summary>
        /// Returns true if property value is true
        /// </summary>
        public bool IsTrue
        {
            get { return this.Value; }
        }

        /// <summary>
        /// Returns true if property value is false
        /// </summary>
        public bool IsFalse
        {
            get { return !this.Value; }
        }

        public static implicit operator bool(BoolProperty bp)
        {
            return bp.Value;
        }

        public override bool Equals(Property other)
        {
            if (ReferenceEquals(other, null))
                return false;

            bool cmpValue;
            object objValue = other.GetValue();
            if (objValue is bool)
            {
                cmpValue = (bool)objValue;
            }
            else
            {
                cmpValue = default(bool);
            }

            return this.Value.Equals(cmpValue);
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
