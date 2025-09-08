function toggleMenu() {
    const menu = document.getElementById('mobileMenu');
    menu.classList.toggle('d-none');
}

function toggleSearchOverlay() {
    const overlay = document.getElementById("searchOverlay");
    overlay.classList.toggle("d-none");

    if (!overlay.classList.contains("d-none")) {
        overlay.querySelector("input").focus();
    }
}

// Ẩn overlay khi click ra ngoài
document.addEventListener('click', function (e) {
    const overlay = document.getElementById("searchOverlay");
    const searchIcon = document.querySelectorAll('.fa-magnifying-glass');

    // Nếu đang mở và click ngoài input/search icon thì ẩn
    if (!overlay.classList.contains("d-none") &&
        !overlay.contains(e.target) &&
        ![...searchIcon].some(icon => icon.contains(e.target))) {
        overlay.classList.add("d-none");
    }
});

// File: wwwroot/js/layout_user.js

document.addEventListener("DOMContentLoaded", function () {

    const goToTopBtn = document.getElementById("goToTopBtn");

    if (goToTopBtn) {
        // ==========================================================
        // PHẦN 1: LOGIC ẨN/HIỆN NÚT (Phần này vẫn giữ nguyên)
        // ==========================================================
        window.addEventListener("scroll", function () {
            if (window.scrollY > 300) { // Bạn có thể đổi ngưỡng 300px
                goToTopBtn.classList.add("show");
            } else {
                goToTopBtn.classList.remove("show");
            }
        });

        // ==========================================================
        // PHẦN 2: LOGIC CUỘN MƯỢT KHI CLICK (MÃ MỚI HOÀN TOÀN)
        // ==========================================================
        goToTopBtn.addEventListener("click", function () {

            const duration = 700; // Thời gian cuộn (miligiây), bạn có thể thay đổi
            const start = window.scrollY;
            const distance = -start;
            let startTime = null;

            function animation(currentTime) {
                if (startTime === null) startTime = currentTime;
                const timeElapsed = currentTime - startTime;

                // Công thức easing (làm cho hiệu ứng mượt hơn ở đầu và cuối)
                const run = ease(timeElapsed, start, distance, duration);
                window.scrollTo(0, run);

                if (timeElapsed < duration) requestAnimationFrame(animation);
            }

            // Hàm tính toán easing
            function ease(t, b, c, d) {
                t /= d / 2;
                if (t < 1) return c / 2 * t * t + b;
                t--;
                return -c / 2 * (t * (t - 2) - 1) + b;
            }

            requestAnimationFrame(animation);
        });
    }
});


