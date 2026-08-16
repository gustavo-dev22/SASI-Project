document.addEventListener('change', function (e) {
    if (e.target && e.target.id === 'Tipo') {
        const tipo = e.target.value;

        const divPadre = document.getElementById('divPadre');
        const padreSelect = document.querySelector('[name="IdPadre"]');

        if (divPadre && padreSelect) {
            if (tipo === 'Submenu' || tipo === 'Item') {
                divPadre.style.display = 'block';
            } else {
                divPadre.style.display = 'none';
                padreSelect.value = '';
            }
        }
    }
});

$(function () {
    const token = document.querySelector('meta[name="RequestVerificationToken"]')?.getAttribute('content');

    if (token) {
        $.ajaxSetup({
            beforeSend: function (xhr) {
                xhr.setRequestHeader('RequestVerificationToken', token);
            }
        });
    }
});

(function () {
    const token = document.querySelector('meta[name="RequestVerificationToken"]')?.getAttribute('content');
    if (!token) return;

    const fetchOriginal = window.fetch;
    window.fetch = function (url, options) {
        options = options || {};
        const method = (options.method || 'GET').toUpperCase();
        if (method !== 'GET' && method !== 'HEAD' && method !== 'OPTIONS') {
            if (options.headers instanceof Headers) {
                if (!options.headers.has('RequestVerificationToken')) {
                    options.headers.append('RequestVerificationToken', token);
                }
            } else {
                options.headers = options.headers || {};
                options.headers['RequestVerificationToken'] = token;
            }
        }
        return fetchOriginal.call(this, url, options);
    };
})();

function mostrarSpinnerYMensaje({ mensajeExito, callbackFinal = null, tiempoSimulado = 3000 }) {
    Swal.fire({
        title: 'Procesando...',
        text: 'Por favor, espera un momento.',
        allowOutsideClick: false,
        allowEscapeKey: false,
        didOpen: () => {
            Swal.showLoading();
        }
    });

    setTimeout(() => {
        Swal.fire({
            icon: 'success',
            title: 'Éxito',
            text: mensajeExito || 'Operación completada correctamente.',
            confirmButtonText: 'Aceptar',
            allowOutsideClick: false,
            allowEscapeKey: false
        }).then(() => {
            if (typeof callbackFinal === 'function') {
                callbackFinal();
            }
        });
    }, tiempoSimulado);
}
