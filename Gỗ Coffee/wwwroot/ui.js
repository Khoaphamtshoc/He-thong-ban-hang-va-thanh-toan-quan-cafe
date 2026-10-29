// Delegated handlers survive Blazor navigation without registering twice.
// Checkout still goes through the same Blazor button and server validation.
(() => {
  document.addEventListener('click', (event) => {
    if (event.target.closest('.sidebar .menu a')) {
      const menu = document.getElementById('nav-toggle');
      if (menu) menu.checked = false;
    }
  });
  document.addEventListener('keydown', (event) => {
    const sales = document.querySelector('[data-sales-page]');
    if (event.key === 'Escape') {
      const menu = document.getElementById('nav-toggle');
      if (menu) menu.checked = false;
      const close = sales?.querySelector('[data-dismiss-checkout]');
      if (close && !close.disabled) close.click();
    }
    if (!sales || event.repeat || event.isComposing) return;
    const dialog = sales.querySelector('[data-checkout-dialog]');
    if ((event.ctrlKey || event.metaKey) && !event.altKey && event.key.toLowerCase() === 'k') {
      event.preventDefault();
      if (!dialog) {
        const search = sales.querySelector('#sales-search');
        search?.focus();
        search?.select();
      }
    }
    if (event.key === 'F4' && !event.ctrlKey && !event.metaKey && !event.altKey && !event.shiftKey) {
      event.preventDefault();
      const checkout = sales.querySelector('[data-checkout]');
      if (!dialog && checkout && !checkout.disabled) checkout.click();
    }
  });
})();
