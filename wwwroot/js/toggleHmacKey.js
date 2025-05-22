document.addEventListener("DOMContentLoaded", function () {
    const toggleButton = document.getElementById("toggleHmacKey");
    const input = document.getElementById("hmacKey");

    if (toggleButton && input) {
        toggleButton.addEventListener("click", function () {
            if (input.type === "password") {
                input.type = "text";
                this.innerHTML = '<i class="bi bi-eye-slash"></i>';
            } else {
                input.type = "password";
                this.innerHTML = '<i class="bi bi-eye"></i>';
            }
        });
    }
});
