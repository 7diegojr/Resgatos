// Copia a chave PIX ao clicar no botão do card de Doação por PIX
document.addEventListener("DOMContentLoaded", function () {
    var button = document.querySelector(".copy-pix");
    if (!button) return;

    button.addEventListener("click", function () {
        var key = document.querySelector(".pix-value strong").textContent.trim();

        if (navigator.clipboard && window.isSecureContext) {
            navigator.clipboard.writeText(key);
            return;
        }

        // Fallback para quando o site não estiver em HTTPS/localhost
        var field = document.createElement("textarea");
        field.value = key;
        field.style.position = "fixed";
        field.style.opacity = "0";
        document.body.appendChild(field);
        field.select();
        document.execCommand("copy");
        document.body.removeChild(field);
    });
});
