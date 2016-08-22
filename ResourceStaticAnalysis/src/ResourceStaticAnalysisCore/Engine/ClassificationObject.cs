/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.ResourceStaticAnalysis.Core.DataSource;
using Microsoft.ResourceStaticAnalysis.Core.Engine.Properties;
using Microsoft.ResourceStaticAnalysis.Core.Misc;

namespace Microsoft.ResourceStaticAnalysis.Core.Engine
{
    /// <summary>
    /// A predicate expression that takes Classification Object and returns true or false.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="co"></param>
    /// <returns></returns>
    public delegate bool COExpression<T>(T co) where T : ClassificationObject;

    /// <summary>
    /// Allows unified casting of expressions
    /// </summary>
    /// <typeparam name="T">Name of the Classification Object type whose expression is to be casted</typeparam>
    public static class ExpressionCasting<T> where T : ClassificationObject
    {
        /// <summary>
        /// Casts from COExpression T to Func T, bool
        /// </summary>
        /// <param name="expr"></param>
        /// <returns></returns>
        public static COExpression<ClassificationObject> ExpressionCast(object expr)
        {
            if (expr is COExpression<T>)
            {
                return target => ((COExpression<T>)expr).Invoke((T)target);
            }
            else if (expr is Func<T, bool>)
            {
                return target => ((Func<T, bool>)expr).Invoke((T)target);
            }
            else
                throw new InvalidCastException(String.Format(CultureInfo.CurrentCulture, "Cannot cast object of type {0}. Expected type {1} or {2}",
                    expr.GetType().Name, typeof(COExpression<T>).Name, typeof(Func<T, bool>).Name));
        }
    }

    public abstract class ClassificationObject : IEquatable<ClassificationObject>
    {
        /// <summary>
        /// Creates an instance of CO
        /// </summary>
        /// <param name="propertyProvider">Object that provides CO properties based on underlying data sources.</param>
        /// <param name="outputSize">Maximum number of output items that can be stored for the CO. This is equal to the number of rules registered with ResourceStaticAnalysis. This optimizes performance of output logging.</param>
        protected ClassificationObject(PropertyProvider propertyProvider)
        {
            this._propertyProvider = propertyProvider;
            this._properties = new PropertyCollection(this);
        }

        /// <summary>
        /// Unique key. Introduced to optimize accessing objects in space
        /// </summary>
        public uint Key { get; internal set; }
        /// <summary>
        /// Object Space to which this CO belongs.
        /// </summary>

        protected internal Space ObjectSpace { get; internal set; }

        /// <summary>
        /// Make sure to call base implementation first, as it may perform some cleanup for the base class. 
        /// ResourceStaticAnalysis calls this method once
        /// for each type when the first instance of the type is loaded. In this method
        /// you can register any cleanup code you want to run with the ResourceStaticAnalysis.EngineCleanup event.
        /// This cleanup code IS NOT for instances of Classification Objects but for the entire type - i.e.
        /// if you have static fields that cache data you may want to register them for cleanup so in case engine
        /// runs multiple times they can be reset between the multiple runs.
        /// </summary>
        /// <param name="engine">ResourceStaticAnalysis instance for which cleanup we want to listen.</param>
        public virtual void RegisterTypeForEngineCleanup(ResourceStaticAnalysisEngine engine) { }

        /// <summary>
        /// This stores the property provider object that contains property adapters and data sources
        /// for this instance of Classification Object
        /// </summary>
        private readonly PropertyProvider _propertyProvider;

        private readonly PropertyCollection _properties;

        public PropertyCollection Properties
        {
            get
            {
                return _properties;
            }
        }

        public class PropertyCollection
        {
            private static readonly Property[] EmptyPropertyArray = new Property[0];
            internal PropertyCollection(ClassificationObject parentCo)
            {
                this._parentCo = parentCo;
                this._properties = EmptyPropertyArray;
            }

            private Property[] _properties;
            /// <summary>
            /// Used to control access to _properties
            /// </summary>
            private static readonly ReaderWriterLockSlim PropertiesLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
            private readonly ClassificationObject _parentCo;

