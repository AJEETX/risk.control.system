document.addEventListener("DOMContentLoaded", function () {
    const summaryForm = document.getElementById("summaryForm");
    const fileInput = document.getElementById("pdfFile");
    const dropZone = document.getElementById("drop-zone");
    const summaryResult = document.getElementById("summaryResult");
    const btnSubmit = document.getElementById("btnSubmit");
    const loader = document.getElementById("loader");
    const fileNameDisplay = document.getElementById("fileNameDisplay");

    // Click to browse
    dropZone.addEventListener("click", () => fileInput.click());

    // Drag & Drop effects using classes
    dropZone.addEventListener("dragover", (e) => {
        e.preventDefault();
        dropZone.classList.add("drop-zone-active"); // Use a CSS class instead of inline style
    });

    ["dragleave", "dragend", "drop"].forEach(type => {
        dropZone.addEventListener(type, () => {
            dropZone.classList.remove("drop-zone-active");
        });
    });

    dropZone.addEventListener("drop", (e) => {
        e.preventDefault();
        if (e.dataTransfer.files.length) {
            fileInput.files = e.dataTransfer.files;
            handleFileSelection(fileInput.files[0]);
        }
    });

    fileInput.addEventListener("change", () => {
        if (fileInput.files.length) handleFileSelection(fileInput.files[0]);
    });

    function handleFileSelection(file) {
        if (file.type !== "application/pdf") {
            alert("Only PDF files are supported.");
            fileInput.value = "";
            fileNameDisplay.classList.add("d-none");
            return;
        }
        fileNameDisplay.innerText = file.name;
        fileNameDisplay.classList.remove("d-none");
    }

    // AJAX Submission
    summaryForm.addEventListener("submit", async function (e) {
        e.preventDefault();

        // UI State: SHOW LOADER OVERLAY
        btnSubmit.disabled = true;
        const originalBtnText = btnSubmit.innerHTML;
        btnSubmit.innerHTML = `<span class="spinner-border spinner-border-sm"></span> Analyzing PDF...`;

        // Show the overlay loader (it sits on top of the textarea)
        loader.classList.remove("d-none");
        summaryResult.placeholder = "AI is reading your document...";
        summaryResult.value = "";

        const formData = new FormData();
        formData.append("pdfFile", fileInput.files[0]);

        try {
            const response = await fetch('/PdfSummary/Summarize', {
                method: 'POST',
                body: formData
            });
            const result = await response.json();

            if (response.ok) {
                summaryResult.value = result.summary;

                // Update remaining tries UI
                if (result.remaining !== undefined) {
                    const remainingLabel = document.getElementById('remainingCount');
                    remainingLabel.innerText = result.remaining;

                    if (result.remaining <= 0) {
                        btnSubmit.innerHTML = `<i class="fas fa-lock"></i> Limit Reached`;
                        btnSubmit.disabled = true;
                        $("#usageBadge").removeClass("badge-soft-info").addClass("badge-soft-danger");
                    }
                }
            } else {
                summaryResult.value = "AI Alert: " + (result.errorMessage || "Processing failed.");
            }
        } catch (error) {
            summaryResult.value = "System Error: Unable to reach the AI engine.";
        } finally {
            // UI State: HIDE LOADER OVERLAY
            loader.classList.add("d-none");

            // Only re-enable if there are tries left
            const currentTries = parseInt(document.getElementById('remainingCount').innerText);
            if (currentTries > 0) {
                btnSubmit.disabled = false;
                btnSubmit.innerHTML = originalBtnText;
            }
        }
    });
});