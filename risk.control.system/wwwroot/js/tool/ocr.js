
const dropZone = document.getElementById('drop-zone');
const fileInput = document.getElementById('DocumentImage');
const previewImg = document.getElementById('previewDoc');
const dropText = document.getElementById('drop-text');

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
                dropText.innerHTML = `<strong>Selected:</strong> ${file.name}`;
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
        $("#ocrResult").addClass("d-none");
        $("#resultActions").addClass("d-none");
        $("#btnSubmit").prop("disabled", true).html('<i class="fas fa-sync fa-spin"></i> Processing Document...');
        dropZone.addClass('scanning');

        $.ajax({
            url: '/Ocr/OcrDocument',
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            success: function (response) {
                $("#ocrResult").val(response).removeClass("d-none");
                $("#resultActions").removeClass("d-none");
            },
            error: function (xhr) {
                alert("Error: " + (xhr.responseText || "Check file size or format."));
            },
            complete: function () {
                $("#loader").addClass("d-none");
                $("#btnSubmit").prop("disabled", false).html('<i class="fas fa-bolt"></i> Start Extraction');
                dropZone.removeClass('scanning'); // Stop animation
            }
        });
    });
});