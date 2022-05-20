using Microsoft.AspNetCore.Mvc;

namespace PSM.Core.Models {
    public class TokenResponseModel {
		/// <summary>
		/// The value of the JWT.
		/// </summary>
		public string? Token { get; set; }

		/// <summary>
		/// When the token expires.
		/// </summary>
		public DateTimeOffset ExpiresAt { get; set; }
	}
}
