// Các biến productVariants và defaultPrice được truyền từ View Details.cshtml

$(document).ready(function () {

    // === 1. KHAI BÁO BIẾN TRẠNG THÁI ===
    let currentSelection = {};
    const totalOptionGroups = $('#variantOptions > .border').length;

    // === 2. GẮN KẾT SỰ KIỆN ===

    // Gắn sự kiện cho các nút tùy chọn
    $('.option-btn').on('click', function () {
        const clickedButton = $(this);
        const attributeId = clickedButton.data('attribute-id').toString();
        const valueId = clickedButton.data('value-id').toString();

        // Cập nhật giao diện: Bỏ active của các nút anh em, active nút được click
        clickedButton.closest('.d-flex').find('.option-btn').removeClass('active');
        clickedButton.addClass('active');

        // Cập nhật lựa chọn hiện tại của người dùng
        currentSelection[attributeId] = valueId;

        // Tìm và hiển thị phiên bản phù hợp
        findMatchingVariant();
    });

    // === GẮN SỰ KIỆN "THÊM VÀO GIỎ" BẰNG AJAX ===
    $('#addToCartButton').on('click', function () {
        const variantId = $(this).data('variant-id');
        const quantity = $('#quantity').val();
        const button = $(this);

        if (variantId && quantity > 0) {
            button.prop('disabled', true).find('span').text('Đang thêm...');
            button.find('i').removeClass('fa-cart-shopping').addClass('fa-spinner fa-spin');

            $.ajax({
                url: '/Cart/AddToCart',
                type: 'POST',
                data: {
                    variantId: parseInt(variantId),
                    quantity: parseInt(quantity)
                },
                success: function (response) {
                    if (response.success) {
                        toastr.success("Đã thêm sản phẩm vào giỏ hàng!");
                        updateCartView();
                    } else {
                        toastr.error(response.message || "Không thể thêm sản phẩm.");
                    }
                },
                error: function () {
                    toastr.error("Có lỗi xảy ra, vui lòng thử lại.");
                },
                complete: function () {
                    button.prop('disabled', false).find('span').text('Thêm vào giỏ hàng');
                    button.find('i').removeClass('fa-spinner fa-spin').addClass('fa-cart-shopping');
                }
            });
        }
    });

    function updateCartView() {
        $.ajax({
            url: '/Cart/RenderCartDropdown', // Action chỉ để lấy HTML
            type: 'GET',
            success: function (cartHtmlResult) {
                $('#shoppingCartContainer').html(cartHtmlResult);
            }
        });
    }


    // === 3. HÀM LOGIC CỐT LÕI ===

    function findMatchingVariant() {
        if (Object.keys(currentSelection).length !== totalOptionGroups) {
            disableAddToCart("Vui lòng chọn đủ các tùy chọn");
            resetPriceAndStock();
            return;
        }

        let selectedValueIds = Object.values(currentSelection).map(Number).sort();
        let foundVariant = productVariants.find(variant =>
            variant.attributeValueIds.length === selectedValueIds.length &&
            variant.attributeValueIds.every((value, index) => value === selectedValueIds[index])
        );
        updateDisplay(foundVariant);
    }

    // === 4. KHỞI TẠO BAN ĐẦU ===

    // Nếu là sản phẩm đơn giản, kích hoạt luôn nút mua
    if (productVariants.length === 1 && totalOptionGroups === 0) {
        updateDisplay(productVariants[0]);
    } else {
        disableAddToCart("Vui lòng chọn các tùy chọn");
    }

});

// === CÁC HÀM PHỤ ĐỂ CẬP NHẬT GIAO DIỆN ===
// (Đặt bên ngoài document.ready để cho gọn, nhưng vẫn hoạt động)

function updateDisplay(variant) {
    if (variant) {
        $('#productPrice').text(formatPrice(variant.price) + ' VNĐ');
        updateStockStatus(variant.stock);
        enableAddToCart(variant.id);
    } else {
        resetPriceAndStock();
        updateStockStatus(null);
        disableAddToCart("Tổ hợp này không có sẵn");
    }
}

function updateStockStatus(stockQuantity) {
    if ($('#stockStatus').length === 0 && stockQuantity !== null) {
        // ID `root_stock` không có trong HTML, sửa thành chèn sau giá
        $('<p id="stockStatus" class="small mt-2"></p>').insertAfter('#root_stock');
    }
    if (stockQuantity !== null && stockQuantity > 0) {
        $('#stockStatus').text(`Còn hàng: ${stockQuantity} sản phẩm`).removeClass('text-danger').addClass('text-success');
    } else if (stockQuantity !== null && stockQuantity <= 0) {
        $('#stockStatus').text('Hết hàng').removeClass('text-success').addClass('text-danger');
    } else {
        $('#stockStatus').remove();
    }
}

function enableAddToCart(variantId) {
    const btn = $('#addToCartButton');
    btn.prop('disabled', false);
    btn.find('#addToCartButtonText').text('Thêm vào giỏ hàng');
    btn.data('variant-id', variantId);
}

function disableAddToCart(message) {
    const btn = $('#addToCartButton');
    btn.prop('disabled', true);
    btn.find('#addToCartButtonText').text(message);
    btn.removeData('variant-id');
}

function formatPrice(price) {
    return price.toLocaleString('vi-VN');
}

function resetPriceAndStock() {
    if (typeof defaultPrice !== 'undefined') {
        $('#productPrice').text(formatPrice(defaultPrice) + ' VNĐ');
    }
}