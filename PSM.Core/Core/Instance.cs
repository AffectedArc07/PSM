namespace PSM.Core.Core {
    /// <summary>
    /// Class that holds an instance
    /// </summary>
    public class Instance {
        // INSTANCE "META" VARS //

        /// <summary>
        /// Internal instance ID
        /// </summary>
        public int id { get; }

        /// <summary>
        /// Name of the instance
        /// </summary>
        public string name { get; }

        /// <summary>
        /// Path of the instance on disk
        /// </summary>
        public string instance_path { get; }

        /// <summary>
        /// Is this instance active
        /// </summary>
        public bool is_active { get; }

        // INSTANCE MANAGERS //
    }
}
