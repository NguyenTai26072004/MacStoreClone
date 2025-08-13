$(document).ready(function () {
    // ... (Phần khởi tạo Select2 và logic thông số kỹ thuật không đổi) ...
    $('#CategoryDropdown').select2({
        placeholder: "-- Chọn Danh mục --",
        allowClear: true
    });

    // =======================================================
    // LOGIC UPLOAD ẢNH (ĐÃ SỬA LỖI)
    // =======================================================
    let selectedFiles = new DataTransfer();

    $('#imageUpload').on('change', function (event) {
        for (let i = 0; i < event.target.files.length; i++) {
            selectedFiles.items.add(event.target.files[i]);
        }
        $('#imageUpload')[0].files = selectedFiles.files;
        updateImagePreview();
    });

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
        $('#imagePreviewContainer').empty();
        $('#fileCount').text(selectedFiles.files.length > 0 ? `${selectedFiles.files.length} ảnh đã được chọn.` : 'Chưa có ảnh nào được chọn.');

        for (let i = 0; i < selectedFiles.files.length; i++) {
            let file = selectedFiles.files[i];
            let reader = new FileReader();
            reader.onload = function (e) {
                let imagePreview = `
                            <div class="position-relative image-preview-item" style="width: 150px; height: 150px; margin-right: 0.75rem; margin-bottom: 0.75rem;">
                                <img src="${e.target.result}" class="img-thumbnail w-100 h-100" style="object-fit: cover;" />
                                <button type="button" class="position-absolute remove-image-btn"
                                        style="top: -8px; right: -8px; border: none; background-color: rgba(40, 40, 40, 0.8); color: white; border-radius: 50%; width: 24px; height: 24px; display: flex; align-items: center; justify-content: center; line-height: 1; z-index: 10;"
                                        data-index="${i}">
                                    &times;
                                </button>
                            </div>`;

                // === DÒNG BỊ THIẾU ĐÃ ĐƯỢC THÊM LẠI TẠI ĐÂY ===
                $('#imagePreviewContainer').append(imagePreview);
                // ===============================================
            }
            reader.readAsDataURL(file);
        }
    }

    // ... (Phần logic thông số kỹ thuật không đổi) ...
    let specIndex = 0;
    $("#addSpecification").click(function () {
        var newSpecRow = `
                    <div class="row mb-2 spec-row">
                        <div class="col-4">
                            <input name="Product.Specifications[${specIndex}].Key" class="form-control" placeholder="Tên thông số (ví dụ: CPU)" />
                        </div>
                        <div class="col-7">
                            <input name="Product.Specifications[${specIndex}].Value" class="form-control" placeholder="Giá trị (ví dụ: Apple M4)" />
                        </div>
                        <div class="col-1">
                            <button type="button" class="btn btn-danger remove-spec">Xóa</button>
                        </div>
                    </div>`;
        $("#specificationsContainer").append(newSpecRow);
        specIndex++;
    });
    $("#specificationsContainer").on("click", ".remove-spec", function () {
        $(this).closest(".spec-row").remove();
    });


    // == LOGIC TẠO PHIÊN BẢN ĐỘNG
    // =======================================================
    // Gắn sự kiện 'change' vào tất cả các checkbox có class 'variant-checkbox'
    $('.variant-checkbox').on('change', generateVariants);

    function generateVariants() {
        // Bước 1: Thu thập tất cả các giá trị đã được chọn
        let selected = {}; // Tạo một đối tượng rỗng để lưu các lựa chọn
        $('.variant-checkbox:checked').each(function () {
            let attrId = $(this).data('attribute-id');
            let attrName = $(this).data('attribute-name');
            let valueId = $(this).val();
            let valueName = $(this).data('value-name');

            // Nếu chưa có thuộc tính này trong đối tượng selected, hãy tạo mới
            if (!selected[attrId]) {
                selected[attrId] = { name: attrName, values: [] };
            }
            // Thêm giá trị đã chọn vào mảng của thuộc tính tương ứng
            selected[attrId].values.push({ id: valueId, name: valueName });
        });

        // Bước 2: Tạo ra tất cả các tổ hợp có thể có
        let valueGroups = Object.values(selected).map(a => a.values);
        if (valueGroups.length === 0) {
            $('#variantsContainer').empty(); // Nếu không chọn gì, xóa bảng
            return;
        }

        // Thuật toán tính Tích Descartes để tạo tổ hợp
        let combinations = valueGroups.reduce((a, b) => a.flatMap(x => b.map(y => [...x, y])), [[]]);

        // Bước 3: Vẽ lại bảng các phiên bản
        $('#variantsContainer').empty();
        if (combinations[0].length === 0) return;

        // Tạo phần đầu của bảng và các cột tiêu đề
        let tableHtml = '<table class="table table-bordered table-striped"><thead><tr>';
        Object.values(selected).forEach(attr => { tableHtml += `<th>${attr.name}</th>`; });
        tableHtml += '<th>Giá</th><th>Mã SKU</th><th>Số lượng</th></tr></thead><tbody>';

        // Tạo các dòng cho mỗi tổ hợp
        combinations.forEach((combo, comboIndex) => {
            tableHtml += '<tr>';
            // Tạo các ô cho tên thuộc tính và các ô ẩn chứa ID
            combo.forEach((value, valueIndex) => {
                tableHtml += `<td>${value.name}</td>`;
                tableHtml += `<input type="hidden" name="Product.Variants[${comboIndex}].VariantValues[${valueIndex}].AttributeValueId" value="${value.id}" />`;
            });

            // Tạo các ô nhập liệu cho Giá, SKU, Số lượng
            tableHtml += `<td><input type="number" name="Product.Variants[${comboIndex}].Price" class="form-control" placeholder="Giá" required/></td>`;
            tableHtml += `<td><input type="text" name="Product.Variants[${comboIndex}].Sku" class="form-control" placeholder="SKU" /></td>`;
            tableHtml += `<td><input type="number" name="Product.Variants[${comboIndex}].StockQuantity" class="form-control" placeholder="Số lượng" required/></td>`;
            tableHtml += '</tr>';
        });

        tableHtml += '</tbody></table>';
        // Chèn toàn bộ HTML của bảng vào container
        $('#variantsContainer').html(tableHtml);
    }
});