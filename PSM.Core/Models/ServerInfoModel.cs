using Microsoft.AspNetCore.Mvc;

namespace PSM.Core.Models {
    public class ServerInfoModel {
        public string? current_username { get; set; }
        public string? server_version { get; set; }
    }
}
