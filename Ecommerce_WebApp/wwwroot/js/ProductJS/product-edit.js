$(document).ready(function () {
    // --- KHỞI TẠO ---
    $('#CategoryDropdown').select2({ placeholder: "-- Chọn Danh mục --", allowClear: true });

    // =======================================================
    // == LOGIC UPLOAD ẢNH MỚI & XÓA ẢNH CŨ
    // =======================================================
    let selectedFiles = new DataTransfer();

    // Xử lý khi người dùng chọn thêm ảnh mới
    $('#imageUpload').on('change', function (event) {
        for (let i = 0; i < event.target.files.length; i++) {
            selectedFiles.items.add(event.target.files[i]);
        }
        this.files = selectedFiles.files;
        updateNewImagePreview(); // Chỉ vẽ lại phần ảnh mới
    });

    // Xử lý xóa ảnh MỚI (chưa lưu) khỏi danh sách xem trước
    $('#imagePreviewContainer').on('click', '.remove-new-image-btn', function () {
        let indexToRemove = parseInt($(this).data('index'), 10);
        let newFiles = new DataTransfer();
        for (let i = 0; i < selectedFiles.files.length; i++) {
            if (i !== indexToRemove) {
                newFiles.items.add(selectedFiles.files[i]);
            }
        }
        selectedFiles = newFiles;
        $('#imageUpload')[0].files = selectedFiles.files;
        updateNewImagePreview();
    });

    // Xử lý "đánh dấu xóa" cho ảnh CŨ (đã có)
    $('#imagePreviewContainer').on('click', '.remove-existing-image-btn', function () {
        var button = $(this);
        var imageItem = button.closest('.existing-image-item');
        var hiddenInput = imageItem.find('input[name="imagesToDelete"]');

        if (hiddenInput.is(':disabled')) {
            imageItem.find('img').css('opacity', '0.5');
            hiddenInput.prop('disabled', false);
            button.text('Hoàn tác');
            button.removeClass('btn-danger').addClass('btn-warning');
        } else {
            imageItem.find('img').css('opacity', '1');
            hiddenInput.prop('disabled', true);
            button.html('&times;'); // Trả về dấu X
            button.removeClass('btn-warning').addClass('btn-danger');
        }
    });

    function updateNewImagePreview() {
        // Chỉ xóa và vẽ lại các ảnh MỚI xem trước
        $('#imagePreviewContainer .new-image-preview-item').remove();

        $('#fileCount').text(selectedFiles.files.length > 0 ? `${selectedFiles.files.length} ảnh mới đã được chọn.` : 'Chưa có ảnh nào được chọn.');

        for (let i = 0; i < selectedFiles.files.length; i++) {
            let file = selectedFiles.files[i];
            let reader = new FileReader();
            reader.onload = function (e) {
                // Thêm class 'new-image-preview-item' để phân biệt với ảnh cũ
                let imagePreview = `
                    <div class="position-relative new-image-preview-item" style="width: 150px; height: 150px; margin-right: 0.75rem; margin-bottom: 0.75rem;">
                        <img src="${e.target.result}" class="img-thumbnail w-100 h-100" style="object-fit: cover;" />
                        <button type="button" class="position-absolute remove-new-image-btn" style="top: -8px; right: -8px; border: none; background-color: rgba(40, 40, 40, 0.8); color: white; border-radius: 50%; width: 24px; height: 24px; display: flex; align-items: center; justify-content: center; line-height: 1; z-index: 10;" data-index="${i}">&times;</button>
                    </div>`;
                $('#imagePreviewContainer').append(imagePreview);
            }
            reader.readAsDataURL(file);
        }
    }

    // =======================================================
    // == LOGIC THÊM/XÓA THÔNG SỐ KỸ THUẬT
    // =======================================================
    // Bắt đầu chỉ số từ số lượng dòng đã có sẵn trên trang
    let specIndex = $('#specificationsContainer .spec-row').length;

    $("#addSpecification").click(function () {
        var newSpecRow = `
            <div class="row mb-2 spec-row">
                <input type="hidden" name="Product.Specifications.Index" value="${specIndex}" />
                <div class="col-4"><input name="Product.Specifications[${specIndex}].Key" class="form-control" placeholder="Tên thông số" /></div>
                <div class="col-7"><input name="Product.Specifications[${specIndex}].Value" class="form-control" placeholder="Giá trị" /></div>
                <div class="col-1"><button type="button" class="btn btn-danger remove-spec">Xóa</button></div>
            </div>`;
        $("#specificationsContainer").append(newSpecRow);
        specIndex++;
    });

    // Logic xóa vẫn giữ nguyên
    $("#specificationsContainer").on("click", ".remove-spec", function () {
        $(this).closest(".spec-row").remove();
    });
});