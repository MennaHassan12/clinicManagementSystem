// Wires up a row of single-digit OTP boxes: auto-advances focus as the user
// types, supports backspace/paste, and keeps a hidden input (asp-for="Otp")
// in sync so the value still posts normally with the form.
// function initOtpInputs(containerId, hiddenInputId) {
//     const container = document.getElementById(containerId);
//     const hiddenInput = document.getElementById(hiddenInputId);
//     if (!container || !hiddenInput) return;

//     const boxes = Array.from(container.querySelectorAll(".otp-box"));

//     function syncHiddenValue() {
//         hiddenInput.value = boxes.map(b => b.value).join("");
//     }

//     boxes.forEach((box, index) => {
//         box.addEventListener("input", () => {
//             box.value = box.value.replace(/[^0-9]/g, "").slice(0, 1);
//             if (box.value && index < boxes.length - 1) {
//                 boxes[index + 1].focus();
//             }
//             syncHiddenValue();
//         });

//         box.addEventListener("keydown", (e) => {
//             if (e.key === "Backspace" && !box.value && index > 0) {
//                 boxes[index - 1].focus();
//             }
//         });

//         box.addEventListener("paste", (e) => {
//             e.preventDefault();
//             const digits = (e.clipboardData.getData("text") || "").replace(/[^0-9]/g, "").split("");
//             boxes.forEach((b, i) => { b.value = digits[i] || ""; });
//             const lastFilled = Math.min(digits.length, boxes.length) - 1;
//             if (lastFilled >= 0) boxes[lastFilled].focus();
//             syncHiddenValue();
//         });
//     });

//     if (boxes.length) boxes[0].focus();
// }
function togglePwd(inputId, btn) {
    const input = document.getElementById(inputId);
    input.type = input.type === "password" ? "text" : "password";
}

// Highlight fields red when jquery-validate flags them invalid
document.addEventListener("DOMContentLoaded", function () {
    document.querySelectorAll("input[data-val='true']").forEach(function (el) {
        el.addEventListener("blur", function () {
            const wrapper = el.closest(".input-field");
            const errorSpan = wrapper.parentElement.querySelector(".field-error");
            if (wrapper && errorSpan) {
                if (errorSpan.textContent.trim().length > 0) {
                    wrapper.classList.add("input-invalid");
                } else {
                    wrapper.classList.remove("input-invalid");
                }
            }
        });
    });
});
