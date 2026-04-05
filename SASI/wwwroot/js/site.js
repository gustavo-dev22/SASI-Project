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
