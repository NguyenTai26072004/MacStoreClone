using Microsoft.AspNetCore.Identity;

// Đảm bảo namespace này khớp với cấu trúc thư mục của bạn
namespace Ecommerce_WebApp.Services
{
    public class VietnameseIdentityErrorDescriber : IdentityErrorDescriber
    {
        // Ghi đè phương thức trả về lỗi mật khẩu không khớp
        public override IdentityError PasswordMismatch()
        {
            return new IdentityError
            {
                Code = nameof(PasswordMismatch),
                Description = "Mật khẩu và mật khẩu xác nhận không trùng khớp."
            };
        }

        // Ghi đè phương thức trả về lỗi mật khẩu quá ngắn
        public override IdentityError PasswordTooShort(int length)
        {
            return new IdentityError
            {
                Code = nameof(PasswordTooShort),
                Description = $"Mật khẩu phải có độ dài ít nhất {length} ký tự."
            };
        }

        // Ghi đè phương thức trả về lỗi yêu cầu ký tự đặc biệt
        public override IdentityError PasswordRequiresNonAlphanumeric()
        {
            return new IdentityError
            {
                Code = nameof(PasswordRequiresNonAlphanumeric),
                Description = "Mật khẩu phải có ít nhất một ký tự không phải là chữ và số (ký tự đặc biệt)."
            };
        }

        // Ghi đè phương thức trả về lỗi yêu cầu chữ số
        public override IdentityError PasswordRequiresDigit()
        {
            return new IdentityError
            {
                Code = nameof(PasswordRequiresDigit),
                Description = "Mật khẩu phải có ít nhất một chữ số ('0'-'9')."
            };
        }

        // Ghi đè phương thức trả về lỗi yêu cầu chữ thường
        public override IdentityError PasswordRequiresLower()
        {
            return new IdentityError
            {
                Code = nameof(PasswordRequiresLower),
                Description = "Mật khẩu phải có ít nhất một chữ thường ('a'-'z')."
            };
        }

        // Ghi đè phương thức trả về lỗi yêu cầu chữ hoa
        public override IdentityError PasswordRequiresUpper()
        {
            return new IdentityError
            {
                Code = nameof(PasswordRequiresUpper),
                Description = "Mật khẩu phải có ít nhất một chữ hoa ('A'-'Z')."
            };
        }

        // Ghi đè phương thức trả về lỗi tên người dùng đã tồn tại
        public override IdentityError DuplicateUserName(string userName)
        {
            return new IdentityError
            {
                Code = nameof(DuplicateUserName),
                Description = $"Tên người dùng '{userName}' đã tồn tại. Vui lòng chọn một tên khác."
            };
        }

        // Ghi đè phương thức trả về lỗi email đã tồn tại
        public override IdentityError DuplicateEmail(string email)
        {
            return new IdentityError
            {
                Code = nameof(DuplicateEmail),
                Description = $"Email '{email}' đã được sử dụng."
            };
        }

        // Bạn có thể tiếp tục override các phương thức khác ở đây...
        // Để xem danh sách đầy đủ, hãy chuột phải vào "IdentityErrorDescriber" và chọn "Go to Definition"
    }
}