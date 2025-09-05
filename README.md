# MacStore Clone - Dự án Website Thương mại Điện tử ASP.NET Core MVC

Chào mừng bạn đến với MacStore Clone! Đây là một dự án website thương mại điện tử hoàn chỉnh, mô phỏng theo mô hình của một cửa hàng bán lẻ các sản phẩm Apple. Dự án được xây dựng từ đầu bằng **ASP.NET Core 8 MVC**, thể hiện các luồng nghiệp vụ phức tạp từ quản lý sản phẩm có biến thể, giỏ hàng động, đến quy trình thanh toán tích hợp.

## 🚀 Live Demo
*   **Trang chủ:** `https://taitomshop.me` 
*   **Tài khoản Admin:** `admin@macstore.com`
*   **Mật khẩu:** `Admin@123`

---
## 🌟 Các tính năng nổi bật

Dự án được xây dựng với đầy đủ các chức năng của một trang E-commerce hiện đại, chia thành hai khu vực chính.

### 👤 Giao diện Người dùng (Client-Side)
-   **Giao diện Responsive:** Tối ưu hiển thị trên cả Desktop và Mobile.
-   **Hệ thống Danh mục Đa cấp:** Menu được render động từ CSDL, hỗ trợ danh mục cha-con.
-   **Trang Chi tiết Sản phẩm Thông minh:**
    -   **Logic chọn Phiên bản (Variant):** Người dùng có thể chọn các tùy chọn (Màu sắc, RAM, CPU). Giao diện sẽ **tự động cập nhật Giá và Tình trạng Kho hàng** theo thời gian thực mà không cần tải lại trang.
    -   Thư viện ảnh sản phẩm chuyên nghiệp, cho phép xem và phóng to.
-   **Bộ lọc Sản phẩm:** Cho phép lọc sản phẩm theo danh mục con và khoảng giá.
-   **Giỏ hàng Mini (Mini-Cart):**
    -   Sử dụng **AJAX** để Thêm/Sửa/Xóa sản phẩm khỏi giỏ hàng mà không làm gián đoạn trải nghiệm người dùng.
    -   Hiệu ứng Hover trên Desktop và Link trực tiếp trên Mobile.
-   **Luồng Thanh toán Hoàn chỉnh:**
    -   Hỗ trợ **Khách vãng lai (Guest Checkout)**, không bắt buộc đăng nhập.
    -   Form nhập địa chỉ thông minh với API Tỉnh/Huyện/Xã của Việt Nam.
    -   Tích hợp **Thanh toán khi nhận hàng (COD)** và **Cổng thanh toán MoMo (Sandbox)**.
-   **Xác thực & Tài khoản:** Đầy đủ chức năng Đăng ký, Đăng nhập (bao gồm cả **Google Authentication**), trang quản lý hồ sơ và xem Lịch sử đơn hàng.
-   **Tìm kiếm & Email:** Chức năng tìm kiếm sản phẩm và hệ thống gửi Email HTML tự động để cập nhật được các thay đổi của trạng thái đơn hàng.

### ⚙️ Khu vực Quản trị (Admin Area)
-   **Dashboard Thống kê:** Giao diện trực quan với các chỉ số kinh doanh quan trọng (doanh thu, đơn hàng mới...) và **biểu đồ doanh thu** theo tháng.
-   **Quản lý Sản phẩm Nâng cao:**
    -   Hệ thống CRUD mạnh mẽ với bảng dữ liệu **DataTables.js** (Sắp xếp, Tìm kiếm, Phân trang).
    -   **Hỗ trợ Sản phẩm có Biến thể:** Admin có thể tạo ra các phiên bản sản phẩm từ các thuộc tính đã định nghĩa, với Giá, SKU, và Tồn kho riêng cho từng phiên bản.
    -   Tích hợp trình soạn thảo văn bản **TinyMCE** cho phần mô tả sản phẩm.
    -   Giao diện upload nhiều ảnh trực quan với chức năng xem trước và xóa.
-   **Quản lý Đơn hàng:**
    -   Xem và lọc đơn hàng theo trạng thái.
    -   **Xử lý luồng vận hành:** Cập nhật trạng thái đơn hàng (Xác nhận ➔ Đang xử lý ➔ Đang giao ➔ Hoàn tất ➔ Hủy).
    -   Hỗ trợ **hoàn trả kho** khi hủy đơn và **gửi email tự động** cho khách hàng khi trạng thái thay đổi.
-   **Quản lý Người dùng & Phân quyền:**
    -   Xem danh sách, **Khóa/Mở khóa** tài khoản người dùng.
    -   **Phân quyền (Role Management):** Gán hoặc gỡ bỏ vai trò (Admin, Customer) cho từng tài khoản.
    -   Xem lịch sử mua hàng của từng người dùng.
-   **Quản lý Dữ liệu Gốc:** CRUD cho Danh mục, Thuộc tính và Giá trị thuộc tính.

---
## 🛠️ Công nghệ sử dụng
| Lĩnh vực      | Công nghệ                                                |
| ------------- | -------------------------------------------------------- |
| **Backend**     | C# 12, ASP.NET Core 8 MVC, Entity Framework Core 8       |
| **Database**    | Microsoft SQL Server (Code-First Approach)               |
| **Authentication**| ASP.NET Core Identity, Google Authentication             |
| **Frontend**    | HTML5, CSS3, JavaScript (ES6), Bootstrap 5, jQuery       |
| **UI Plugins**  | DataTables.js, Select2.js, Toastr.js, TinyMCE, Chart.js |
| **Kiến trúc**  | MVC, Dependency Injection |
| **API Tích hợp**  | MoMo Payment API (Sandbox), Gmail SMTP                  |

---
## 🚀 Cài đặt và Chạy dự án
Để chạy dự án này trên máy local của bạn, hãy làm theo các bước sau:

#### 1. Yêu cầu
-   .NET 8 SDK
-   Visual Studio 2022
-   SQL Server (bản LocalDB được cài sẵn cùng Visual Studio là đủ)

#### 2. Cài đặt
1.  **Clone a copy:**
    ```bash
    git clone https://github.com/NguyenTai26072004/MacStoreClone.git
    ```
2.  **Mở bằng Visual Studio:**
    Mở file `.sln` với Visual Studio 2022.

3.  **Cấu hình User Secrets:**
    -   Click chuột phải vào project `Ecommerce_WebApp` -> chọn **Manage User Secrets**.
    -   Một file `secrets.json` sẽ mở ra. Hãy thêm vào các key cần thiết:
        -   `ConnectionStrings:DefaultConnection`: Chuỗi kết nối đến SQL Server local của bạn.
        -   `Authentication:Google:ClientId` và `ClientSecret`: Lấy từ Google Cloud Console.
        -   `EmailSettings`: Cấu hình tài khoản Gmail và Mật khẩu Ứng dụng.
        -   `MomoSettings`: Các key API từ tài khoản MoMo for Business (sandbox).
    
4.  **Cập nhật Database:**
    -   Mở **Package Manager Console** (Tools -> NuGet Package Manager -> Package Manager Console).
    -   Chạy lệnh sau để tạo database và các bảng:
    ```powershell
    Update-Database
    ```
5.  **Chạy ứng dụng:**
    -   Nhấn `F5` hoặc nút ▶️ màu xanh để chạy dự án. Ứng dụng sẽ khởi động, `DbSeeder` sẽ tự động tạo Roles và tài khoản Admin mặc định.

Cảm ơn bạn đã xem dự án!


