// Đặt tất cả code vào trong $(document).ready() để đảm bảo các phần tử HTML đã tồn tại.
$(document).ready(function () {

    // =======================================================
    // == LOGIC HIỂN THỊ GIỎ HÀNG MINI KHI HOVER
    // =======================================================
    let hideCartTimeout;

    // Sử dụng document.on() để sự kiện hover vẫn hoạt động sau khi giỏ hàng được làm mới
    $(document).on('mouseenter', '.mini-cart-container', function () {
        clearTimeout(hideCartTimeout);
        $(this).find('.mini-cart-dropdown').stop(true, true).fadeIn(200);
    });

    $(document).on('mouseleave', '.mini-cart-container', function () {
        hideCartTimeout = setTimeout(() => {
            $(this).find('.mini-cart-dropdown').stop(true, true).fadeOut(200);
        }, 300);
    });


    // =======================================================
    // == LOGIC CẬP NHẬT SỐ LƯỢNG TRONG GIỎ HÀNG
    // =======================================================
    $(document).on('change', '.update-cart-qty', function () {
        const inputElement = $(this);
        const variantId = inputElement.data('id');
        const newQuantity = parseInt(inputElement.val());
        const oldValue = inputElement.data('old-value') || newQuantity;

        inputElement.prop('disabled', true);

        $.ajax({
            url: '/Cart/UpdateQuantity',
            type: 'POST',
            data: { variantId: variantId, quantity: newQuantity },
            success: function (response) {
                if (response.success) {
                    $('#subtotal-' + variantId).text(response.newSubTotal + 'đ');
                    updateCartTotalsInHeader(response.cartItemCount, response.newTotal + 'đ');
                } else {
                    toastr.error(response.message);
                    inputElement.val(oldValue);
                }
            },
            error: function () {
                toastr.error("Lỗi kết nối.");
                inputElement.val(oldValue);

            },
            complete: function () {
                inputElement.prop('disabled', false);
            }
        });
    });
    // Lưu lại giá trị cũ khi focus
    $(document).on('focus', '.update-cart-qty', function () {
        $(this).data('old-value', $(this).val());
    });


    // =======================================================
    // == LOGIC XÓA SẢN PHẨM KHỎI GIỎ HÀNG
    // =======================================================
    $(document).on('click', '.remove-from-cart', function () {
        const button = $(this);
        const variantId = button.data('id');
        button.prop('disabled', true);

        $.ajax({
            url: '/Cart/RemoveFromCart',
            type: 'POST',
            data: { variantId: variantId },
            success: function (response) {
                if (response.success) {
                    $('#item-' + variantId).fadeOut(300, function () {
                        $(this).remove();
                        // Chỉ gọi update nếu giỏ hàng trống
                        if (response.cartItemCount == 0) {
                            updateFullCartView();
                        }
                    });
                    updateCartTotalsInHeader(response.cartItemCount, response.newTotal + 'đ');
                    toastr.success("Đã xóa sản phẩm.");
                } else {
                    toastr.error("Lỗi khi xóa sản phẩm.");
                    button.prop('disabled', false);
                }
            },
            error: function () {
                button.prop('disabled', false);
                toastr.error("Lỗi kết nối.");
            }
        });
    });
});


// === CÁC HÀM HELPER  ===
function updateFullCartView() {
    $.ajax({
        url: '/Cart/RenderCartDropdown',
        type: 'GET',
        success: function (cartHtmlResult) {
            $('#shoppingCartContainer').html(cartHtmlResult);
        }
    });
}

function updateCartTotalsInHeader(itemCount, totalAmount) {
    const cartBadge = $('#cart-count');
    // Nếu không tìm thấy cart-badge, tạo mới nó (cho lần đầu thêm vào giỏ rỗng)
    if (cartBadge.length === 0 && itemCount > 0) {
        // Tìm icon giỏ hàng và thêm thẻ span vào sau nó.
        $('#cartIconLink i').after(`<span id="cart-count" class="position-absolute top-0 start-100 translate-middle badge rounded-pill bg-danger">${itemCount}</span>`);
    } else {
        cartBadge.text(itemCount);
    }

    // Ẩn/hiện con số
    if (itemCount > 0) {
        $('#cart-count').removeClass('d-none');
    } else {
        $('#cart-count').addClass('d-none');
    }

    // Cập nhật tổng tiền
    $('#mini-cart-total').text(totalAmount);
}