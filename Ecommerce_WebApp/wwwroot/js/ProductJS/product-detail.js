$(document).ready(function () {

    // === 1. KHAI BÁO BIẾN ===
    let currentSelection = {};
    const totalOptionGroups = $('#variantOptions > .border').length;

    // === 2. HÀM CẬP NHẬT GIAO DIỆN ===
    function updateDisplay(variant) {
        if (variant) {
            // Cập nhật giá và tồn kho
            $('#productPrice').text(formatPrice(variant.price) + ' VNĐ');
            updateStockStatus(variant.stock);
            // Kích hoạt nút mua hàng
            enableAddToCart(variant.id);
        } else {
            // Nếu không tìm thấy phiên bản
            resetPriceAndStock();
            updateStockStatus(null); // Ẩn thông tin kho
            disableAddToCart("Tổ hợp này không có sẵn");
        }
    }


    function updateStockStatus(stockQuantity) {
        // Tạo thẻ p để hiển thị nếu chưa có
        if ($('#stockStatus').length === 0 && stockQuantity !== null) {
            $('<p id="stockStatus" class="small mt-2"></p>').insertAfter('#root_stock');
        }

        if (stockQuantity !== null && stockQuantity > 0) {
            $('#stockStatus').text(`Còn hàng: ${stockQuantity} sản phẩm`).removeClass('text-danger').addClass('text-success');
        } else if (stockQuantity !== null && stockQuantity <= 0) {
            $('#stockStatus').text('Hết hàng').removeClass('text-success').addClass('text-danger');
        } else {
            $('#stockStatus').remove(); // Ẩn đi nếu không có thông tin
        }
    }

    // === 3. HÀM LOGIC CỐT LÕI ===
    function findMatchingVariant() {
        // Chỉ tìm khi người dùng đã chọn đủ tùy chọn
        if (Object.keys(currentSelection).length !== totalOptionGroups) {
            disableAddToCart("Vui lòng chọn đủ các tùy chọn");
            resetPriceAndStock();
            updateStockStatus(null);
            return;
        }

        // Sắp xếp các ID giá trị đã chọn để so sánh
        let selectedValueIds = Object.values(currentSelection).map(Number).sort();

        // Tìm phiên bản có bộ ID trùng khớp
        let foundVariant = productVariants.find(variant =>
            variant.attributeValueIds.length === selectedValueIds.length &&
            variant.attributeValueIds.every((value, index) => value === selectedValueIds[index])
        );

        updateDisplay(foundVariant);
    }

    $('.option-btn').on('click', function () {
        const clickedButton = $(this);
        const attributeId = clickedButton.data('attribute-id').toString();
        const valueId = clickedButton.data('value-id').toString();

        // Bỏ active của TẤT CẢ các nút anh em
        clickedButton.closest('.d-flex').find('.option-btn').removeClass('active');

        clickedButton.addClass('active');

        // Cập nhật lựa chọn
        currentSelection[attributeId] = valueId;

        // Tìm phiên bản
        findMatchingVariant();
    });

    // === 5. KHỞI TẠO BAN ĐẦU ===
    // Nếu là sản phẩm đơn giản (chỉ có 1 phiên bản) -> Kích hoạt luôn
    if (productVariants.length === 1 && totalOptionGroups === 0) {
        updateDisplay(productVariants[0]);
    } else {
        disableAddToCart("Vui lòng chọn các tùy chọn");
    }

});


function enableAddToCart(variantId) { const btn = $('#addToCartButton'); btn.prop('disabled', false); btn.find('#addToCartButtonText').text('Thêm vào giỏ hàng'); btn.data('variant-id', variantId); }
function disableAddToCart(message) { const btn = $('#addToCartButton'); btn.prop('disabled', true); btn.find('#addToCartButtonText').text(message); btn.removeData('variant-id'); }
function formatPrice(price) { return price.toLocaleString('vi-VN'); }
function resetPriceAndStock() { $('#productPrice').text(formatPrice(defaultPrice) + ' VNĐ'); }