            public Property this[byte propertyId]
            {
                get
                {
                    Property value = null;
                    bool haveToCreate = false;

                    // get the index of the property using PropertyIndexer - this optimizes memory usage as only properties that are actually
                    // used at runtime will be indexed and the properties array will only grow accordingly to actual usage of properties
                    byte propertyIndex = _parentCo.PropertyIndexer.GetPropertyIndex(propertyId);
                    PropertiesLock.EnterUpgradeableReadLock();
                    try
                    {
                        if (propertyIndex >= _properties.Length)
                        {
                            haveToCreate = true;
                        }
                        else
                        {
                            value = _properties[propertyIndex];
                            if (Object.ReferenceEquals(value, null))
                            {
                                haveToCreate = true;
                            }
                        }
                        if (haveToCreate)
                        {
                            // obtain lock
                            PropertiesLock.EnterWriteLock();
                            try
                            {
                                Array.Resize<Property>(ref _properties, propertyIndex + 4);
                                string propertyName = _parentCo.PropertyEnumManager.GetNameFromId(propertyId);
                                if (!_parentCo._propertyProvider.GetProperty((byte) propertyId, out value))
                                    throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture,
                                        "Property Provider failed to obtain property with id: {0} name: {1}. Check if correct property adapters and data sources have been configured.",
                                        propertyId, propertyName));
                                value.Name = propertyName;
                                _properties[propertyIndex] = value;
                            }
                            finally
                            {
                                PropertiesLock.ExitWriteLock();
                            }
                        }
                    }
                    finally
                    {
                        PropertiesLock.ExitUpgradeableReadLock();
                    }
                    
                    return value;
                }
            }

