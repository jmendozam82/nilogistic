/* Carga de Google Analytics 4 con consentimiento previo - HU-066 */
(function () {
  var actual = document.currentScript;
  if (!actual) return;

  var id = actual.dataset.idMedicion || '';
  if (!/^G-[A-Z0-9]{6,12}$/.test(id)) return;

  window.dataLayer = window.dataLayer || [];
  window.gtag = function () {
    window.dataLayer.push(arguments);
  };

  var script = document.createElement('script');
  script.async = true;
  script.src = 'https://www.googletagmanager.com/gtag/js?id=' + encodeURIComponent(id);
  document.head.appendChild(script);

  window.gtag('js', new Date());
  window.gtag('config', id, {
    allow_google_signals: false,
    allow_ad_personalization_signals: false,
    cookie_domain: window.location.hostname,
    cookie_flags: 'SameSite=Lax;Secure'
  });
})();
