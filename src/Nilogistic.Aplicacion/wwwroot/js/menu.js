/* Menú móvil accesible - HU-036 */
(function () {
  document.addEventListener('keydown', function (e) {
    if (e.key !== 'Escape') return;
    var openMenus = document.querySelectorAll('.collapse.show[id^="menu-"]');
    openMenus.forEach(function (m) {
      var inst = bootstrap.Collapse.getInstance(m);
      if (inst) {
        inst.hide();
      }
      var btn = document.querySelector('[data-bs-target="#' + m.id + '"]');
      if (btn) {
        btn.focus();
      }
    });
  });
})();