            /// <summary>
            /// Returns a collection of property values matching the Ids provided
            /// </summary>
            /// <param name="propertyIds">List of property Ids to retrieve.</param>
            /// <returns></returns>
            public ICollection<Property> GetPropertySet(params byte[] propertyIds)
            {
                return GetPropertySet(propertyIds as IEnumerable<byte>);
            }
            public ICollection<Property> GetPropertySet(IEnumerable<byte> propertyIds)
            {
                return (
                    from p in propertyIds
                    select this[p]
                    ).ToArray();
            }
        }

        #region IEquatable<ClassificationObject> Members and operators
        public override bool Equals(object obj)
        {
            ClassificationObject co = obj as ClassificationObject;
            if (System.Object.ReferenceEquals(co, null))
                return false;
            return this.Equals(co);
        }

        public bool Equals(ClassificationObject co)
        {
            if (Object.ReferenceEquals(co, null))
                return false;
            return this.Key.Equals(co.Key);
        }

        public static bool operator ==(ClassificationObject o1, ClassificationObject o2)
        {
            if (System.Object.ReferenceEquals(o1, o2))
                return true;
            if (System.Object.ReferenceEquals(o1, null))
                return false;
            return o1.Equals(o2);
        }
        public static bool operator !=(ClassificationObject o1, ClassificationObject o2)
        {
            return !(o1 == o2);
        }

        /// <summary>
        /// Checks whether two COs are physically different objects in any respect.
        /// </summary>
        /// <param name="o1">Left CO</param>
        /// <param name="o2">Right CO</param>
        /// <returns>True if the objects look the same from all aspects (that is they reside in the same point in abstract space).
        /// False if they are different points in our abstract space.</returns>
        private static bool CompareCOs(ClassificationObject o1, ClassificationObject o2)
        {
            //1. Obvious check for type mismatch
            if (o1.GetType() != o2.GetType()) return false;
            //2. Performance optimization by using hash codes (they are computed from all properties already saving tons of processing time
            if (o1.GetHashCode() != o2.GetHashCode()) return false;
            //3. If the two points still look the same, use brute force comparison of all known properties. If any of these properties mismatch, return false; true otherwise.
            return !(o1.EnabledProperties.Any(property => o1.Properties[property] != o2.Properties[property]));
        }

        /// <summary>
        /// Gets hashcode of the key of the ClassificationObject
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return this.Key.GetHashCode();
        }
        #endregion

        /// <summary>
        /// Returns a collection of property ids supported by this type.
        /// </summary>
        public System.Collections.ObjectModel.ReadOnlyCollection<byte> EnabledProperties
        {
            get
            {
                return this.PropertyEnumManager.PropertyIds;
            }
        }

        /// <summary>
        /// Derived class should create an Enum : byte type that lists all Properties implemented by the type.
        /// This enum type is to be used to reference properties in internal calls to ClassificationObject.Properties as an alternative to using
        /// string keys, for perf reasons.
        /// Derived class should implement a static field of type PropertyEnumManager, passing the Enum type as parameter. THis static field 
        /// should be returned by instance objects via this property.
        /// </summary>
        public abstract PropertyEnumManager<byte> PropertyEnumManager
        {
            get;
        }

        /// <summary>
        /// Each implementation of ClassificationObject should create a static instance fo PropertyIndexer
        /// that will support memory efficient property indexing inside the base CO implemenatation.
        /// Instances of the type derrived from CO should return this static instance using this property.
        /// </summary>
        protected internal abstract PropertyIndexer PropertyIndexer
        {
            get;
        }

        public static IEqualityComparer<ClassificationObject> COComparer
        {
            get { return _keyComparer; }
        }
        private static CoKeyComparer _keyComparer = new CoKeyComparer();

        public class CoKeyComparer : IEqualityComparer<ClassificationObject>
        {

            #region IEqualityComparer<ClassificationObject> Members

            public bool Equals(ClassificationObject x, ClassificationObject y)
            {
                if (x.Key == y.Key)
                    return true;
                else
                    return false;
            }

            public int GetHashCode(ClassificationObject obj)
            {
                return obj.Key.GetHashCode();
            }

            #endregion
        }
        private static CoRefComparer _refComparer = new CoRefComparer();
        public class CoRefComparer : IEqualityComparer<ClassificationObject>
        {

            #region IEqualityComparer<ClassificationObject> Members

            public bool Equals(ClassificationObject x, ClassificationObject y)
            {
                if (System.Object.ReferenceEquals(x, y))
                    return true;
                else
                    return false;
            }

            public int GetHashCode(ClassificationObject obj)
            {
                return (obj as object).GetHashCode();
            }

            #endregion
        }
        #region Back-binding with engine using this object to allow the engine to instruct it when to release its static resources when engine releases
        /// <summary>
        /// Engine calls this method to tell this type that it has loaded this type.
        /// The type should hook up to EngineCleanup event so that when that event is called, this type can flush all resources it has cached.
        /// Engine raises this event when it has finished a session run.
        /// </summary>
        /// <param name="host">Host, whose EngineCleanup event which we should handle when this type should reinitialize</param>
        public static void EngineCleanupHandler(ResourceStaticAnalysisEngine host)
        {
            //Implementation of ClassificationObject should provide its own override for this method (using 'new' keyword)
            //if it contains static resources that grow over time. This way the engine has a way to tell this implementation to free its resources
            //when the engine reinitializes.
            //You can do it by adding a hander such as the following:
            //engineCleanupAction += MyCleanupMethod;
            //And reset your static resources to initial state.
        }
        #endregion

        #region METHODS FOR SEARCHING CO SPACE
        private static readonly ClassificationObject[] EmptyCOArray = new ClassificationObject[0];

        /// <summary>
        /// PUBLIC API
        /// Find objects in space that are similar to the current object.
        /// Similar means that the properties listed in parameter must be the same.
        /// </summary>
        /// <param name="propertyIds">List of properties that must match</param>
        /// <returns>A list of objects that are similar, excluding this object. Empty list if none.</returns>
        public ICollection<ClassificationObject> FindSimilar(IEnumerable<byte> propertyIds)
        {
            COHashTable<ClassificationObject> ret = this._FindSimilar(propertyIds);
            if (ret == null)
                return EmptyCOArray;
            else if (ret.Contains(this))
                ret.Remove(this);
            return ret.Objects;
        }

        /// <summary>
        /// Find objects in space that are similar to the current object.
        /// Similar means that the properties listed in parameter must be the same.
        /// </summary>
        /// <param name="propertyIds">List of properties that must match</param>
        /// <returns>A list of objects that are similar, excluding this object. Empty list if none.</returns>
        protected COHashTable<ClassificationObject> _FindSimilar(IEnumerable<byte> propertyIds)
        {
            if (propertyIds == null || propertyIds.Count() == 0 || !ValidProperties(propertyIds.ToArray()))
                return null;

            return this.GetDimensionIntersection(this.Properties.GetPropertySet(propertyIds));
        }
        /// <summary>
        /// Gets objects from Space with the specified value of Property.
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public COHashTable<ClassificationObject> GetObjectsAt(Property p)
        {
            return this.ObjectSpace.GetObjectsAt(p);
        }
        /// <summary>
        /// Gets an intersection of multiple dimensions at coordinates specified by 
        /// properties listed in params.
        /// </summary>
        /// <param name="properties"></param>
        /// <returns></returns>
        public COHashTable<ClassificationObject> GetDimensionIntersection(params Property[] properties)
        {
            return this.ObjectSpace.GetDimensionIntersection(properties);
        }
        public COHashTable<ClassificationObject> GetDimensionIntersection(ICollection<Property> properties)
        {
            return this.ObjectSpace.GetDimensionIntersection(properties);
        }

        /// <summary>
        /// Opposite to FindSimilar(). Different means an object that has the listed properties different
        /// than the current object. The rest of the properties must be the same
        /// </summary>
        /// <param name="propertyIds">List of properties that must differ.</param>
        /// <returns>A list of different objects.</returns>
        public ICollection<ClassificationObject> FindDifferent(IEnumerable<byte> propertyIds)
        {
            // Algorithm: 
            // 1. Create a list of properties that must be equal.
            // 2. Find objects that have those properties equal. (Set A)
            // 3. From setA remove objects that have diff properties equal to this
            // 4. Return SetA

            // Step 1.
            var propertiesEqual = this.EnabledProperties.Except(propertyIds).ToArray();

            // Step 2.
            COHashTable<ClassificationObject> setA = this._FindSimilar(propertiesEqual);

            // Step 3.
            foreach (var prop in propertyIds)
            {
                setA.ExceptAndRemove(this.ObjectSpace.GetObjectsAt(this.Properties[prop]));
            }

            return setA.Objects;
        }

        #region Dimension selection methods. These allow to select objects around this object in give dimension
        /// <summary>
        /// Gets objects present in x+1 point of the dimension, where x is the value of the dimension property
        /// for this object.
        /// </summary>
        /// <param name="dimensionName">Name of the dimension</param>
        /// <returns></returns>
        public ICollection<ClassificationObject> Next(byte dimensionId)
        {
            return this.NextPrevious(dimensionId, 1);
        }

        /// <summary>
        /// Gets objects present in x+number point of the dimension, where x is the value of the dimension property
        /// for this object.
        /// </summary>
        /// <param name="dimensionName">Name of the dimension</param>
        /// <returns></returns>
        public ICollection<ClassificationObject> Next(byte dimensionId, uint number)
        {
            return this.NextPrevious(dimensionId, (int)number);
        }

        /// <summary>
        /// Gets objects present in x-1 point of the dimension, where x is the value of the dimension property
        /// for this object.
        /// </summary>
        /// <param name="dimensionName">Name of the dimension</param>
        /// <returns></returns>
        public ICollection<ClassificationObject> Previous(byte dimensionId)
        {
            return this.NextPrevious(dimensionId, -1);
        }

        /// <summary>
        /// Gets objects present in x-number point of the dimension, where x is the value of the dimension property
        /// for this object.
        /// </summary>
        /// <param name="dimensionName">Name of the dimension</param>
        /// <returns></returns>
        public ICollection<ClassificationObject> Previous(byte dimensionId, uint number)
        {
            return this.NextPrevious(dimensionId, -1 * (int)number);
        }

        private ICollection<ClassificationObject> NextPrevious(byte dimensionId, int number)
        {
            return this.ObjectSpace.NextPrevious(this.Properties[dimensionId], number);
        }

        /// <summary>
        /// Gets objects present in the range of points (x+/-number) of the dimension, where x is the value of the dimension property
        /// for this object.
        /// </summary>
        /// <param name="dimensionName">Name of the dimension</param>
        /// <returns></returns>
        public ICollection<ClassificationObject> Range(byte dimensionId, int before, int after)
        {
            return this.ObjectSpace.Range(this.Properties[dimensionId], before, after);
        }
        #endregion

        /// <summary>
        /// Gets a Property value to the left of the current location of the 
        /// object in the propertie's dimension.
        /// Example: if this object's property X has value "1" and the dimension for X
        /// has the following points: 0, 1, 2
        /// this will return a property object for 0
        /// </summary>
        /// <param name="propertyId">The Id of the property for which to return the value.</param>
        /// <returns>Returns null if there are no points to the left of the object on the Property's dimension</returns>
        public Property GetPreviousPropertyValue(byte propertyId)
        {
            var retIndex = this.ObjectSpace.GetDimension(propertyId).GetPropertyIndex(this.Properties[propertyId]).Previous;
            return Object.ReferenceEquals(retIndex, null) ? null : retIndex.Value;
        }

        /// <summary>
        /// Gets a Property value to the right of the current location of the 
        /// object in the propertie's dimension.
        /// Example: if this object's property X has value "1" and the dimension for X
        /// has the following points: 0, 1, 2
        /// this will return a property object for 2
        /// </summary>
        /// <param name="propertyId">The Id of the property for which to return the value.</param>
        /// <returns>Returns null if there are no points to the right of the object on the Property's dimension</returns>
        public Property GetNextPropertyValue(byte propertyId)
        {
            var retIndex = this.ObjectSpace.GetDimension(propertyId).GetPropertyIndex(this.Properties[propertyId]).Next;
            return Object.ReferenceEquals(retIndex, null) ? null : retIndex.Value;
        }

        /// <summary>
        /// Check if the property Ids have been explicitly enabled in the derrived CO class.
        /// </summary>
        /// <param name="propertyIds"></param>
        /// <returns></returns>
        private bool ValidProperties(IEnumerable<Byte> propertyIds)
        {
            foreach (Byte prop in propertyIds)
            {
                if (!this.EnabledProperties.Contains(prop))
                    return false;
            }
            return true;
        }
        #endregion
    }
}
