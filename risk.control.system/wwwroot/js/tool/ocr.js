
const dropZone = document.getElementById('drop-zone');
const fileInput = document.getElementById('DocumentImage');
const previewImg = document.getElementById('previewDoc');

// 1. Click to trigger file input
dropZone.addEventListener('click', () => fileInput.click());

// 2. Handle File Selection via Input
fileInput.addEventListener('change', function () {
    handleFiles(this.files);
});

// 3. Prevent default drag behaviors
['dragenter', 'dragover', 'dragleave', 'drop'].forEach(eventName => {
    dropZone.addEventListener(eventName, preventDefaults, false);
});

function preventDefaults(e) {
    e.preventDefault();
    e.stopPropagation();
}

// 4. Highlight drop zone when dragging over
['dragenter', 'dragover'].forEach(eventName => {
    dropZone.addEventListener(eventName, () => dropZone.classList.add('bg-secondary', 'text-white'), false);
});

['dragleave', 'drop'].forEach(eventName => {
    dropZone.addEventListener(eventName, () => dropZone.classList.remove('bg-secondary', 'text-white'), false);
});

// 5. Handle Dropped Files
dropZone.addEventListener('drop', (e) => {
    const dt = e.dataTransfer;
    const files = dt.files;

    // Assign dropped files to the hidden input
    fileInput.files = files;
    handleFiles(files);
});

// 6. Preview Logic
function handleFiles(files) {
    if (files.length > 0) {
        const file = files[0];
        if (file.type.startsWith('image/')) {
            const reader = new FileReader();
            reader.onload = (e) => {
                previewImg.src = e.target.result;
            };
            reader.readAsDataURL(file);
        }
    }
}
$(document).ready(function () {
    // Image Preview Logic
    $("#DocumentImage").change(function () {
        if (this.files && this.files[0]) {
            var reader = new FileReader();
            reader.onload = function (e) {
                $('#previewDoc').attr('src', e.target.result);
            }
            reader.readAsDataURL(this.files[0]);
        }
    });

    // Ajax Submission
    $("#ocrForm").on("submit", function (e) {
        e.preventDefault();

        var formData = new FormData(this);
        $("#loader").removeClass("d-none");
        $("#btnSubmit").prop("disabled", true).html('<i class="fas fa-sync fa-spin"></i> Processing Document...');
        dropZone.classList.add('scanning');

        $.ajax({
            url: '/Ocr/OcrDocument',
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            success: function (response) {
                // 1. Set the OCR text
                $("#ocrResult").val(response.description).removeClass("d-none");

                // 2. Update the numeric count
                const remaining = response.remaining;
                $("#remainingCount").text(remaining);

                // 3. Update Badge Visuals (Optional: Change color if low)
                const badge = $("#usageBadge");
                if (remaining <= 1) {
                    badge.removeClass("badge-light").addClass("badge-danger");
                } else {
                    badge.removeClass("badge-danger").addClass("badge-light");
                }
                // Add this inside your success function
                $("#usageBadge").fadeOut(100).fadeIn(100).fadeOut(100).fadeIn(100);
                // 4. If limit is hit, disable the submit button immediately
                if (remaining <= 0) {
                    $("#btnSubmit").prop("disabled", true).html('<i class="fas fa-lock"></i> Limit Reached');
                }
            },
            error: function (xhr) {
                if (xhr.status === 403) {
                    // Specifically handle the limit reached error
                    $("#btnSubmit").prop("disabled", true).text("Limit Reached");
                } else {
                    alert("Error: " + (xhr.responseText || "Check file size or format."));
                }
            },
            complete: function () {
                $("#loader").addClass("d-none");
                dropZone.classList.remove('scanning');

                // Only re-enable if the count hasn't hit zero
                const currentRemaining = parseInt($("#remainingCount").text());
                if (currentRemaining > 0) {
                    $("#btnSubmit").prop("disabled", false).html('<i class="fas fa-bolt"></i> Start Extraction');
                } else {
                    $("#btnSubmit").prop("disabled", true).html('<i class="fas fa-lock"></i> Limit Reached');
                }
            }
        });
    });
});