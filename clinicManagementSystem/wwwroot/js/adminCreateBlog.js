document.addEventListener("DOMContentLoaded", function() {
    var fileInput = document.querySelector(".custom-file-input");

    if (fileInput)
    {
        fileInput.addEventListener("change", function() {
            var fileName = this.value.split("\\").pop();
            var label = this.nextElementSibling;
            if (label && label.classList.contains("custom-file-label"))
            {
                label.classList.add("selected");
                label.innerHTML = fileName || "Choose file...";
            }
        });
    }
});