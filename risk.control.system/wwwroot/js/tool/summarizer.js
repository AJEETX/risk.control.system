document.addEventListener("DOMContentLoaded", function () {
    const summaryForm = document.getElementById("summaryForm");
    const fileInput = document.getElementById("pdfFile");
    const dropZone = document.getElementById("drop-zone");
    const summaryResult = document.getElementById("summaryResult");
    const btnSubmit = document.getElementById("btnSubmit");
    const loader = document.getElementById("loader");
    const fileNameDisplay = document.getElementById("fileNameDisplay");
    const summaryActions = document.getElementById("summaryActions");

    // Click to browse
    dropZone.addEventListener("click", () => fileInput.click());

    // Drag & Drop effects
    dropZone.addEventListener("dragover", (e) => {
        e.preventDefault();
        dropZone.style.borderColor = "#007bff";
        dropZone.style.backgroundColor = "#eef9ff";
    });

    ["dragleave", "dragend", "drop"].forEach(type => {
        dropZone.addEventListener(type, () => {
            dropZone.style.backgroundColor = "";
            dropZone.style.borderColor = "";
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
            return;
        }
        fileNameDisplay.innerText = file.name;
        fileNameDisplay.classList.remove("d-none");
    }

    // AJAX Submission
    summaryForm.addEventListener("submit", async function (e) {
        e.preventDefault();

        // UI State: Loading
        btnSubmit.disabled = true;
        btnSubmit.innerHTML = `<span class="spinner-border spinner-border-sm"></span> Processing...`;
        loader.classList.remove("d-none");
        summaryResult.classList.add("d-none");
        summaryActions.classList.add("d-none");

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
                summaryResult.classList.remove("d-none");
                summaryActions.classList.remove("d-none");
            } else {
                summaryResult.value = "AI Alert: " + (result.errorMessage || "Processing failed.");
                summaryResult.classList.remove("d-none");
            }
        } catch (error) {
            summaryResult.value = "System Error: Unable to reach the AI engine.";
            summaryResult.classList.remove("d-none");
        } finally {
            btnSubmit.disabled = false;
            btnSubmit.innerHTML = `<i class="fas fa-magic mr-2"></i> Generate Summary`;
            loader.classList.add("d-none");
        }
    });

    // Copy to Clipboard feature
    document.getElementById("copyBtn").addEventListener("click", () => {
        summaryResult.select();
        document.execCommand("copy");
        const originalText = document.getElementById("copyBtn").innerHTML;
        document.getElementById("copyBtn").innerHTML = '<i class="fas fa-check"></i> Copied!';
        setTimeout(() => {
            document.getElementById("copyBtn").innerHTML = originalText;
        }, 2000);
    });
});