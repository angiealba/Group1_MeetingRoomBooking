using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ASI.Basecode.WebApp.Models
{
    /// <summary>
    /// Login View Model
    /// </summary>
    public class LoginViewModel
    {
        /// <summary>ユーザーID</summary>
        [JsonPropertyName("userId")]
        [Required(ErrorMessage = "Username is required.")]
        public string userID { get; set; }
        /// <summary>パスワード</summary>
        [JsonPropertyName("password")]
        [Required(ErrorMessage = "Password is required.")]
        public string password { get; set; }
    }
}
