$(document).ready(function () {
    // === XỬ LÝ NÚT TĂNG/GIẢM SỐ LƯỢNG ===
    $('.btn-increase-qty, .btn-decrease-qty').on('click', function () {
        const button = $(this);
        const input = button.siblings('.update-cart-qty-page');
        let currentQty = parseInt(input.val());

        if (button.hasClass('btn-increase-qty')) {
            currentQty++;
        } else {
            if (currentQty > 1) {
                currentQty--;
            } else {
                return; // Không cho giảm dưới 1
            }
        }
        input.val(currentQty);
        updateQuantity(input, currentQty); // Gọi hàm ajax
    });

    // === XỬ LÝ XÓA SẢN PHẨM ===
    $('.remove-from-cart-page').on('click', function () {
        const button = $(this);
        const variantId = button.data('id');

        button.prop('disabled', true);

        $.ajax({
            url: '/Cart/RemoveFromCart',
            type: 'POST',
            data: { variantId: variantId },
            success: function (response) {
                if (response.success) {
                    $('#cart-item-' + variantId).fadeOut(300, function () {
                        $(this).remove();
                        // Kiểm tra lại nếu giỏ hàng rỗng thì tải lại trang để hiển thị đúng giao diện
                        if ($('.cart-item-row').length === 0) {
                            window.location.reload();
                        }
                    });
                    // Cập nhật lại số tổng và giỏ hàng mini
                    updateCartTotals(response);
                    toastr.success("Đã xóa sản phẩm.");
                } else {
                    button.prop('disabled', false);
                }
            },
            error: function () { toastr.error("Lỗi kết nối."); button.prop('disabled', false); }
        });
    });

    // Hàm gọi AJAX để cập nhật số lượng
    function updateQuantity(inputElement, quantity) {
        const variantId = inputElement.data('id');

        inputElement.prop('disabled', true); // Vô hiệu hóa trong khi xử lý

        $.ajax({
            url: '/Cart/UpdateQuantity',
            type: 'POST',
            data: { variantId: variantId, quantity: quantity },
            success: function (response) {
                if (response.success) {
                    // Cập nhật các con số trên trang
                    $('#subtotal-page-' + variantId).text(response.newSubTotal + '₫');
                    $('#cart-subtotal-text').text(response.newTotal + '₫');
                    $('#cart-total-text').text(response.newTotal + '₫');
                    // Cập nhật giỏ hàng mini trên header
                    $('#cart-count').text(response.cartItemCount);
                } else {
                    toastr.error(response.message);
                    inputElement.val(quantity - 1); // Trả lại giá trị cũ (hoặc lấy từ data-old-value)
                }
            },
            error: function () {
                toastr.error("Lỗi kết nối.");
                inputElement.val(quantity - 1);
            },
            complete: function () {
                inputElement.prop('disabled', false); // Kích hoạt lại
            }
        });
    }
});