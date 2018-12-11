using System.Collections.Generic;

namespace EcWamp
{
    /// <summary>
    /// A dictionary with an indexer that produces an informative
    /// KeyNotFoundException message.
    /// </summary>

    public class DiagnosticDictionary<TKey, TValue> : Dictionary<TKey, TValue>
    {
        protected object tag;
        protected string name = "unknown";

        /// <summary>
        /// Gets/sets an object that you can associate with the dictionary.
        /// </summary>
        /// De
        public object Tag
        {
            get { return tag; }
            set { tag = value; }
        }

        /// <summary>
        /// The dictionary name. The default is "unknown".
        /// Used to enhance the KeyNotFoundException.
        /// </summary>

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        /// <summary>
        /// Parameterless constructor.
        /// </summary>
        public DiagnosticDictionary()
        {
        }

        public DiagnosticDictionary(IEqualityComparer<TKey> comp) : base(comp)
        {
        }

        /// <summary>
        /// Constructor that takes a name.
        /// </summary>
        public DiagnosticDictionary(string name)
        {
            this.name = name;
        }

        /// <summary>
        /// Constructor that takes a name.
        /// </summary>
        public DiagnosticDictionary(string name, IEqualityComparer<TKey> comp) : base(comp)
        {
            this.name = name;
        }

        /// <summary>
        /// Indexer that produces a more useful KeyNotFoundException.
        /// </summary>
        public new TValue this[TKey key]
        {
            get
            {
                try
                {
                    return base[key];
                }
                catch (KeyNotFoundException)
                {
                    throw new KeyNotFoundException("The key '" + key.ToString() +
                       "' was not found in the dictionary '" + name + "'. in method: " + System.Reflection.MethodBase.GetCurrentMethod().Name);
                }
            }

            set { base[key] = value; }
        }

        public void Add(Dictionary<TKey, TValue> indict)
        {
            foreach (KeyValuePair<TKey, TValue> kvp in indict)
            {
                Add(kvp.Key, kvp.Value);
            }
        }
    }
}