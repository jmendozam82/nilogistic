/* Respaldo accesible para Turnstile - HU-013 */
(function () {
  var contenedor = document.querySelector('[data-nl-antibot]');
  if (!contenedor) return;

  var aviso = contenedor.querySelector('[data-nl-antibot-fallo]');
  if (!aviso) return;

  function ocultarAviso() {
    aviso.hidden = true;
  }

  function mostrarAviso() {
    aviso.hidden = false;
  }

  window.nlAntibotExito = ocultarAviso;
  window.nlAntibotError = mostrarAviso;

  window.addEventListener('load', function () {
    if (!('turnstile' in window)) {
      mostrarAviso();
    }
  });
})();
