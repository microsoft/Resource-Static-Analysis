/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace Microsoft.ResourceStaticAnalysis.Core.Engine.Properties
{
    public class PropertyEnumManager<T>
    {
        public PropertyEnumManager(Type enumType)
        {
            if (enumType == null)
            {
                throw new ArgumentNullException(nameof(enumType));
            }
            if (!enumType.IsEnum || !Enum.GetUnderlyingType(enumType).Equals(typeof(T)))
            {
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, "Type must be an enum based on {0}", typeof(T).FullName), nameof(enumType));
            }

            this._enumType = enumType;
            this.PropertyIds = Array.AsReadOnly<T>(Enum.GetValues(enumType).Cast<T>().ToArray());
            this.PropertyNames = Array.AsReadOnly<string>(Enum.GetNames(enumType));
            this._nameToId = new Dictionary<string, T>(PropertyIds.Count, StringComparer.Ordinal);
            foreach (var id in PropertyIds)
            {
                _nameToId.Add(Enum.GetName(enumType, id), id);
            }
        }
        readonly Type _enumType;
        readonly Dictionary<string, T> _nameToId;


        public ReadOnlyCollection<T> PropertyIds { get; private set; }
        public ReadOnlyCollection<string> PropertyNames { get; private set; }

        public string GetNameFromId(T id)
        {
            return Enum.GetName(_enumType, id);
        }

        public T GetIdFromName(string name)
        {
            return _nameToId[name];
        }


    }
}
