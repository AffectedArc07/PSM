namespace PSM.Core.Models {
    public class User {
        public int Id { get; set;  } 
        public bool Enabled { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public User? CreatedBy { get; set; }
    }
}
