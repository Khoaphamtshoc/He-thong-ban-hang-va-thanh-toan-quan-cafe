// Một đoạn JavaScript nhỏ chỉ để đóng menu điện thoại khi điều hướng.
// Không can thiệp Blazor, dữ liệu đơn hàng hoặc quyền truy cập.
(() => {
  document.addEventListener('click', (event) => {
    if (event.target.closest('.sidebar .menu a')) {
      const menu = document.getElementById('nav-toggle');
      if (menu) menu.checked = false;
    }
  });
  document.addEventListener('keydown', (event) => {
    if (event.key === 'Escape') {
      const menu = document.getElementById('nav-toggle');
      if (menu) menu.checked = false;
    }
  });
})();
