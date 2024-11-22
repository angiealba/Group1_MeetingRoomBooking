function showToast(notificationMessage, timeLeftMessage) {
    // Display the Toast with message and time left
    const toastContainer = document.querySelector('.toast-container') || createToastContainer();
    const toastElement = document.createElement('div');
    toastElement.classList.add('toast');
    toastElement.setAttribute('role', 'alert');
    toastElement.setAttribute('aria-live', 'assertive');
    toastElement.setAttribute('aria-atomic', 'true');

    toastElement.innerHTML = `
        <div class="toast-header">
            <strong class="me-auto">Booking Reminder</strong>
            <small>Just now</small>
            <button type="button" class="btn-close" data-bs-dismiss="toast" aria-label="Close"></button>
        </div>
        <div class="toast-body">
            ${notificationMessage}<br><strong>${timeLeftMessage}</strong>
        </div>
    `;

    toastContainer.appendChild(toastElement);

    const toast = new bootstrap.Toast(toastElement, {
        autohide: true,
        delay: 4000
    });

    toast.show();

    toastElement.addEventListener('hidden.bs.toast', () => {
        toastElement.remove();
    });
}

function createToastContainer() {
    const toastContainer = document.createElement('div');
    toastContainer.classList.add('toast-container');
    document.body.appendChild(toastContainer);
    return toastContainer;
}
