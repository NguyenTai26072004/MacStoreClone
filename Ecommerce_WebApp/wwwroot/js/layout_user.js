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


