using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace Ecommerce_WebApp.ViewModels
{
    public class RoleManagementVM
    {
        // Thông tin người dùng đang được chỉnh sửa
        public ApplicationUser User { get; set; }

        // Danh sách tất cả các vai trò có thể có trong hệ thống
        public IEnumerable<SelectListItem> RoleList { get; set; }
    }
}