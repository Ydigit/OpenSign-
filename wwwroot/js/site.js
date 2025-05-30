﻿// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
particlesJS("particles-js", {
    "particles": {
        "number": {
            "value": 100,
            "density": {
                "enable": true,
                "value_area": 800
            }
        },
        "color": { "value": "#ffffff" },
        "shape": { "type": "circle" },
        "opacity": { "value": 0.5 },
        "size": { "value": 3, "random": true },
        "line_linked": {
            "enable": true,
            "distance": 150,
            "color": "#ffffff",
            "opacity": 0.4,
            "width": 1
        },
        "move": {
            "enable": true,
            "speed": 6,
            "direction": "none",
            "out_mode": "out"
        }
    },
    "interactivity": {
        "events": {
            "onhover": { "enable": true, "mode": "grab" },
            "onclick": { "enable": true, "mode": "push" }
        },
        "modes": {
            "grab": { "distance": 140, "line_linked": { "opacity": 1 } },
            "push": { "particles_nb": 4 }
        }
    },
    "retina_detect": true
});

document.addEventListener("DOMContentLoaded", function () {
    const passwordInput = document.querySelector("#pss");
    const toggleButton = document.querySelector("#togglePassword");

    if (passwordInput && toggleButton) {
        toggleButton.addEventListener("click", () => {
            const type = passwordInput.getAttribute("type") === "password" ? "text" : "password";
            passwordInput.setAttribute("type", type);

            // Altera ícone do botão
            const icon = toggleButton.querySelector("i");
            if (icon) {
                icon.classList.toggle("bi-eye");
                icon.classList.toggle("bi-eye-slash");
            }
        });
    }
});

