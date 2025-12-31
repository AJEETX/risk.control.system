document.addEventListener("DOMContentLoaded", function () {
    // Select all buttons inside the tool cards
    const toolButtons = document.querySelectorAll('.card-body .btn');

    toolButtons.forEach(button => {
        button.addEventListener('click', function (e) {
            // 1. Capture the existing text of the button (e.g., "Launch OCR Tool")
            const originalText = this.innerText.trim();

            // 3. Update the clicked button style
            this.classList.add('disabled');
            this.style.pointerEvents = 'none';

            // 4. Set the button HTML to: Spinner + Original Text
            this.innerHTML = `<i class="fas fa-sync fa-spin mr-2"></i> ${originalText}...`;

            // The browser continues to the href destination automatically
        });
    });
});