using System.Security.Cryptography;

namespace PSM.Core.Core {
    public static class Constants {
        public const string DEFAULT_INSTANCE_DIR = "C:/PSM/instances/";
        public const string SYSTEM_USER_NAME = "SYSTEM";
        public const string ADMIN_USER_NAME = "ADMIN";
        public const string DEFAULT_ADMIN_PASS = "ChangeMeYouMuppet";

        private static byte[]? JWT_BYTES;

        public static byte[] GetJWTBytes() {
            if (JWT_BYTES == null) {
                JWT_BYTES = RandomNumberGenerator.GetBytes(256);
            }

            return JWT_BYTES;
        }
    }
}
