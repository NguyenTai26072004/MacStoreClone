$(document).ready(function () {
    // =======================================================
    // KHỞI TẠO
    // =======================================================
    // Khởi tạo Select2 cho dropdown Category
    $('#CategoryDropdown').select2({
        placeholder: "-- Chọn Danh mục --",
        allowClear: true
    });

    // Khởi tạo lại generateVariants ngay khi trang tải xong (cho sản phẩm đơn giản)
    generateVariants();

    // =======================================================
    // LOGIC UPLOAD ẢNH & PREVIEW
    // =======================================================
    let selectedFiles = new DataTransfer();

    $('#imageUpload').on('change', function (event) {
        for (let i = 0; i < event.target.files.length; i++) {
            selectedFiles.items.add(event.target.files[i]);
        }
        $('#imageUpload')[0].files = selectedFiles.files;
        updateImagePreview();
    });

    // Logic xóa ảnh (sửa lỗi xóa từng ảnh với DataTransfer)
    $('#imagePreviewContainer').on('click', '.remove-image-btn', function () {
        let indexToRemove = parseInt($(this).data('index'), 10);
        let newFiles = new DataTransfer();
        for (let i = 0; i < selectedFiles.files.length; i++) {
            if (i !== indexToRemove) {
                newFiles.items.add(selectedFiles.files[i]);
            }
        }
        selectedFiles = newFiles;
        $('#imageUpload')[0].files = selectedFiles.files;
        updateImagePreview();
    });

    function updateImagePreview() {
        // Luôn clear và vẽ lại toàn bộ ảnh xem trước mới
        $('#imagePreviewContainer').empty();
        $('#fileCount').text(selectedFiles.files.length > 0 ? `${selectedFiles.files.length} ảnh đã được chọn.` : 'Chưa có ảnh nào được chọn.');

        for (let i = 0; i < selectedFiles.files.length; i++) {
            let file = selectedFiles.files[i];
            let reader = new FileReader();
            reader.onload = function (e) {
                // Sử dụng data-index cho nút xóa để biết vị trí file trong DataTransfer
                let imagePreview = `
                    <div class="position-relative image-preview-item" style="width: 150px; height: 150px; margin-right: 0.75rem; margin-bottom: 0.75rem;">
                        <img src="${e.target.result}" class="img-thumbnail w-100 h-100" style="object-fit: cover;" />
                        <button type="button" class="position-absolute remove-image-btn"
                                style="top: -8px; right: -8px; border: none; background-color: rgba(40, 40, 40, 0.8); color: white; border-radius: 50%; width: 24px; height: 24px; display: flex; align-items: center; justify-content: center; line-height: 1; z-index: 10;"
                                data-index="${i}">
                            &times;
                        </button>
                    </div>`;
                $('#imagePreviewContainer').append(imagePreview);
            }
            reader.readAsDataURL(file);
        }
    }

    // =======================================================
    // LOGIC THÔNG SỐ KỸ THUẬT (STATIC)
    // =======================================================
    let specIndex = 0;
    $("#addSpecification").click(function () {
        var newSpecRow = `
            <div class="row mb-2 spec-row">
                <input type="hidden" name="Product.Specifications.Index" value="${specIndex}" />
                <div class="col-4"><input name="Product.Specifications[${specIndex}].Key" class="form-control" placeholder="Tên thông số (ví dụ: CPU)" /></div>
                <div class="col-7"><input name="Product.Specifications[${specIndex}].Value" class="form-control" placeholder="Giá trị (ví dụ: Apple M4)" /></div>
                <div class="col-1"><button type="button" class="btn btn-danger remove-spec">Xóa</button></div>
            </div>`;
        $("#specificationsContainer").append(newSpecRow);
        specIndex++;
    });

    $("#specificationsContainer").on("click", ".remove-spec", function () {
        $(this).closest(".spec-row").remove();
    });

    // =======================================================
    // LOGIC TẠO PHIÊN BẢN (CHO SẢN PHẨM CÓ HOẶC KHÔNG CÓ BIẾN THỂ)
    // =======================================================
    $('.variant-checkbox').on('change', generateVariants);

    function generateVariants() {
        // 1. Thu thập các giá trị đã chọn
        let selected = {};
        $('.variant-checkbox:checked').each(function () {
            let attrId = $(this).data('attribute-id');
            let attrName = $(this).data('attribute-name');
            let valueId = $(this).val();
            let valueName = $(this).data('value-name');

            if (!selected[attrId]) {
                selected[attrId] = { name: attrName, values: [] };
            }
            selected[attrId].values.push({ id: valueId, name: valueName });
        });

        let valueGroups = Object.values(selected).map(a => a.values);

        // 2. Nếu không có bất kỳ tùy chọn nào được chọn:
        if (valueGroups.length === 0) {
            $('#variantsContainer').empty();
            let singleVariantHtml = `
                <h5 class="mt-4">Giá & Tồn kho</h5>
                <div class="alert alert-info small">Đây là sản phẩm đơn giản không có tùy chọn. Nhập thông tin giá và tồn kho mặc định dưới đây.</div>
                <div class="row">
                    <!-- Quan trọng: name của input phải tuân theo cấu trúc Product.Variants[0].... -->
                    <input type="hidden" name="Product.Variants.Index" value="0" />
                    <div class="col-md-4 mb-3"><label class="form-label">Giá</label><input type="number" name="Product.Variants[0].Price" class="form-control" placeholder="Giá" required /></div>
                    <div class="col-md-4 mb-3"><label class="form-label">Mã SKU</label><input type="text" name="Product.Variants[0].Sku" class="form-control" placeholder="SKU" /></div>
                    <div class="col-md-4 mb-3"><label class="form-label">Số lượng</label><input type="number" name="Product.Variants[0].StockQuantity" class="form-control" placeholder="Số lượng" required /></div>
                </div>`;
            $('#variantsContainer').html(singleVariantHtml);
            return;
        }

        // 3. Nếu có tùy chọn: Tạo bảng các tổ hợp (Variants)
        let combinations = valueGroups.reduce((a, b) => a.flatMap(x => b.map(y => [...x, y])), [[]]);

        $('#variantsContainer').empty();
        let tableHtml = '<h5 class="mt-4">Bảng giá các phiên bản</h5><table class="table table-bordered table-striped"><thead><tr>';
        Object.values(selected).forEach(attr => { tableHtml += `<th>${attr.name}</th>`; });
        tableHtml += '<th>Giá</th><th>Mã SKU</th><th>Số lượng</th></tr></thead><tbody>';

        combinations.forEach((combo, comboIndex) => {
            tableHtml += '<tr>';
            combo.forEach((value, valueIndex) => {
                tableHtml += `<td>${value.name}</td>`;
                // Quan trọng: Thêm cả ID phiên bản và ID thuộc tính cho Model Binding ở phía server
                tableHtml += `<input type="hidden" name="Product.Variants[${comboIndex}].VariantValues[${valueIndex}].AttributeValueId" value="${value.id}" />`;
            });

            // Input ẩn để Model Binder biết vị trí trong danh sách
            tableHtml += `<input type="hidden" name="Product.Variants.Index" value="${comboIndex}" />`;

            // Các input cho Giá, SKU, Tồn kho
            tableHtml += `<td><input type="number" name="Product.Variants[${comboIndex}].Price" class="form-control" placeholder="Giá" required/></td>`;
            tableHtml += `<td><input type="text" name="Product.Variants[${comboIndex}].Sku" class="form-control" placeholder="SKU" /></td>`;
            tableHtml += `<td><input type="number" name="Product.Variants[${comboIndex}].StockQuantity" class="form-control" placeholder="Số lượng" required/></td>`;
            tableHtml += '</tr>';
        });

        tableHtml += '</tbody></table>';
        $('#variantsContainer').html(tableHtml);
    }
});