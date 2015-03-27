//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.EMIC.Cloud.Notification
{
    /// <summary>
    /// This class is used a serializable KeyValuePair container
    /// </summary>
    /// <typeparam name="K">Key</typeparam>
    /// <typeparam name="V">Value</typeparam>
    [Serializable]
    public class SerializableKeyValuePair<K, V>
    {
        /// <summary>
        /// Gets or sets the key.
        /// </summary>
        /// <value>
        /// The key.
        /// </value>
        public K Key { get; set; }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        public V Value { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializableKeyValuePair&lt;K, V&gt;"/> class.
        /// </summary>
        public SerializableKeyValuePair()
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializableKeyValuePair&lt;K, V&gt;"/> class.
        /// </summary>
        /// <param name="k">The k.</param>
        /// <param name="v">The v.</param>
        public SerializableKeyValuePair(K k, V v)
        {
            Key = k;
            Value = v;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializableKeyValuePair&lt;K, V&gt;"/> class.
        /// </summary>
        /// <param name="gkvp">The GKVP.</param>
        public SerializableKeyValuePair(System.Collections.Generic.KeyValuePair<K, V> gkvp)
        {
            Key = gkvp.Key;
            Value = gkvp.Value;
        }

        internal System.Collections.Generic.KeyValuePair<K, V> CastToGenericsKVP()
        {
            return new System.Collections.Generic.KeyValuePair<K, V>(Key, Value);
        }
    }
}
