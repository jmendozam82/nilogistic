(function () {
    "use strict";

    var elementos = document.querySelectorAll(".nl-revelar");
    if (elementos.length === 0) {
        return;
    }

    // Sin IntersectionObserver o con movimiento reducido no se oculta nada: el contenido queda visible.
    if (typeof IntersectionObserver !== "function" ||
        (typeof window.matchMedia === "function" &&
            window.matchMedia("(prefers-reduced-motion: reduce)").matches)) {
        return;
    }

    var observador = new IntersectionObserver(function (entradas, obs) {
        entradas.forEach(function (entrada) {
            if (entrada.isIntersecting) {
                entrada.target.classList.remove("nl-revelar--oculto");
                obs.unobserve(entrada.target);
            }
        });
    }, { threshold: 0.15 });

    elementos.forEach(function (elemento) {
        // Lo que ya está en pantalla no se oculta (evita un parpadeo al cargar).
        if (elemento.getBoundingClientRect().top < window.innerHeight) {
            return;
        }

        elemento.classList.add("nl-revelar--oculto");
        observador.observe(elemento);

        // Si el foco entra (por teclado) en un elemento aún oculto, se revela de inmediato.
        elemento.addEventListener("focusin", function () {
            elemento.classList.remove("nl-revelar--oculto");
            observador.unobserve(elemento);
        });
    });
})();
