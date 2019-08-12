﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal class PropertyPageData
    {
        public PropertyPageData(string category, string propertyName, object value, List<object>? setValues = null)
        {
            Category = category;
            PropertyName = propertyName;
            Value = value;
            SetValues = setValues ?? new List<object>();
        }

        public string Category { get; }
        public string PropertyName { get; }
        public object Value { get; }
        public List<object> SetValues { get; }
    }
}
