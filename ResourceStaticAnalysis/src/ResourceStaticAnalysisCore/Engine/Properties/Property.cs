/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.ResourceStaticAnalysis.Core.Engine.Properties
{
    /// <summary>
    /// An individual property of a classification object.
    /// </summary>
    public abstract class Property : IEquatable<Property>, IComparable<Property>
    {
        /// <summary>
        /// Name of the property.
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// Efficient ID object that can be used to compare property name without using the string.
        /// Using byte, assuming that there will be less than 256 properties for every Classification Object type
        /// </summary>
        public byte Id { get; private set; }

        /// <summary>
        /// The property value. This should only be used internally by the Property class to offer generic methods for comparison, hashcode, etc.
        /// Boxing occurs if the implemented class stores a value object instead of a reference object.
        /// </summary>
        public abstract object GetValue();

        protected Property(byte id)
        {
            Id = id;
        }

        /// <summary>
        /// This allows us to call Property.ToString() instead of Property.Value.ToString() in output writes.
        /// So we can control how we want to write a particular property in its definition without changing outputwriters.
        /// Some more complex properties may want to do fancier stuff than just value.ToString();
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (Object.ReferenceEquals(GetValue(), null))
            {
                Trace.TraceWarning(
                    "There is a problem with property {0}. Property.Value = null. Check data adapter implementation!",
                    Name);
                return String.Empty;
            }

            return GetValue().ToString();
        }

        #region Overriding Equals and operators
        /// <summary>
        /// Override in your property implementation. The desired logic is to compare Values of two Property objects even if these objects
        /// refer to different properties. So for example comparing a SourceString property against TargetString should return true if values
        /// are the same.
        /// </summary>
        /// <returns>True if Values are equal</returns>
        public abstract bool Equals(Property other);

        /// <summary>
        /// Override in your property implementation. The desired logic is to compare Value of the property against the object.
        /// </summary>
        /// <param name="obj">Object to compare against</param>
        /// <returns>True if obj is the same type and Properties are equal</returns>
        public abstract override bool Equals(object obj);

        /// <summary>
        /// Checks if you can compare the values of this property against "otherProperty".
        /// Returns false if Property types are different or "otherProperty" is null or if property Names are different
        /// </summary>
        protected bool CanBeCompared(Property otherProperty)
        {
            if (ReferenceEquals(otherProperty, null)) return false;
            if (!GetType().Equals(otherProperty.GetType())) return false;
            if (!Id.Equals(otherProperty.Id)) return false;
            return true;
        }

        /// <summary>
        /// Uses Property.Equals overriden by the Property implementation. The correct logic is comparing
        /// the Values of the two Properties. Lookup Property.Equals(Property) for more details.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>True if values of the two proprties are equal.</returns>
        public static bool operator ==(Property x, Property y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            return x.Equals(y);
        }

        /// <summary>
        /// Uses Property.Equals overriden by the Property implementation. The correct logic is comparing
        /// the Values of the two Properties. Lookup Property.Equals(Property) for more details.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>True if values of the two proprties are different.</returns>
        public static bool operator !=(Property x, Property y)
        {
            return !(x == y);
        }

        /// <summary>
        /// Uses Property.Equals overriden by the Property implementation. The correct logic is comparing
        /// the Value of the Property with the object. Lookup Property.Equals(object) for more details.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>True if value of the property is equal to the object.</returns>
        public static bool operator ==(Property p, object obj)
        {
            if (ReferenceEquals(p, null))
            {
                if (ReferenceEquals(obj, null))
                    return true;
                else
                    return false;
            }
            return p.Equals(obj);
        }

        /// <summary>
        /// Uses Property.Equals overriden by the Property implementation. The correct logic is comparing
        /// the Value of the Property with the object. Lookup Property.Equals(object) for more details.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>True if value of the property is not equal to the object.</returns>
        public static bool operator !=(Property p, object obj)
        {
            return !(p == obj);
        }

        /// <summary>
        /// Gets hash code based on property name and value.
        /// </summary>
        /// <returns>Hashcode of the Value object in Property</returns>
        public override int GetHashCode()
        {
            var value = GetValue();
            try
            {
                if (Object.ReferenceEquals(value, null))
                {
                    Trace.TraceWarning(
                        "There is a problem with property {0}. Property.Value = null. Check data adapter implementation!",
                        Name);
                    return Id.GetHashCode();
                }
                else
                {
                    return Id.GetHashCode() ^ value.GetHashCode();
                }
            }
            catch (NullReferenceException)
            {
                return 0;
            }
        }
        #endregion

        #region IComparable<Property> Members
        /// <summary>
        /// Compares the Value objects of two properties.
        /// Relies on the implementation of the IComparable in Value types.
        /// If your value type does not implement IComparable, override this method
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public virtual int CompareTo(Property other)
        {
            return DefaultPropertyCompare(this, other);
        }
        #endregion

        static int DefaultPropertyCompare(Property x, Property y)
        {
            object v1 = ReferenceEquals(x, null) ? null : x.GetValue();
            object v2 = ReferenceEquals(y, null) ? null : y.GetValue();
            if (ReferenceEquals(v1, null) || !(v1 is IComparable))
            {
                if (!ReferenceEquals(v2, null) && (v2 is IComparable))
                {
                    return (v2 as IComparable).CompareTo(v1) * -1;
                }
                return 0;
            }
            if (v1 is IComparable)
            {
                return (v1 as IComparable).CompareTo(v2);
            }
            return 0;
        }

        #region disabling property by ref comparison
        /*
        private static readonly PropertyRefEqualityComparer refComparer = new PropertyRefEqualityComparer();
        /// <summary>
        /// Instance of a Property comparer that compares by reference.
        /// </summary>
        public static PropertyRefEqualityComparer RefComparer
        {
            get { return refComparer; }
        }
        /// <summary>
        /// Used to compare two Property objects by reference.
        /// </summary>
        public class PropertyRefEqualityComparer : IEqualityComparer<Property>
        {

            #region IEqualityComparer<Property> Members

            public bool Equals(Property x, Property y)
            {
                return Object.ReferenceEquals(x, y);
            }

            public int GetHashCode(Property obj)
            {
                return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
            }
            #endregion
        }*/
        #endregion

        private static readonly PropertyNameAndValueEqualityComparer _nameAndValueComparer = new PropertyNameAndValueEqualityComparer();
        /// <summary>
        /// Instance of a Property Comparer that compares both Name and Value of the property.
        /// Name comparison is an Ordinal string comparison.
        /// Value comparison is done through call to Property.Equals(Property) that must be implemented by each Property derrived class.
        /// </summary>
        public static PropertyNameAndValueEqualityComparer NameAndValueComparer
        {
            get { return _nameAndValueComparer; }
        }

        /// <summary>
        /// Used to compare two Property objects by looking at property name and property value.
        /// This differs from the default Equals implementation which only looks at value.
        /// </summary>
        public class PropertyNameAndValueEqualityComparer : IEqualityComparer<Property>
        {
            #region IEqualityComparer<Property> Members

            public bool Equals(Property x, Property y)
            {
                if (ReferenceEquals(x, null))
                {
                    if (ReferenceEquals(y, null))
                    {
                        return true;
                    }
                    return false;
                }
                if (ReferenceEquals(y, null))
                {
                    return false;
                }
                return x.Id.Equals(y.Id) && x.Equals(y);
            }

            public int GetHashCode(Property obj)
            {
                return obj.GetHashCode();
            }
            #endregion
        }
    }
}
